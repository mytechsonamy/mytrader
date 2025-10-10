using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using MyTrader.Api.Hubs;
using MyTrader.Core.Models;
using MyTrader.Core.Enums;
using MyTrader.Core.Interfaces;
using MarketStatus = MyTrader.Core.Enums.MarketStatus;
using MyTrader.Services.Market;
using System.Collections.Concurrent;

namespace MyTrader.Api.Services;

/// <summary>
/// Enhanced broadcast service that connects the existing Binance service to SignalR hubs
/// Supports multi-asset data broadcasting with enhanced features
/// </summary>
public class MultiAssetDataBroadcastService : IHostedService, IDisposable
{
    private readonly IBinanceWebSocketService _binanceService;
    private readonly YahooFinancePollingService _yahooFinanceService;
    private readonly IServiceProvider _serviceProvider; // Changed to IServiceProvider for optional DataSourceRouter
    private readonly IHubContext<DashboardHub> _dashboardHubContext;
    private readonly IHubContext<MarketDataHub> _marketDataHubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMarketHoursService _marketHoursService;
    private readonly ILogger<MultiAssetDataBroadcastService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _lastBroadcastTimes;
    private readonly Timer _metricsTimer;
    private bool _disposed;
    private MyTrader.Core.Services.IDataSourceRouter? _dataSourceRouter; // Optional - only if Alpaca enabled

    // Broadcast throttling configuration
    private readonly TimeSpan _minBroadcastInterval = TimeSpan.FromMilliseconds(50); // Max 20 updates per second per symbol
    private readonly int _maxConcurrentBroadcasts = 10;
    private readonly SemaphoreSlim _broadcastSemaphore;

    // Metrics
    private long _totalBroadcasts;
    private long _throttledUpdates;
    private long _failedBroadcasts;

    public MultiAssetDataBroadcastService(
        IBinanceWebSocketService binanceService,
        YahooFinancePollingService yahooFinanceService,
        IServiceProvider serviceProvider,
        IHubContext<DashboardHub> dashboardHubContext,
        IHubContext<MarketDataHub> marketDataHubContext,
        IServiceScopeFactory scopeFactory,
        IMarketHoursService marketHoursService,
        ILogger<MultiAssetDataBroadcastService> logger)
    {
        _binanceService = binanceService;
        _yahooFinanceService = yahooFinanceService;
        _serviceProvider = serviceProvider;
        _dashboardHubContext = dashboardHubContext;
        _marketDataHubContext = marketDataHubContext;
        _scopeFactory = scopeFactory;
        _marketHoursService = marketHoursService;
        _logger = logger;
        _lastBroadcastTimes = new ConcurrentDictionary<string, DateTime>();
        _broadcastSemaphore = new SemaphoreSlim(_maxConcurrentBroadcasts, _maxConcurrentBroadcasts);

        // Initialize metrics timer (log metrics every 60 seconds)
        _metricsTimer = new Timer(LogMetrics, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MultiAssetDataBroadcastService starting - connecting multi-asset data to SignalR");

        try
        {
            // Subscribe to Binance service events (crypto)
            _binanceService.PriceUpdated += OnBinancePriceUpdated;

            // Try to get DataSourceRouter (optional - only if Alpaca streaming is enabled)
            _dataSourceRouter = _serviceProvider.GetService<MyTrader.Core.Services.IDataSourceRouter>();

            if (_dataSourceRouter != null)
            {
                // Subscribe to routed stock price events from DataSourceRouter
                _dataSourceRouter.PriceDataRouted += OnRoutedStockPriceUpdated;
                _logger.LogInformation("Connected to DataSourceRouter for Alpaca/Yahoo routing");

                // Also subscribe to Yahoo directly so it can notify the router
                _yahooFinanceService.StockPriceUpdated += OnYahooStockPriceForRouter;
            }
            else
            {
                // Fallback: Subscribe directly to Yahoo Finance service events (legacy mode)
                _yahooFinanceService.StockPriceUpdated += OnStockPriceUpdated;
                _logger.LogInformation("DataSourceRouter not available, using direct Yahoo Finance integration");
            }

            // Start metrics timer
            _metricsTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

            _logger.LogInformation("MultiAssetDataBroadcastService started successfully - listening to Binance (crypto) and stock data sources");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MultiAssetDataBroadcastService");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MultiAssetDataBroadcastService stopping");

        try
        {
            // Stop metrics timer
            _metricsTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Unsubscribe from service events
            _binanceService.PriceUpdated -= OnBinancePriceUpdated;

            if (_dataSourceRouter != null)
            {
                _dataSourceRouter.PriceDataRouted -= OnRoutedStockPriceUpdated;
                _yahooFinanceService.StockPriceUpdated -= OnYahooStockPriceForRouter;
            }
            else
            {
                _yahooFinanceService.StockPriceUpdated -= OnStockPriceUpdated;
            }

            // Wait for any ongoing broadcasts to complete
            for (int i = 0; i < _maxConcurrentBroadcasts; i++)
            {
                await _broadcastSemaphore.WaitAsync(cancellationToken);
            }

            _logger.LogInformation("MultiAssetDataBroadcastService stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MultiAssetDataBroadcastService");
            throw;
        }
    }

    private async void OnBinancePriceUpdated(PriceUpdateData priceUpdate)
    {
        if (ShouldThrottleUpdate(priceUpdate.Symbol))
        {
            Interlocked.Increment(ref _throttledUpdates);
            return;
        }

        // Convert to multi-asset format for enhanced broadcasting
        var multiAssetUpdate = new MultiAssetPriceUpdate
        {
            Type = "PriceUpdate",
            AssetClass = AssetClassCode.CRYPTO,
            Symbol = priceUpdate.Symbol,
            Price = priceUpdate.Price,
            Change24h = priceUpdate.PriceChange,
            Volume = priceUpdate.Volume,
            MarketStatus = MyTrader.Core.Enums.MarketStatus.OPEN, // Crypto markets are always open
            Timestamp = priceUpdate.Timestamp,
            Source = "BINANCE",
            Metadata = new Dictionary<string, object>
            {
                { "exchange", "BINANCE" },
                { "originalTimestamp", priceUpdate.Timestamp }
            }
        };

        _ = Task.Run(async () => await BroadcastPriceUpdateAsync(multiAssetUpdate));
    }

    private async void OnStockPriceUpdated(StockPriceData stockUpdate)
    {
        // Legacy handler for direct Yahoo integration (when DataSourceRouter is not available)
        _logger.LogDebug("Received stock price update for {Symbol} ({AssetClass}): ${Price} from {Source}",
            stockUpdate.Symbol, stockUpdate.AssetClass, stockUpdate.Price, stockUpdate.Source);

        // Enrich with market status information
        EnrichWithMarketStatus(stockUpdate);

        // Convert to multi-asset format for broadcasting
        var multiAssetUpdate = new MultiAssetPriceUpdate
        {
            Type = "PriceUpdate",
            AssetClass = stockUpdate.AssetClass,
            Symbol = stockUpdate.Symbol,
            Price = stockUpdate.Price,
            Change24h = stockUpdate.PriceChange, // ✅ FIX: Use price change amount, not percent
            Volume = stockUpdate.Volume,
            MarketStatus = stockUpdate.MarketStatus,
            Timestamp = stockUpdate.Timestamp,
            Source = $"YAHOO_{stockUpdate.Market}",
            Metadata = new Dictionary<string, object>
            {
                { "market", stockUpdate.Market },
                { "priceChange", stockUpdate.PriceChange },
                { "priceChangePercent", stockUpdate.PriceChangePercent },
                { "originalTimestamp", stockUpdate.Timestamp },
                { "dataSource", stockUpdate.Source },
                { "nextOpenTime", stockUpdate.NextOpenTime },
                { "nextCloseTime", stockUpdate.NextCloseTime },
                { "marketClosureReason", stockUpdate.MarketClosureReason ?? string.Empty }
            }
        };

        _ = Task.Run(async () => await BroadcastPriceUpdateAsync(multiAssetUpdate));
    }

    /// <summary>
    /// Handler for routed stock price data from DataSourceRouter (Alpaca/Yahoo)
    /// </summary>
    private async void OnRoutedStockPriceUpdated(StockPriceData stockUpdate)
    {
        _logger.LogDebug("Received routed stock price update for {Symbol}: ${Price} from {Source}",
            stockUpdate.Symbol, stockUpdate.Price, stockUpdate.Source);

        // Enrich with market status information
        EnrichWithMarketStatus(stockUpdate);

        // Convert to multi-asset format for broadcasting
        var multiAssetUpdate = new MultiAssetPriceUpdate
        {
            Type = "PriceUpdate",
            AssetClass = stockUpdate.AssetClass,
            Symbol = stockUpdate.Symbol,
            Price = stockUpdate.Price,
            Change24h = stockUpdate.PriceChange, // ✅ FIX: Use price change amount, not percent
            Volume = stockUpdate.Volume,
            MarketStatus = stockUpdate.MarketStatus,
            Timestamp = stockUpdate.Timestamp,
            Source = stockUpdate.Source, // "ALPACA" or "YAHOO_FALLBACK"
            Metadata = new Dictionary<string, object>
            {
                { "market", stockUpdate.Market },
                { "priceChange", stockUpdate.PriceChange },
                { "priceChangePercent", stockUpdate.PriceChangePercent },
                { "originalTimestamp", stockUpdate.Timestamp },
                { "dataSource", stockUpdate.Source },
                { "isRealTime", stockUpdate.IsRealTime },
                { "qualityScore", stockUpdate.QualityScore },
                { "nextOpenTime", stockUpdate.NextOpenTime },
                { "nextCloseTime", stockUpdate.NextCloseTime },
                { "marketClosureReason", stockUpdate.MarketClosureReason ?? string.Empty }
            }
        };

        _ = Task.Run(async () => await BroadcastPriceUpdateAsync(multiAssetUpdate));
    }

    /// <summary>
    /// Handler for Yahoo price data that forwards to DataSourceRouter
    /// </summary>
    private void OnYahooStockPriceForRouter(StockPriceData stockUpdate)
    {
        if (_dataSourceRouter != null)
        {
            // Mark as Yahoo fallback source
            stockUpdate.Source = "YAHOO_FALLBACK";
            stockUpdate.QualityScore = 80; // Lower quality score for fallback

            _dataSourceRouter.OnYahooPriceUpdate(stockUpdate);
        }
    }

    private async Task BroadcastPriceUpdateAsync(MultiAssetPriceUpdate priceUpdate)
    {
        if (!await _broadcastSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning("Broadcast semaphore timeout for price update: {Symbol}", priceUpdate.Symbol);
            Interlocked.Increment(ref _failedBroadcasts);
            return;
        }

        try
        {
            _logger.LogDebug("Broadcasting price update: {AssetClass} {Symbol} = {Price}",
                priceUpdate.AssetClass, priceUpdate.Symbol, priceUpdate.Price);

            var broadcastTasks = new List<Task>();

            // Send to symbol-specific group with new standard event names
            var symbolGroup = $"{priceUpdate.AssetClass}_{priceUpdate.Symbol}";
            
            // Broadcast to both DashboardHub and MarketDataHub
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(symbolGroup).SendAsync("PriceUpdate", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(symbolGroup).SendAsync("MarketDataUpdate", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(symbolGroup).SendAsync("PriceUpdate", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(symbolGroup).SendAsync("MarketDataUpdate", priceUpdate)));

            // Send to asset class group for dashboard updates with new standard event names
            var assetClassGroup = $"AssetClass_{priceUpdate.AssetClass}";
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(assetClassGroup).SendAsync("PriceUpdate", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(assetClassGroup).SendAsync("MarketDataUpdate", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(assetClassGroup).SendAsync("PriceUpdate", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(assetClassGroup).SendAsync("MarketDataUpdate", priceUpdate)));

            // Legacy format for backward compatibility with ReceivePriceUpdate and ReceiveMarketData events
            var legacyUpdate = new
            {
                symbol = priceUpdate.Symbol,
                price = priceUpdate.Price,
                change = priceUpdate.Change24h,
                volume = priceUpdate.Volume,
                timestamp = priceUpdate.Timestamp,
                assetClass = priceUpdate.AssetClass.ToString()
            };

            // Legacy events for backward compatibility with better error handling
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(symbolGroup).SendAsync("ReceivePriceUpdate", legacyUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(assetClassGroup).SendAsync("ReceivePriceUpdate", legacyUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(symbolGroup).SendAsync("ReceiveMarketData", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _dashboardHubContext.Clients.Group(assetClassGroup).SendAsync("ReceiveMarketData", priceUpdate)));
            
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(symbolGroup).SendAsync("ReceivePriceUpdate", legacyUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(assetClassGroup).SendAsync("ReceivePriceUpdate", legacyUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(symbolGroup).SendAsync("ReceiveMarketData", priceUpdate)));
            broadcastTasks.Add(SafeBroadcastAsync(() =>
                _marketDataHubContext.Clients.Group(assetClassGroup).SendAsync("ReceiveMarketData", priceUpdate)));

            // Additional legacy crypto-specific groups for backward compatibility
            if (priceUpdate.AssetClass == AssetClassCode.CRYPTO)
            {
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _dashboardHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("PriceUpdate", legacyUpdate)));
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _dashboardHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("ReceivePriceUpdate", legacyUpdate)));
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _dashboardHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("MarketDataUpdate", priceUpdate)));
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _dashboardHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("ReceiveMarketData", priceUpdate)));
                    
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _marketDataHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("PriceUpdate", legacyUpdate)));
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _marketDataHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("ReceivePriceUpdate", legacyUpdate)));
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _marketDataHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("MarketDataUpdate", priceUpdate)));
                broadcastTasks.Add(SafeBroadcastAsync(() =>
                    _marketDataHubContext.Clients.Group($"Symbol_{priceUpdate.Symbol}").SendAsync("ReceiveMarketData", priceUpdate)));
            }

            // Execute all broadcasts concurrently with timeout
            var allBroadcasts = Task.WhenAll(broadcastTasks);
            if (await Task.WhenAny(allBroadcasts, Task.Delay(TimeSpan.FromSeconds(10))) == allBroadcasts)
            {
                await allBroadcasts; // This will throw if any broadcast failed
            }
            else
            {
                _logger.LogWarning("Broadcast timeout for symbol: {Symbol}", priceUpdate.Symbol);
                Interlocked.Increment(ref _failedBroadcasts);
                return;
            }

            // Update last broadcast time
            _lastBroadcastTimes.AddOrUpdate(priceUpdate.Symbol, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

            Interlocked.Increment(ref _totalBroadcasts);

            _logger.LogDebug("Successfully broadcasted price update for {Symbol} to {GroupCount} groups",
                priceUpdate.Symbol, broadcastTasks.Count);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedBroadcasts);
            _logger.LogError(ex, "Error broadcasting price update for {AssetClass} {Symbol}",
                priceUpdate.AssetClass, priceUpdate.Symbol);
        }
        finally
        {
            _broadcastSemaphore.Release();
        }
    }

    /// <summary>
    /// Safely executes a SignalR broadcast with error handling
    /// </summary>
    private async Task SafeBroadcastAsync(Func<Task> broadcastFunc)
    {
        try
        {
            await broadcastFunc();
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogDebug(ex, "SignalR connection disposed during broadcast, skipping");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("disposed"))
        {
            _logger.LogDebug(ex, "SignalR hub disposed during broadcast, skipping");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Individual broadcast failed, continuing with others");
            throw; // Re-throw to be counted as failed broadcast
        }
    }


    private bool ShouldThrottleUpdate(string symbol)
    {
        if (!_lastBroadcastTimes.TryGetValue(symbol, out var lastBroadcast))
        {
            return false;
        }

        var timeSinceLastBroadcast = DateTime.UtcNow - lastBroadcast;
        return timeSinceLastBroadcast < _minBroadcastInterval;
    }

    private void LogMetrics(object? state)
    {
        try
        {
            var totalBroadcasts = Interlocked.Read(ref _totalBroadcasts);
            var throttledUpdates = Interlocked.Read(ref _throttledUpdates);
            var failedBroadcasts = Interlocked.Read(ref _failedBroadcasts);

            _logger.LogInformation(
                "MultiAssetDataBroadcast Metrics - Total: {TotalBroadcasts}, Throttled: {ThrottledUpdates}, Failed: {FailedBroadcasts}, Active Symbols: {ActiveSymbols}",
                totalBroadcasts, throttledUpdates, failedBroadcasts, _lastBroadcastTimes.Count);

            // Reset counters for next interval
            Interlocked.Exchange(ref _totalBroadcasts, 0);
            Interlocked.Exchange(ref _throttledUpdates, 0);
            Interlocked.Exchange(ref _failedBroadcasts, 0);

            // Clean up old broadcast times (symbols not seen in last 5 minutes)
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
            var keysToRemove = _lastBroadcastTimes
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _lastBroadcastTimes.TryRemove(key, out _);
            }

            if (keysToRemove.Any())
            {
                _logger.LogDebug("Cleaned up {Count} inactive symbols from broadcast tracking", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging metrics");
        }
    }

    /// <summary>
    /// Enriches stock price data with current market hours and status information
    /// </summary>
    private void EnrichWithMarketStatus(StockPriceData stockUpdate)
    {
        try
        {
            // Determine exchange from symbol
            var exchange = _marketHoursService.GetExchangeForSymbol(stockUpdate.Symbol);
            stockUpdate.Exchange = exchange;

            // Get market status
            var marketStatus = _marketHoursService.GetMarketStatus(exchange);

            // Enrich the DTO with market status information
            stockUpdate.MarketStatus = marketStatus.State;
            stockUpdate.LastUpdateTime = marketStatus.LastCheckTime;
            stockUpdate.NextOpenTime = marketStatus.NextOpenTime;
            stockUpdate.NextCloseTime = marketStatus.NextCloseTime;
            stockUpdate.MarketClosureReason = marketStatus.ClosureReason;

            _logger.LogTrace("Enriched {Symbol} with market status: {State} (Exchange: {Exchange})",
                stockUpdate.Symbol, marketStatus.State, exchange);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich market status for {Symbol}, using defaults",
                stockUpdate.Symbol);

            // Set safe defaults on error
            stockUpdate.MarketStatus = MarketStatus.UNKNOWN;
            stockUpdate.LastUpdateTime = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Stop the service if it's running
            StopAsync(CancellationToken.None).GetAwaiter().GetResult();

            // Dispose timers and semaphore
            _metricsTimer?.Dispose();
            _broadcastSemaphore?.Dispose();

            _disposed = true;
            _logger.LogInformation("MultiAssetDataBroadcastService disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing MultiAssetDataBroadcastService");
        }
    }
}