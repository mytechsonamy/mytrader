using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using System.Collections.Concurrent;

namespace MyTrader.Core.Services;

/// <summary>
/// Orchestrates data requests across multiple data providers
/// Routes requests to appropriate providers based on asset class, market, and availability
/// </summary>
public class DataProviderOrchestrator : IDataProviderOrchestrator
{
    private readonly ILogger<DataProviderOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, IDataProvider> _providers;
    private readonly ConcurrentDictionary<string, ProviderMetrics> _metrics;
    private readonly Dictionary<string, DataProviderConfig> _configurations;

    public event EventHandler<ProviderStatusChangedEventArgs>? OnProviderStatusChanged;
    public event EventHandler<MarketDataUpdateDto>? OnMarketDataUpdate;

    public DataProviderOrchestrator(ILogger<DataProviderOrchestrator> logger)
    {
        _logger = logger;
        _providers = new ConcurrentDictionary<string, IDataProvider>();
        _metrics = new ConcurrentDictionary<string, ProviderMetrics>();
        _configurations = new Dictionary<string, DataProviderConfig>();
    }

    public void RegisterProvider(IDataProvider provider)
    {
        try
        {
            _providers.TryAdd(provider.ProviderId, provider);

            // Initialize metrics for the provider
            _metrics.TryAdd(provider.ProviderId, new ProviderMetrics
            {
                ProviderId = provider.ProviderId,
                MetricsStartTime = DateTime.UtcNow
            });

            // Subscribe to real-time updates if supported
            if (provider is IRealtimeDataProvider realtimeProvider)
            {
                realtimeProvider.OnPriceUpdate += (sender, update) =>
                {
                    OnMarketDataUpdate?.Invoke(this, update);
                };

                realtimeProvider.OnConnectionStatusChanged += (sender, args) =>
                {
                    HandleProviderStatusChange(args.ProviderId, args.IsConnected, args.Reason);
                };
            }

            _logger.LogInformation("Registered data provider: {ProviderId} - {ProviderName}",
                provider.ProviderId, provider.ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering provider: {ProviderId}", provider.ProviderId);
            throw;
        }
    }

    public IEnumerable<IDataProvider> GetProviders()
    {
        return _providers.Values;
    }

    public IEnumerable<IDataProvider> GetProvidersForAssetClass(string assetClass)
    {
        return _providers.Values.Where(p =>
            p.SupportedAssetClasses.Contains(assetClass, StringComparer.OrdinalIgnoreCase));
    }

    public IEnumerable<IDataProvider> GetProvidersForMarket(string market)
    {
        return _providers.Values.Where(p =>
            p.SupportedMarkets.Contains(market, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<IDataProvider?> GetBestProviderAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var availableProviders = GetAvailableProvidersForSymbol(symbol);

            if (!availableProviders.Any())
            {
                _logger.LogWarning("No available providers found for symbol: {Ticker}", symbol.Ticker);
                return null;
            }

            // Score providers based on health, performance, and configuration
            var scoredProviders = new List<(IDataProvider Provider, double Score)>();

            foreach (var provider in availableProviders)
            {
                var score = await CalculateProviderScore(provider, cancellationToken);
                scoredProviders.Add((provider, score));
            }

            // Return the highest scored provider
            var bestProvider = scoredProviders
                .OrderByDescending(p => p.Score)
                .FirstOrDefault().Provider;

            if (bestProvider != null)
            {
                _logger.LogDebug("Selected provider {ProviderId} for symbol {Ticker} with score {Score}",
                    bestProvider.ProviderId, symbol.Ticker, scoredProviders.First(p => p.Provider == bestProvider).Score);
            }

            return bestProvider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting best provider for symbol: {Ticker}", symbol.Ticker);
            return null;
        }
    }

    public async Task<UnifiedMarketDataDto?> GetMarketDataAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        IDataProvider? provider = null;

        try
        {
            provider = await GetBestProviderAsync(symbol, cancellationToken);
            if (provider == null)
            {
                return null;
            }

            var marketData = await provider.GetMarketDataAsync(symbol.Ticker, cancellationToken);

            // Update metrics on success
            UpdateProviderMetrics(provider.ProviderId, startTime, true);

            return marketData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market data for symbol: {Ticker}", symbol.Ticker);

            // Update metrics on failure
            if (provider != null)
            {
                UpdateProviderMetrics(provider.ProviderId, startTime, false, ex.Message);
            }

            throw;
        }
    }

    public async Task<BatchMarketDataDto> GetBatchMarketDataAsync(IEnumerable<Symbol> symbols, CancellationToken cancellationToken = default)
    {
        var response = new BatchMarketDataDto
        {
            RequestTimestamp = DateTime.UtcNow
        };

        try
        {
            var symbolsList = symbols.ToList();
            response.TotalSymbols = symbolsList.Count;

            // Group symbols by best provider
            var providerGroups = new Dictionary<IDataProvider, List<Symbol>>();

            foreach (var symbol in symbolsList)
            {
                var provider = await GetBestProviderAsync(symbol, cancellationToken);
                if (provider != null)
                {
                    if (!providerGroups.ContainsKey(provider))
                        providerGroups[provider] = new List<Symbol>();

                    providerGroups[provider].Add(symbol);
                }
            }

            // Execute batch requests per provider
            var tasks = providerGroups.Select(async kvp =>
            {
                var provider = kvp.Key;
                var providerSymbols = kvp.Value;
                var tickers = providerSymbols.Select(s => s.Ticker);

                try
                {
                    var marketDataList = await provider.GetBatchMarketDataAsync(tickers, cancellationToken);
                    return marketDataList;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting batch data from provider: {ProviderId}", provider.ProviderId);
                    response.Errors.Add($"Provider {provider.ProviderId}: {ex.Message}");
                    return new List<UnifiedMarketDataDto>();
                }
            });

            var results = await Task.WhenAll(tasks);

            // Combine results
            response.MarketData = results.SelectMany(r => r).ToList();
            response.SuccessfulSymbols = response.MarketData.Count;
            response.FailedSymbols = response.TotalSymbols - response.SuccessfulSymbols;
            response.ResponseTimestamp = DateTime.UtcNow;

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch market data");
            response.Errors.Add($"General error: {ex.Message}");
            response.ResponseTimestamp = DateTime.UtcNow;
            return response;
        }
    }

    public async Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(
        Symbol symbol,
        string interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await GetBestProviderAsync(symbol, cancellationToken);
            if (provider == null)
            {
                return null;
            }

            return await provider.GetHistoricalDataAsync(symbol.Ticker, interval, startTime, endTime, limit, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical data for symbol: {Ticker}", symbol.Ticker);
            throw;
        }
    }

    public async Task<MarketStatisticsDto?> GetMarketStatisticsAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await GetBestProviderAsync(symbol, cancellationToken);
            if (provider == null)
            {
                return null;
            }

            return await provider.GetMarketStatisticsAsync(symbol.Ticker, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market statistics for symbol: {Ticker}", symbol.Ticker);
            throw;
        }
    }

    public async Task InitializeAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        var initializationTasks = _providers.Values.Select(async provider =>
        {
            try
            {
                var initialized = await provider.InitializeAsync(cancellationToken);
                _logger.LogInformation("Provider {ProviderId} initialization: {Result}",
                    provider.ProviderId, initialized ? "Success" : "Failed");
                return (provider.ProviderId, initialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing provider: {ProviderId}", provider.ProviderId);
                return (provider.ProviderId, false);
            }
        });

        var results = await Task.WhenAll(initializationTasks);

        var successCount = results.Count(r => r.Item2);
        var totalCount = results.Length;

        _logger.LogInformation("Initialized {SuccessCount}/{TotalCount} data providers", successCount, totalCount);
    }

    public async Task<Dictionary<string, ComponentHealth>> GetAllProvidersHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthChecks = new Dictionary<string, ComponentHealth>();

        var healthTasks = _providers.Values.Select(async provider =>
        {
            try
            {
                var health = await provider.GetHealthAsync(cancellationToken);
                return (provider.ProviderId, health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for provider: {ProviderId}", provider.ProviderId);
                return (provider.ProviderId, new ComponentHealth
                {
                    Status = "Unhealthy",
                    Description = ex.Message
                });
            }
        });

        var results = await Task.WhenAll(healthTasks);

        foreach (var (providerId, health) in results)
        {
            healthChecks[providerId] = health;
        }

        return healthChecks;
    }

    private IEnumerable<IDataProvider> GetAvailableProvidersForSymbol(Symbol symbol)
    {
        return _providers.Values.Where(provider =>
        {
            // Check if provider is connected
            if (!provider.IsConnected)
                return false;

            // Check if provider supports the symbol
            var assetClass = symbol.AssetClassEntity?.Code ?? symbol.AssetClass;
            var market = symbol.Market?.Code ?? symbol.Venue;

            return provider.IsSymbolSupported(symbol.Ticker, assetClass, market);
        });
    }

    private async Task<double> CalculateProviderScore(IDataProvider provider, CancellationToken cancellationToken)
    {
        double score = 0;

        try
        {
            // Base score for connectivity
            if (provider.IsConnected)
                score += 50;

            // Get provider metrics
            if (_metrics.TryGetValue(provider.ProviderId, out var metrics))
            {
                // Success rate score (0-30 points)
                score += metrics.SuccessRate * 30;

                // Response time score (0-20 points, lower is better)
                var responseTimeMs = metrics.AverageResponseTime.TotalMilliseconds;
                if (responseTimeMs > 0)
                {
                    score += Math.Max(0, 20 - (responseTimeMs / 100)); // Penalty for slow responses
                }
            }

            // Configuration-based priority
            if (_configurations.TryGetValue(provider.ProviderId, out var config))
            {
                // Priority score (0-10 points, lower priority number = higher score)
                score += Math.Max(0, 10 - (config.Priority / 10.0));
            }

            // Health check score
            var health = await provider.GetHealthAsync(cancellationToken);
            score += health.Status switch
            {
                "Healthy" => 10,
                "Degraded" => 5,
                _ => 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error calculating provider score for {ProviderId}: {Error}", provider.ProviderId, ex.Message);
        }

        return Math.Max(0, Math.Min(100, score)); // Clamp between 0-100
    }

    private void UpdateProviderMetrics(string providerId, DateTime startTime, bool success, string? errorMessage = null)
    {
        if (!_metrics.TryGetValue(providerId, out var metrics))
            return;

        var responseTime = DateTime.UtcNow - startTime;

        metrics.TotalRequests++;
        metrics.LastRequestTime = DateTime.UtcNow;

        if (success)
        {
            metrics.SuccessfulRequests++;
        }
        else
        {
            metrics.FailedRequests++;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                metrics.RecentErrors.Add($"{DateTime.UtcNow:HH:mm:ss}: {errorMessage}");
                if (metrics.RecentErrors.Count > 10)
                    metrics.RecentErrors.RemoveAt(0);
            }
        }

        // Update average response time
        if (metrics.TotalRequests == 1)
        {
            metrics.AverageResponseTime = responseTime;
        }
        else
        {
            var totalMs = (metrics.AverageResponseTime.TotalMilliseconds * (metrics.TotalRequests - 1)) + responseTime.TotalMilliseconds;
            metrics.AverageResponseTime = TimeSpan.FromMilliseconds(totalMs / metrics.TotalRequests);
        }

        // Update success rate
        metrics.SuccessRate = (double)metrics.SuccessfulRequests / metrics.TotalRequests;
    }

    private void HandleProviderStatusChange(string providerId, bool isConnected, string? reason)
    {
        try
        {
            if (_providers.TryGetValue(providerId, out var provider))
            {
                var args = new ProviderStatusChangedEventArgs
                {
                    ProviderId = providerId,
                    ProviderName = provider.ProviderName,
                    IsConnected = isConnected,
                    Health = new ComponentHealth
                    {
                        Status = isConnected ? "Healthy" : "Unhealthy",
                        Description = reason
                    }
                };

                OnProviderStatusChanged?.Invoke(this, args);

                _logger.LogInformation("Provider {ProviderId} status changed: Connected={IsConnected}, Reason={Reason}",
                    providerId, isConnected, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling provider status change for: {ProviderId}", providerId);
        }
    }
}