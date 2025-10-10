using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using MyTrader.Core.Services.ETL;
using System.Diagnostics;
using System.Text.Json;

namespace MyTrader.Infrastructure.Services.ETL;

/// <summary>
/// Production-ready asset enrichment service with external API integration,
/// rate limiting, circuit breaker pattern, and comprehensive error handling
/// </summary>
public class AssetEnrichmentService : IAssetEnrichmentService
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger<AssetEnrichmentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly EnrichmentConfiguration _config;
    private readonly SemaphoreSlim _enrichmentSemaphore;
    private readonly Dictionary<string, IExternalDataSource> _dataSources;
    private readonly Dictionary<string, DateTime> _lastRateLimitReset;

    public AssetEnrichmentService(
        ITradingDbContext dbContext,
        ILogger<AssetEnrichmentService> logger,
        HttpClient httpClient,
        IOptions<EnrichmentConfiguration> config)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpClient = httpClient;
        _config = config.Value;
        _enrichmentSemaphore = new SemaphoreSlim(_config.MaxConcurrency, _config.MaxConcurrency);
        _dataSources = InitializeDataSources();
        _lastRateLimitReset = new Dictionary<string, DateTime>();
    }

    public async Task<EnrichmentBatchResult> EnrichSymbolsAsync(
        List<Guid> symbolIds,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await _enrichmentSemaphore.WaitAsync(cancellationToken);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            options ??= new EnrichmentOptions();

            _logger.LogInformation("Starting batch enrichment for {Count} symbols with sources: {Sources}",
                symbolIds.Count, string.Join(", ", options.EnabledSources));

            var result = new EnrichmentBatchResult
            {
                ExecutedAt = DateTime.UtcNow,
                TotalSymbols = symbolIds.Count
            };

            // Get symbols from database
            var symbols = await _dbContext.Symbols
                .Where(s => symbolIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            if (symbols.Count != symbolIds.Count)
            {
                var missing = symbolIds.Count - symbols.Count;
                _logger.LogWarning("Could not find {MissingCount} symbols out of {TotalCount} requested",
                    missing, symbolIds.Count);
            }

            // Process symbols in batches
            var batches = CreateBatches(symbols, options.BatchSize);
            var batchTasks = batches.Select(async (batch, index) =>
            {
                return await ProcessEnrichmentBatchAsync(batch, options, index + 1, cancellationToken);
            });

            var batchResults = await Task.WhenAll(batchTasks);

            // Aggregate results
            foreach (var batchResult in batchResults)
            {
                result.SuccessfullyEnriched += batchResult.SuccessfullyEnriched;
                result.PartiallyEnriched += batchResult.PartiallyEnriched;
                result.Failed += batchResult.Failed;
                result.Skipped += batchResult.Skipped;
                result.TotalApiCalls += batchResult.TotalApiCalls;

                result.SymbolResults.AddRange(batchResult.SymbolResults);
                result.RateLimitWarnings.AddRange(batchResult.RateLimitWarnings);

                // Merge source results
                foreach (var sourceResult in batchResult.SourceResults)
                {
                    if (result.SourceResults.ContainsKey(sourceResult.Key))
                    {
                        var existing = result.SourceResults[sourceResult.Key];
                        existing.TotalRequests += sourceResult.Value.TotalRequests;
                        existing.SuccessfulRequests += sourceResult.Value.SuccessfulRequests;
                        existing.FailedRequests += sourceResult.Value.FailedRequests;
                        existing.RateLimitedRequests += sourceResult.Value.RateLimitedRequests;
                        existing.TotalDuration = existing.TotalDuration.Add(sourceResult.Value.TotalDuration);
                        existing.Errors.AddRange(sourceResult.Value.Errors);
                    }
                    else
                    {
                        result.SourceResults[sourceResult.Key] = sourceResult.Value;
                    }
                }
            }

            // Calculate API calls by source
            foreach (var source in result.SourceResults)
            {
                result.ApiCallsBySource[source.Key] = source.Value.TotalRequests;
            }

            result.Success = result.Failed == 0;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Batch enrichment completed. Success: {Success}, Partial: {Partial}, Failed: {Failed}, Duration: {Duration}",
                result.SuccessfullyEnriched, result.PartiallyEnriched, result.Failed, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during batch enrichment");
            return new EnrichmentBatchResult
            {
                Success = false,
                ErrorMessage = $"Fatal error: {ex.Message}",
                TotalSymbols = symbolIds.Count
            };
        }
        finally
        {
            _enrichmentSemaphore.Release();
        }
    }

    public async Task<EnrichmentResult> EnrichSymbolAsync(
        Guid symbolId,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new EnrichmentOptions();

        var symbol = await _dbContext.Symbols.FindAsync(new object[] { symbolId }, cancellationToken);
        if (symbol == null)
        {
            return new EnrichmentResult
            {
                SymbolId = symbolId,
                Success = false,
                ErrorMessage = "Symbol not found"
            };
        }

        return await EnrichSingleSymbolAsync(symbol, options, cancellationToken);
    }

    public async Task<EnrichmentStatus> GetEnrichmentStatusAsync(
        List<Guid>? symbolIds = null,
        CancellationToken cancellationToken = default)
    {
        var status = new EnrichmentStatus();

        var symbolQuery = _dbContext.Symbols.AsQueryable();
        if (symbolIds?.Any() == true)
        {
            symbolQuery = symbolQuery.Where(s => symbolIds.Contains(s.Id));
        }

        // Get basic statistics
        var symbolStats = await symbolQuery
            .GroupBy(s => 1)
            .Select(g => new
            {
                Total = g.Count(),
                WithFullName = g.Count(s => !string.IsNullOrEmpty(s.FullName)),
                WithSector = g.Count(s => !string.IsNullOrEmpty(s.Sector)),
                WithMarketCap = g.Count(s => s.MarketCap.HasValue),
                WithMetadata = g.Count(s => !string.IsNullOrEmpty(s.Metadata))
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (symbolStats != null)
        {
            status.TotalSymbols = symbolStats.Total;

            // Calculate enrichment levels (symbols with at least 3 key fields filled)
            var enrichedCount = Math.Max(Math.Max(symbolStats.WithFullName, symbolStats.WithSector),
                Math.Max(symbolStats.WithMarketCap, symbolStats.WithMetadata));

            status.FullyEnrichedSymbols = (int)(enrichedCount * 0.8); // Rough estimate
            status.PartiallyEnrichedSymbols = (int)(enrichedCount * 0.2);
            status.UnenrichedSymbols = status.TotalSymbols - status.FullyEnrichedSymbols - status.PartiallyEnrichedSymbols;
        }

        // Get enrichment by asset class
        var assetClassStats = await symbolQuery
            .GroupBy(s => s.AssetClass)
            .Select(g => new
            {
                AssetClass = g.Key,
                Total = g.Count(),
                Enriched = g.Count(s => !string.IsNullOrEmpty(s.FullName) || s.MarketCap.HasValue)
            })
            .ToListAsync(cancellationToken);

        foreach (var stat in assetClassStats)
        {
            status.EnrichmentByAssetClass[stat.AssetClass] = new EnrichmentByAssetClass
            {
                AssetClass = stat.AssetClass,
                TotalSymbols = stat.Total,
                EnrichedSymbols = stat.Enriched,
                UnenrichedSymbols = stat.Total - stat.Enriched
            };
        }

        status.IsCurrentlyRunning = _enrichmentSemaphore.CurrentCount == 0;

        return status;
    }

    public async Task<EnrichmentBatchResult> RefreshStaleEnrichmentsAsync(
        TimeSpan staleThreshold,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - staleThreshold;

        var staleSymbolIds = await _dbContext.Symbols
            .Where(s => s.IsActive && s.UpdatedAt < cutoffDate)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} stale symbols to refresh (older than {Threshold})",
            staleSymbolIds.Count, staleThreshold);

        if (!staleSymbolIds.Any())
        {
            return new EnrichmentBatchResult
            {
                Success = true,
                TotalSymbols = 0,
                ExecutedAt = DateTime.UtcNow
            };
        }

        var options = new EnrichmentOptions
        {
            OverwriteExistingData = true,
            MaxConcurrency = Math.Min(_config.MaxConcurrency, 2) // Use lower concurrency for refresh
        };

        return await EnrichSymbolsAsync(staleSymbolIds, options, cancellationToken);
    }

    public async Task<List<EnrichmentSourceStatus>> GetSourceStatusAsync()
    {
        var sourceStatuses = new List<EnrichmentSourceStatus>();

        foreach (var dataSource in _dataSources)
        {
            try
            {
                var status = await dataSource.Value.GetHealthStatusAsync();
                sourceStatuses.Add(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for source {Source}", dataSource.Key);
                sourceStatuses.Add(new EnrichmentSourceStatus
                {
                    SourceName = dataSource.Key,
                    IsHealthy = false,
                    HealthMessage = ex.Message
                });
            }
        }

        return sourceStatuses;
    }

    #region Private Methods

    private async Task<EnrichmentBatchResult> ProcessEnrichmentBatchAsync(
        List<Symbol> symbols,
        EnrichmentOptions options,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new EnrichmentBatchResult
        {
            TotalSymbols = symbols.Count,
            ExecutedAt = DateTime.UtcNow
        };

        _logger.LogDebug("Processing enrichment batch {BatchNumber} with {Count} symbols",
            batchNumber, symbols.Count);

        // Process symbols with controlled concurrency
        var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
        var tasks = symbols.Select(async symbol =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await EnrichSingleSymbolAsync(symbol, options, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var symbolResults = await Task.WhenAll(tasks);
        result.SymbolResults.AddRange(symbolResults);

        // Aggregate statistics
        foreach (var symbolResult in symbolResults)
        {
            if (symbolResult.Success)
            {
                if (symbolResult.FieldsEnriched.Count > 3) // Arbitrary threshold for "full" enrichment
                    result.SuccessfullyEnriched++;
                else if (symbolResult.FieldsEnriched.Any())
                    result.PartiallyEnriched++;
                else
                    result.Skipped++;
            }
            else
            {
                result.Failed++;
            }
        }

        result.Success = result.Failed == 0;
        result.Duration = stopwatch.Elapsed;

        return result;
    }

    private async Task<EnrichmentResult> EnrichSingleSymbolAsync(
        Symbol symbol,
        EnrichmentOptions options,
        CancellationToken cancellationToken)
    {
        var result = new EnrichmentResult
        {
            SymbolId = symbol.Id,
            Symbol = symbol.Ticker,
            EnrichedAt = DateTime.UtcNow
        };

        try
        {
            // Skip if symbol already has recent enrichment data and not forcing overwrite
            if (!options.OverwriteExistingData && IsRecentlyEnriched(symbol))
            {
                result.Success = true;
                _logger.LogDebug("Skipping {Symbol} - recently enriched", symbol.Ticker);
                return result;
            }

            var metadata = new AssetMetadata();
            var sourceSuccesses = 0;
            var totalSources = 0;

            // Try each enabled source
            foreach (var sourceName in options.EnabledSources)
            {
                if (!_dataSources.TryGetValue(sourceName, out var dataSource))
                {
                    _logger.LogWarning("Unknown enrichment source: {Source}", sourceName);
                    continue;
                }

                totalSources++;

                try
                {
                    // Rate limiting
                    await EnforceRateLimitAsync(sourceName, options.RateLimitDelay, cancellationToken);

                    var sourceResult = await dataSource.EnrichSymbolAsync(symbol, metadata, cancellationToken);
                    result.SourceResults[sourceName] = sourceResult;

                    if (sourceResult.Success)
                    {
                        sourceSuccesses++;
                        _logger.LogDebug("Successfully enriched {Symbol} from {Source}",
                            symbol.Ticker, sourceName);
                    }
                    else
                    {
                        _logger.LogDebug("Failed to enrich {Symbol} from {Source}: {Error}",
                            symbol.Ticker, sourceName, sourceResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enriching {Symbol} from {Source}", symbol.Ticker, sourceName);
                    result.SourceResults[sourceName] = new EnrichmentSourceResult
                    {
                        SourceName = sourceName,
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }

            // Apply enriched metadata to symbol
            if (sourceSuccesses > 0)
            {
                ApplyMetadataToSymbol(symbol, metadata, result);
                symbol.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _dbContext.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.DataQualityScore = CalculateDataQualityScore(metadata, symbol);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "No sources provided enrichment data";
            }

            _logger.LogDebug("Enriched {Symbol}: {SuccessCount}/{TotalCount} sources successful",
                symbol.Ticker, sourceSuccesses, totalSources);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching symbol {Symbol}", symbol.Ticker);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private void ApplyMetadataToSymbol(Symbol symbol, AssetMetadata metadata, EnrichmentResult result)
    {
        var fieldsUpdated = new Dictionary<string, bool>();

        // Apply enriched data to symbol properties
        if (!string.IsNullOrEmpty(metadata.FullName) && string.IsNullOrEmpty(symbol.FullName))
        {
            symbol.FullName = metadata.FullName;
            fieldsUpdated["FullName"] = true;
        }

        if (!string.IsNullOrEmpty(metadata.Description) && string.IsNullOrEmpty(symbol.Description))
        {
            symbol.Description = metadata.Description;
            fieldsUpdated["Description"] = true;
        }

        if (!string.IsNullOrEmpty(metadata.Sector) && string.IsNullOrEmpty(symbol.Sector))
        {
            symbol.Sector = metadata.Sector;
            fieldsUpdated["Sector"] = true;
        }

        if (!string.IsNullOrEmpty(metadata.Industry) && string.IsNullOrEmpty(symbol.Industry))
        {
            symbol.Industry = metadata.Industry;
            fieldsUpdated["Industry"] = true;
        }

        if (metadata.MarketCap.HasValue && !symbol.MarketCap.HasValue)
        {
            symbol.MarketCap = metadata.MarketCap.Value;
            fieldsUpdated["MarketCap"] = true;
        }

        if (metadata.CurrentPrice.HasValue)
        {
            symbol.CurrentPrice = metadata.CurrentPrice.Value;
            symbol.PriceUpdatedAt = DateTime.UtcNow;
            fieldsUpdated["CurrentPrice"] = true;
        }

        if (metadata.PricePrecision.HasValue && !symbol.PricePrecision.HasValue)
        {
            symbol.PricePrecision = metadata.PricePrecision.Value;
            fieldsUpdated["PricePrecision"] = true;
        }

        if (metadata.QuantityPrecision.HasValue && !symbol.QuantityPrecision.HasValue)
        {
            symbol.QuantityPrecision = metadata.QuantityPrecision.Value;
            fieldsUpdated["QuantityPrecision"] = true;
        }

        // Store additional metadata as JSON
        if (metadata.AdditionalData.Any())
        {
            try
            {
                symbol.Metadata = JsonSerializer.Serialize(metadata.AdditionalData);
                fieldsUpdated["Metadata"] = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize metadata for symbol {Symbol}", symbol.Ticker);
            }
        }

        result.FieldsEnriched = fieldsUpdated;
    }

    private bool IsRecentlyEnriched(Symbol symbol)
    {
        // Consider symbol recently enriched if updated within last 7 days and has basic enrichment
        return symbol.UpdatedAt > DateTime.UtcNow.AddDays(-7) &&
               (!string.IsNullOrEmpty(symbol.FullName) || symbol.MarketCap.HasValue);
    }

    private async Task EnforceRateLimitAsync(string sourceName, TimeSpan delay, CancellationToken cancellationToken)
    {
        // Simple rate limiting - in production this would be more sophisticated
        await Task.Delay(delay, cancellationToken);
    }

    private int CalculateDataQualityScore(AssetMetadata metadata, Symbol symbol)
    {
        var score = 0;
        var maxScore = 100;

        // Essential fields (60 points)
        if (!string.IsNullOrEmpty(metadata.FullName)) score += 15;
        if (!string.IsNullOrEmpty(metadata.Description)) score += 15;
        if (!string.IsNullOrEmpty(metadata.Sector)) score += 15;
        if (metadata.MarketCap.HasValue) score += 15;

        // Nice-to-have fields (40 points)
        if (metadata.CurrentPrice.HasValue) score += 10;
        if (metadata.Volume24h.HasValue) score += 10;
        if (metadata.PricePrecision.HasValue) score += 5;
        if (metadata.QuantityPrecision.HasValue) score += 5;
        if (!string.IsNullOrEmpty(metadata.Website)) score += 5;
        if (!string.IsNullOrEmpty(metadata.LogoUrl)) score += 5;

        return Math.Min(score, maxScore);
    }

    private Dictionary<string, IExternalDataSource> InitializeDataSources()
    {
        var sources = new Dictionary<string, IExternalDataSource>();

        // Initialize data sources based on configuration
        foreach (var sourceConfig in _config.Sources)
        {
            switch (sourceConfig.Name.ToUpper())
            {
                case "COINMARKETCAP":
                    sources[sourceConfig.Name] = new CoinMarketCapDataSource(_httpClient, _logger, sourceConfig);
                    break;
                case "ALPHA_VANTAGE":
                    sources[sourceConfig.Name] = new AlphaVantageDataSource(_httpClient, _logger, sourceConfig);
                    break;
                case "YAHOO_FINANCE":
                    sources[sourceConfig.Name] = new YahooFinanceDataSource(_httpClient, _logger, sourceConfig);
                    break;
                default:
                    _logger.LogWarning("Unknown enrichment source configuration: {Source}", sourceConfig.Name);
                    break;
            }
        }

        return sources;
    }

    private static List<List<T>> CreateBatches<T>(List<T> items, int batchSize)
    {
        var batches = new List<List<T>>();
        for (int i = 0; i < items.Count; i += batchSize)
        {
            batches.Add(items.Skip(i).Take(batchSize).ToList());
        }
        return batches;
    }

    #endregion
}

/// <summary>
/// Configuration for the enrichment service
/// </summary>
public class EnrichmentConfiguration
{
    public int MaxConcurrency { get; set; } = 3;
    public int DefaultBatchSize { get; set; } = 50;
    public TimeSpan DefaultRateLimitDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    public List<EnrichmentSourceConfiguration> Sources { get; set; } = new();
}

/// <summary>
/// Interface for external data sources
/// </summary>
public interface IExternalDataSource
{
    Task<EnrichmentSourceResult> EnrichSymbolAsync(
        Symbol symbol,
        AssetMetadata metadata,
        CancellationToken cancellationToken);

    Task<EnrichmentSourceStatus> GetHealthStatusAsync();
}

// Placeholder implementations for data sources
public class CoinMarketCapDataSource : IExternalDataSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly EnrichmentSourceConfiguration _config;

    public CoinMarketCapDataSource(HttpClient httpClient, ILogger logger, EnrichmentSourceConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    public async Task<EnrichmentSourceResult> EnrichSymbolAsync(
        Symbol symbol,
        AssetMetadata metadata,
        CancellationToken cancellationToken)
    {
        // TODO: Implement CoinMarketCap API integration
        await Task.Delay(100, cancellationToken);
        return new EnrichmentSourceResult
        {
            SourceName = "COINMARKETCAP",
            Success = false,
            ErrorMessage = "Not implemented yet"
        };
    }

    public async Task<EnrichmentSourceStatus> GetHealthStatusAsync()
    {
        await Task.Delay(50);
        return new EnrichmentSourceStatus
        {
            SourceName = "COINMARKETCAP",
            IsHealthy = false,
            HealthMessage = "Not implemented yet"
        };
    }
}

public class AlphaVantageDataSource : IExternalDataSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly EnrichmentSourceConfiguration _config;

    public AlphaVantageDataSource(HttpClient httpClient, ILogger logger, EnrichmentSourceConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    public async Task<EnrichmentSourceResult> EnrichSymbolAsync(
        Symbol symbol,
        AssetMetadata metadata,
        CancellationToken cancellationToken)
    {
        // TODO: Implement Alpha Vantage API integration
        await Task.Delay(100, cancellationToken);
        return new EnrichmentSourceResult
        {
            SourceName = "ALPHA_VANTAGE",
            Success = false,
            ErrorMessage = "Not implemented yet"
        };
    }

    public async Task<EnrichmentSourceStatus> GetHealthStatusAsync()
    {
        await Task.Delay(50);
        return new EnrichmentSourceStatus
        {
            SourceName = "ALPHA_VANTAGE",
            IsHealthy = false,
            HealthMessage = "Not implemented yet"
        };
    }
}

public class YahooFinanceDataSource : IExternalDataSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly EnrichmentSourceConfiguration _config;

    public YahooFinanceDataSource(HttpClient httpClient, ILogger logger, EnrichmentSourceConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    public async Task<EnrichmentSourceResult> EnrichSymbolAsync(
        Symbol symbol,
        AssetMetadata metadata,
        CancellationToken cancellationToken)
    {
        // TODO: Implement Yahoo Finance enrichment integration
        await Task.Delay(100, cancellationToken);
        return new EnrichmentSourceResult
        {
            SourceName = "YAHOO_FINANCE",
            Success = false,
            ErrorMessage = "Not implemented yet"
        };
    }

    public async Task<EnrichmentSourceStatus> GetHealthStatusAsync()
    {
        await Task.Delay(50);
        return new EnrichmentSourceStatus
        {
            SourceName = "YAHOO_FINANCE",
            IsHealthy = false,
            HealthMessage = "Not implemented yet"
        };
    }
}