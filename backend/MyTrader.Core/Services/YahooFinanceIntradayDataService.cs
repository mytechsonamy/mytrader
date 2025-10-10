using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.Data;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;
using MyTrader.Core.Interfaces;

namespace MyTrader.Core.Services;

/// <summary>
/// Intraday market data synchronization service for Yahoo Finance
/// Handles 5-minute interval data collection with market hours awareness
/// </summary>
public class YahooFinanceIntradayDataService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly YahooFinanceApiService _yahooApiService;
    private readonly IAlpacaMarketDataService _alpacaService;
    private readonly ILogger<YahooFinanceIntradayDataService> _logger;
    private readonly YahooFinanceIntradayConfiguration _config;

    public YahooFinanceIntradayDataService(
        IServiceProvider serviceProvider,
        YahooFinanceApiService yahooApiService,
        IAlpacaMarketDataService alpacaService,
        ILogger<YahooFinanceIntradayDataService> logger,
        IOptions<YahooFinanceIntradayConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _yahooApiService = yahooApiService;
        _alpacaService = alpacaService;
        _logger = logger;
        _config = configuration.Value;
    }

    /// <summary>
    /// Synchronize 5-minute intraday data for all symbols in specified markets
    /// </summary>
    public async Task<IntradaySyncResult> SyncIntradayDataAsync(
        string[]? specificMarkets = null,
        DateTime? specificTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        var result = new IntradaySyncResult
        {
            StartTime = DateTime.UtcNow,
            Interval = "5m"
        };

        try
        {
            _logger.LogInformation("Starting 5-minute intraday sync");

            // Determine markets to sync
            var marketsToSync = specificMarkets ?? GetActiveMarkets();
            result.Markets = marketsToSync;

            var marketResults = new List<MarketSyncResult>();

            foreach (var market in marketsToSync)
            {
                var marketResult = await SyncMarketIntradayDataAsync(market, specificTimestamp, cancellationToken);
                marketResults.Add(marketResult);

                // Add small delay between markets
                if (market != marketsToSync.Last())
                {
                    await Task.Delay(_config.InterMarketDelayMs, cancellationToken);
                }
            }

            // Aggregate results
            result.TotalSymbolsProcessed = marketResults.Sum(m => m.SuccessfulSymbols + m.FailedSymbols);
            result.SuccessfulSymbols = marketResults.Sum(m => m.SuccessfulSymbols);
            result.FailedSymbols = marketResults.Sum(m => m.FailedSymbols);
            result.SkippedSymbols = marketResults.Sum(m => m.SkippedSymbols);
            result.TotalRecordsProcessed = marketResults.Sum(m => m.TotalRecordsProcessed);
            result.MarketResults = marketResults;

            result.Success = result.FailedSymbols == 0 ||
                           (decimal)result.SuccessfulSymbols / result.TotalSymbolsProcessed >= _config.MinSuccessRatePercent / 100;

            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation("5-minute intraday sync completed. " +
                "Processed: {Processed}, Successful: {Successful}, Failed: {Failed}, Duration: {Duration}",
                result.TotalSymbolsProcessed, result.SuccessfulSymbols, result.FailedSymbols, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 5-minute intraday sync");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }
    }

    /// <summary>
    /// Synchronize intraday data for a specific market
    /// </summary>
    private async Task<MarketSyncResult> SyncMarketIntradayDataAsync(
        string market,
        DateTime? specificTimestamp,
        CancellationToken cancellationToken)
    {
        var marketResult = new MarketSyncResult
        {
            Market = market,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Check if market is currently in trading hours
            var currentTime = specificTimestamp ?? DateTime.UtcNow;
            if (!IsMarketInTradingHours(market, currentTime))
            {
                _logger.LogDebug("Market {Market} is not in trading hours at {Time}, skipping sync",
                    market, currentTime.ToString("yyyy-MM-dd HH:mm:ss"));

                marketResult.Success = true;
                marketResult.SkippedReason = "Market closed";
                return marketResult;
            }

            // Get active symbols for this market
            var symbols = await GetActiveSymbolsForMarketAsync(market, cancellationToken);
            if (!symbols.Any())
            {
                _logger.LogWarning("No active symbols found for market {Market}", market);
                marketResult.Success = false;
                marketResult.ErrorMessage = "No active symbols found";
                return marketResult;
            }

            _logger.LogDebug("Found {SymbolCount} symbols for market {Market}", symbols.Count, market);

            // Calculate time range for data collection
            var (startTime, endTime) = GetDataTimeRange(market, currentTime);
            marketResult.DataStartTime = startTime;
            marketResult.DataEndTime = endTime;

            // Process symbols in batches
            var batches = symbols.Chunk(_config.BatchSize).ToList();
            marketResult.TotalBatches = batches.Count;

            foreach (var batch in batches)
            {
                await ProcessIntradayBatchAsync(batch, market, startTime, endTime, marketResult, cancellationToken);
                marketResult.ProcessedBatches++;

                // Add delay between batches
                if (marketResult.ProcessedBatches < marketResult.TotalBatches)
                {
                    await Task.Delay(_config.BatchDelayMs, cancellationToken);
                }
            }

            marketResult.Success = true;
            marketResult.EndTime = DateTime.UtcNow;
            marketResult.Duration = marketResult.EndTime - marketResult.StartTime;

            _logger.LogDebug("Market {Market} sync completed. " +
                "Successful: {Successful}, Failed: {Failed}, Records: {Records}",
                market, marketResult.SuccessfulSymbols, marketResult.FailedSymbols, marketResult.TotalRecordsProcessed);

            return marketResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing market {Market}", market);
            marketResult.Success = false;
            marketResult.ErrorMessage = ex.Message;
            marketResult.EndTime = DateTime.UtcNow;
            marketResult.Duration = marketResult.EndTime - marketResult.StartTime;
            return marketResult;
        }
    }

    /// <summary>
    /// Process a batch of symbols for intraday data collection
    /// </summary>
    private async Task ProcessIntradayBatchAsync(
        Symbol[] batch,
        string market,
        DateTime startTime,
        DateTime endTime,
        MarketSyncResult marketResult,
        CancellationToken cancellationToken)
    {
        // Process symbols sequentially to avoid DbContext threading issues
        foreach (var symbol in batch)
        {
            await ProcessSymbolIntradayDataAsync(symbol, market, startTime, endTime, marketResult, cancellationToken);
        }
    }

    /// <summary>
    /// Process intraday data for a single symbol
    /// </summary>
    private async Task ProcessSymbolIntradayDataAsync(
        Symbol symbol,
        string market,
        DateTime startTime,
        DateTime endTime,
        MarketSyncResult marketResult,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var maxRetries = _config.MaxRetryAttempts;

        while (retryCount <= maxRetries)
        {
            try
            {
                // Create a new scope for this symbol to avoid DbContext threading issues
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

                // Normalize symbol ticker for API calls
                var normalizedSymbol = NormalizeSymbolForMarket(symbol.Ticker, market);

                // Check if recent data already exists to avoid duplicates
                var recentDataExists = await CheckRecentDataExistsAsync(dbContext, symbol.Ticker, market, endTime, cancellationToken);
                if (recentDataExists && !_config.OverwriteExistingData)
                {
                    _logger.LogDebug("Recent 5-minute data already exists for {Symbol}, skipping",
                        symbol.Ticker);
                    marketResult.SkippedSymbols++;
                    return;
                }

                // Fetch intraday data - use Alpaca for CRYPTO, Yahoo Finance for other markets
                List<HistoricalMarketData>? dataList = null;
                bool success = false;
                string? errorMessage = null;

                if (market.ToUpper() == "CRYPTO")
                {
                    // Use Alpaca for crypto data
                    try
                    {
                        _logger.LogDebug("Fetching Alpaca crypto data for {Symbol} (normalized: {Normalized})",
                            symbol.Ticker, normalizedSymbol);

                        var alpacaResult = await _alpacaService.GetHistoricalDataAsync(
                            normalizedSymbol, "5m", startTime, endTime, cancellationToken);

                        if (alpacaResult != null && alpacaResult.Candles.Any())
                        {
                            dataList = alpacaResult.Candles.Select(bar => new HistoricalMarketData
                            {
                                Timestamp = bar.OpenTime,
                                OpenPrice = bar.Open,
                                HighPrice = bar.High,
                                LowPrice = bar.Low,
                                ClosePrice = bar.Close,
                                Volume = bar.Volume
                            }).ToList();
                            success = true;
                            _logger.LogDebug("Successfully fetched {Count} records from Alpaca for {Symbol}",
                                dataList.Count, symbol.Ticker);
                        }
                        else
                        {
                            errorMessage = "No data received from Alpaca API";
                            _logger.LogWarning("No data returned from Alpaca for {Symbol}", symbol.Ticker);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMessage = $"Alpaca API error: {ex.Message}";
                        _logger.LogWarning("Alpaca API error for {Symbol}: {Error}", symbol.Ticker, ex.Message);
                    }
                }
                else
                {
                    // Use Yahoo Finance for other markets
                    var apiResult = await _yahooApiService.GetIntradayDataAsync(
                        normalizedSymbol, startTime, endTime, "5m", market, cancellationToken);
                    success = apiResult.Success;
                    errorMessage = apiResult.ErrorMessage;
                    dataList = apiResult.Data;
                }

                if (!success)
                {
                    // Handle retry logic for both Alpaca and Yahoo Finance
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        await Task.Delay(_config.RetryDelayMs * retryCount, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Failed to fetch 5-minute data for {Symbol}: {Error}",
                        symbol.Ticker, errorMessage);

                    marketResult.FailedSymbols++;
                    marketResult.Errors.Add($"{symbol.Ticker}: {errorMessage}");
                    return;
                }

                if (dataList?.Any() == true)
                {
                    // Convert HistoricalMarketData to MarketData
                    var convertedRecords = dataList.Select(h => ConvertToMarketData(h, symbol.Ticker, market)).ToList();

                    // Validate and filter data
                    var validRecords = new List<MarketData>();
                    foreach (var record in convertedRecords)
                    {
                        if (!ValidateIntradayRecord(record, marketResult))
                            continue;

                        if (_config.OverwriteExistingData || !await CheckRecordExistsAsync(dbContext, record, cancellationToken))
                        {
                            validRecords.Add(record);
                        }
                    }

                    if (validRecords.Any())
                    {
                        // Remove existing records if overwrite is enabled
                        if (_config.OverwriteExistingData)
                        {
                            await RemoveExistingRecordsAsync(dbContext, symbol.Ticker, market, startTime, endTime, "5MIN", cancellationToken);
                        }

                        await dbContext.MarketData.AddRangeAsync(validRecords, cancellationToken);
                        await dbContext.SaveChangesAsync(cancellationToken);

                        _logger.LogDebug("Saved {RecordCount} 5-minute records for {Symbol}",
                            validRecords.Count, symbol.Ticker);

                        marketResult.SuccessfulSymbols++;
                        marketResult.TotalRecordsProcessed += validRecords.Count;
                    }
                    else
                    {
                        _logger.LogDebug("No new valid 5-minute records for {Symbol}", symbol.Ticker);
                        marketResult.SkippedSymbols++;
                    }
                }
                else
                {
                    _logger.LogDebug("No 5-minute data returned for {Symbol} in time range {Start} to {End}",
                        symbol.Ticker, startTime.ToString("yyyy-MM-dd HH:mm"), endTime.ToString("yyyy-MM-dd HH:mm"));
                    marketResult.NoDataSymbols++;
                }

                return; // Success, exit retry loop
            }
            catch (Exception ex)
            {
                if (retryCount < maxRetries)
                {
                    retryCount++;
                    _logger.LogWarning("Retry {RetryCount}/{MaxRetries} for {Symbol}: {Error}",
                        retryCount, maxRetries, symbol.Ticker, ex.Message);
                    await Task.Delay(_config.RetryDelayMs * retryCount, cancellationToken);
                    continue;
                }

                _logger.LogError(ex, "Failed to process 5-minute data for {Symbol} after {RetryCount} retries",
                    symbol.Ticker, retryCount);
                marketResult.FailedSymbols++;
                marketResult.Errors.Add($"{symbol.Ticker}: {ex.Message}");
                return;
            }
        }
    }

    /// <summary>
    /// Check if market is currently in trading hours
    /// </summary>
    private bool IsMarketInTradingHours(string market, DateTime currentTime)
    {
        return market.ToUpper() switch
        {
            "BIST" => IsInBistTradingHours(currentTime),
            "NYSE" or "NASDAQ" => IsInUSMarketTradingHours(currentTime),
            "CRYPTO" => true, // 24/7
            _ => false
        };
    }

    /// <summary>
    /// Check if current time is within BIST trading hours (10:00-18:00 Turkey Time, Mon-Fri)
    /// </summary>
    private bool IsInBistTradingHours(DateTime utcTime)
    {
        try
        {
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, turkeyTimeZone);

            // Check if it's a weekday
            if (turkeyTime.DayOfWeek == DayOfWeek.Saturday || turkeyTime.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // Check if it's within trading hours (10:00-18:00)
            var marketOpen = new TimeOnly(10, 0);
            var marketClose = new TimeOnly(18, 0);
            var currentTime = TimeOnly.FromDateTime(turkeyTime);

            return currentTime >= marketOpen && currentTime <= marketClose;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking BIST trading hours, defaulting to false");
            return false;
        }
    }

    /// <summary>
    /// Check if current time is within US market trading hours (9:30-16:00 ET, Mon-Fri)
    /// </summary>
    private bool IsInUSMarketTradingHours(DateTime utcTime)
    {
        try
        {
            var etTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var etTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, etTimeZone);

            // Check if it's a weekday
            if (etTime.DayOfWeek == DayOfWeek.Saturday || etTime.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // Check if it's within trading hours (9:30-16:00)
            var marketOpen = new TimeOnly(9, 30);
            var marketClose = new TimeOnly(16, 0);
            var currentTime = TimeOnly.FromDateTime(etTime);

            return currentTime >= marketOpen && currentTime <= marketClose;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking US market trading hours, defaulting to false");
            return false;
        }
    }

    /// <summary>
    /// Get the time range for data collection (typically last few minutes to current time)
    /// </summary>
    private (DateTime startTime, DateTime endTime) GetDataTimeRange(string market, DateTime currentTime)
    {
        // For 5-minute intervals, collect data from the last collection period
        var endTime = currentTime;
        var startTime = currentTime.AddMinutes(-_config.LookbackMinutes);

        return (startTime, endTime);
    }

    /// <summary>
    /// Get active markets for processing
    /// </summary>
    private string[] GetActiveMarkets()
    {
        var activeMarkets = new List<string>();

        if (_config.EnableBistSync) activeMarkets.Add("BIST");
        if (_config.EnableUSMarketsSync)
        {
            activeMarkets.Add("NYSE");
            activeMarkets.Add("NASDAQ");
        }
        if (_config.EnableCryptoSync) activeMarkets.Add("CRYPTO");

        return activeMarkets.ToArray();
    }

    /// <summary>
    /// Get active symbols for a specific market
    /// </summary>
    private async Task<List<Symbol>> GetActiveSymbolsForMarketAsync(string market, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

        var query = dbContext.Symbols.Where(s => s.IsActive);

        // Filter by market-specific criteria
        switch (market.ToUpper())
        {
            case "BIST":
                query = query.Where(s => (s.Market != null && s.Market.Code == "BIST") || s.AssetClass == "BIST");
                break;
            case "NASDAQ":
                query = query.Where(s => (s.Market != null && s.Market.Code == "NASDAQ") || s.Venue == "NASDAQ");
                break;
            case "NYSE":
                query = query.Where(s => (s.Market != null && s.Market.Code == "NYSE") || s.Venue == "NYSE");
                break;
            case "CRYPTO":
                query = query.Where(s => s.AssetClass == "CRYPTO" || (s.Market != null && s.Market.Code == "CRYPTO"));
                break;
        }

        return await query.Take(_config.MaxSymbolsPerMarket).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Validate intraday record data quality
    /// </summary>
    private bool ValidateIntradayRecord(MarketData record, MarketSyncResult marketResult)
    {
        var errors = new List<string>();

        // Basic validation
        if (record.Close <= 0)
        {
            errors.Add("Invalid close price");
        }

        if (record.Volume < 0)
        {
            errors.Add("Negative volume");
        }

        if (record.High < record.Low)
        {
            errors.Add("High price less than low price");
        }

        // Check timestamp is reasonable
        var timeDiff = Math.Abs((DateTime.UtcNow - record.Timestamp).TotalHours);
        if (timeDiff > 24)
        {
            errors.Add("Timestamp too old or in future");
        }

        if (errors.Any())
        {
            _logger.LogWarning("Intraday data validation failed for {Symbol} at {Timestamp}: {Errors}",
                record.Symbol, record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), string.Join(", ", errors));
            marketResult.ValidationErrors.AddRange(errors);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if recent data already exists for a symbol
    /// </summary>
    private async Task<bool> CheckRecentDataExistsAsync(ITradingDbContext dbContext, string symbolTicker, string market, DateTime endTime, CancellationToken cancellationToken)
    {
        var cutoffTime = endTime.AddMinutes(-10); // Check for data in last 10 minutes

        return await dbContext.MarketData
            .AnyAsync(h => h.Symbol == symbolTicker &&
                          h.Timeframe == "5MIN" &&
                          h.Timestamp >= cutoffTime, cancellationToken);
    }

    /// <summary>
    /// Check if a specific record already exists
    /// </summary>
    private async Task<bool> CheckRecordExistsAsync(ITradingDbContext dbContext, MarketData record, CancellationToken cancellationToken)
    {
        return await dbContext.MarketData
            .AnyAsync(h => h.Symbol == record.Symbol &&
                          h.Timeframe == record.Timeframe &&
                          h.Timestamp == record.Timestamp, cancellationToken);
    }

    /// <summary>
    /// Remove existing records in the time range (for overwrite scenarios)
    /// </summary>
    private async Task RemoveExistingRecordsAsync(
        ITradingDbContext dbContext,
        string symbolTicker,
        string market,
        DateTime startTime,
        DateTime endTime,
        string timeframe,
        CancellationToken cancellationToken)
    {
        var existingRecords = await dbContext.MarketData
            .Where(h => h.Symbol == symbolTicker &&
                       h.Timeframe == timeframe &&
                       h.Timestamp >= startTime &&
                       h.Timestamp <= endTime)
            .ToListAsync(cancellationToken);

        if (existingRecords.Any())
        {
            dbContext.MarketData.RemoveRange(existingRecords);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Removed {Count} existing records for {Symbol} in timeframe {Timeframe}",
                existingRecords.Count, symbolTicker, timeframe);
        }
    }

    /// <summary>
    /// Convert HistoricalMarketData to MarketData
    /// </summary>
    private MarketData ConvertToMarketData(HistoricalMarketData historical, string symbolTicker, string market)
    {
        return new MarketData
        {
            Id = Guid.NewGuid(),
            Symbol = symbolTicker,
            Timeframe = "5MIN",
            Timestamp = historical.Timestamp ?? DateTime.UtcNow,
            Open = historical.OpenPrice ?? 0,
            High = historical.HighPrice ?? 0,
            Low = historical.LowPrice ?? 0,
            Close = historical.ClosePrice ?? 0,
            Volume = historical.Volume ?? 0,
            AssetClass = market
        };
    }

    /// <summary>
    /// Normalize symbol format for different market APIs
    /// </summary>
    private string NormalizeSymbolForMarket(string originalSymbol, string market)
    {
        var normalizedSymbol = originalSymbol;

        if (market.ToUpper() == "CRYPTO")
        {
            // Handle crypto symbol formats
            // Convert ETH-USD-USD to ETH-USD
            // Convert ETHUSD to ETH-USD if needed
            if (normalizedSymbol.Contains("-USD-USD"))
            {
                normalizedSymbol = normalizedSymbol.Replace("-USD-USD", "-USD");
            }
            else if (normalizedSymbol.Contains("USD") && !normalizedSymbol.Contains("-"))
            {
                // Convert ETHUSD to ETH-USD
                var baseSymbol = normalizedSymbol.Replace("USD", "");
                if (!string.IsNullOrEmpty(baseSymbol) && baseSymbol.Length >= 2)
                {
                    normalizedSymbol = $"{baseSymbol}-USD";
                }
            }
            // Handle other USD pairs like BTCUSD -> BTC-USD
            else if (normalizedSymbol.EndsWith("USD") && !normalizedSymbol.Contains("-") && normalizedSymbol.Length > 3)
            {
                var baseSymbol = normalizedSymbol[..^3]; // Remove "USD" from the end
                normalizedSymbol = $"{baseSymbol}-USD";
            }

            // Log only if normalization actually changed the symbol
            if (normalizedSymbol != originalSymbol)
            {
                _logger.LogDebug("Normalized crypto symbol: {Original} -> {Normalized}",
                    originalSymbol, normalizedSymbol);
            }
        }

        return normalizedSymbol;
    }
}

/// <summary>
/// Configuration for Yahoo Finance intraday data service
/// </summary>
public class YahooFinanceIntradayConfiguration
{
    public int BatchSize { get; set; } = 5; // Smaller batches for more frequent updates
    public int BatchDelayMs { get; set; } = 500; // Faster processing for real-time data
    public int InterMarketDelayMs { get; set; } = 1000;
    public int MaxRetryAttempts { get; set; } = 2; // Less retries for time-sensitive data
    public int RetryDelayMs { get; set; } = 1000;
    public bool OverwriteExistingData { get; set; } = false;
    public int LookbackMinutes { get; set; } = 15; // How far back to collect data
    public int MaxSymbolsPerMarket { get; set; } = 1000;
    public decimal MinSuccessRatePercent { get; set; } = 70.0m;

    // Market enablement flags
    public bool EnableBistSync { get; set; } = true;
    public bool EnableUSMarketsSync { get; set; } = true;
    public bool EnableCryptoSync { get; set; } = true;
}

/// <summary>
/// Result of intraday synchronization operation
/// </summary>
public class IntradaySyncResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string Interval { get; set; } = "5m";
    public string[] Markets { get; set; } = Array.Empty<string>();

    public int TotalSymbolsProcessed { get; set; }
    public int SuccessfulSymbols { get; set; }
    public int FailedSymbols { get; set; }
    public int SkippedSymbols { get; set; }
    public int TotalRecordsProcessed { get; set; }

    public List<MarketSyncResult> MarketResults { get; set; } = new();
}

/// <summary>
/// Result of market-specific synchronization
/// </summary>
public class MarketSyncResult
{
    public string Market { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SkippedReason { get; set; }

    public DateTime? DataStartTime { get; set; }
    public DateTime? DataEndTime { get; set; }

    public int TotalBatches { get; set; }
    public int ProcessedBatches { get; set; }
    public int SuccessfulSymbols { get; set; }
    public int FailedSymbols { get; set; }
    public int SkippedSymbols { get; set; }
    public int NoDataSymbols { get; set; }
    public int TotalRecordsProcessed { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}