using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.Data;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;

namespace MyTrader.Core.Services;

/// <summary>
/// Daily market data synchronization service for Yahoo Finance
/// Handles batch processing, data validation, and gap detection
/// </summary>
public class YahooFinanceDailyDataService
{
    private readonly ITradingDbContext _dbContext;
    private readonly YahooFinanceApiService _yahooApiService;
    private readonly ILogger<YahooFinanceDailyDataService> _logger;
    private readonly YahooFinanceDailyConfiguration _config;

    public YahooFinanceDailyDataService(
        ITradingDbContext dbContext,
        YahooFinanceApiService yahooApiService,
        ILogger<YahooFinanceDailyDataService> logger,
        IOptions<YahooFinanceDailyConfiguration> configuration)
    {
        _dbContext = dbContext;
        _yahooApiService = yahooApiService;
        _logger = logger;
        _config = configuration.Value;
    }

    /// <summary>
    /// Synchronize daily data for a specific market
    /// </summary>
    public async Task<DailySyncResult> SyncMarketDataAsync(
        string market,
        DateTime? specificDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DailySyncResult
        {
            Market = market,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting daily sync for market {Market}", market);

            // Get active symbols for this market
            var symbols = await GetActiveSymbolsForMarketAsync(market, cancellationToken);
            if (!symbols.Any())
            {
                _logger.LogWarning("No active symbols found for market {Market}", market);
                result.Success = false;
                result.ErrorMessage = "No active symbols found";
                return result;
            }

            _logger.LogInformation("Found {SymbolCount} symbols for market {Market}", symbols.Count, market);

            // Determine date to sync
            var syncDate = specificDate ?? GetDefaultSyncDate(market);
            result.SyncDate = syncDate;

            // Check if market is open for this date
            if (!IsMarketTradingDay(market, syncDate))
            {
                _logger.LogInformation("Market {Market} was closed on {Date}, skipping sync", market, syncDate.ToString("yyyy-MM-dd"));
                result.Success = true;
                result.SkippedReason = "Market closed";
                return result;
            }

            // Process symbols in batches
            var batches = symbols.Chunk(_config.BatchSize).ToList();
            result.TotalBatches = batches.Count;

            foreach (var batch in batches)
            {
                await ProcessBatchAsync(batch, market, syncDate, result, cancellationToken);
                result.ProcessedBatches++;

                // Add delay between batches to respect rate limits
                if (result.ProcessedBatches < result.TotalBatches)
                {
                    await Task.Delay(_config.BatchDelayMs, cancellationToken);
                }
            }

            // Perform post-sync validations
            await PerformPostSyncValidationAsync(market, syncDate, result, cancellationToken);

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation("Daily sync completed for market {Market}. " +
                "Processed: {Processed}, Failed: {Failed}, Duration: {Duration}",
                market, result.SuccessfulSymbols, result.FailedSymbols, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during daily sync for market {Market}", market);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }
    }

    /// <summary>
    /// Detect and fill data gaps for a market
    /// </summary>
    public async Task<DataGapResult> DetectAndFillGapsAsync(
        string market,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var result = new DataGapResult
        {
            Market = market,
            StartDate = startDate,
            EndDate = endDate
        };

        try
        {
            _logger.LogInformation("Detecting data gaps for market {Market} from {StartDate} to {EndDate}",
                market, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var symbols = await GetActiveSymbolsForMarketAsync(market, cancellationToken);
            var gaps = new List<DataGap>();

            foreach (var symbol in symbols)
            {
                var symbolGaps = await DetectSymbolGapsAsync(symbol.Ticker, market, startDate, endDate, cancellationToken);
                gaps.AddRange(symbolGaps);
            }

            result.GapsDetected = gaps.Count;
            _logger.LogInformation("Found {GapCount} data gaps for market {Market}", gaps.Count, market);

            // Fill gaps if configured
            if (_config.AutoFillGaps && gaps.Any())
            {
                var fillTasks = gaps.Take(_config.MaxGapsToFill)
                    .Select(gap => FillDataGapAsync(gap, cancellationToken))
                    .ToList();

                var fillResults = await Task.WhenAll(fillTasks);
                result.GapsFilled = fillResults.Count(r => r);
                result.GapsFailed = fillResults.Count(r => !r);
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting/filling gaps for market {Market}", market);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private async Task ProcessBatchAsync(
        Symbol[] batch,
        string market,
        DateTime syncDate,
        DailySyncResult result,
        CancellationToken cancellationToken)
    {
        var tasks = batch.Select(symbol => ProcessSymbolAsync(symbol, market, syncDate, result, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessSymbolAsync(
        Symbol symbol,
        string market,
        DateTime syncDate,
        DailySyncResult result,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var maxRetries = _config.MaxRetryAttempts;

        while (retryCount <= maxRetries)
        {
            try
            {
                // Check if data already exists
                var existingData = await _dbContext.HistoricalMarketData
                    .Where(h => h.SymbolTicker == symbol.Ticker &&
                               h.MarketCode == market &&
                               h.TradeDate == DateOnly.FromDateTime(syncDate) &&
                               h.DataSource == "YAHOO")
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingData != null && !_config.OverwriteExistingData)
                {
                    _logger.LogDebug("Data already exists for {Symbol} on {Date}, skipping",
                        symbol.Ticker, syncDate.ToString("yyyy-MM-dd"));
                    result.SkippedSymbols++;
                    return;
                }

                // Fetch data from Yahoo Finance
                var apiResult = await _yahooApiService.GetHistoricalDataAsync(
                    symbol.Ticker, syncDate, syncDate, market, cancellationToken);

                if (!apiResult.Success)
                {
                    if (apiResult.IsRetryable && retryCount < maxRetries)
                    {
                        retryCount++;
                        await Task.Delay(_config.RetryDelayMs * retryCount, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Failed to fetch data for {Symbol}: {Error}",
                        symbol.Ticker, apiResult.ErrorMessage);

                    result.FailedSymbols++;
                    result.Errors.Add($"{symbol.Ticker}: {apiResult.ErrorMessage}");
                    return;
                }

                if (apiResult.Data?.Any() == true)
                {
                    // Update symbol reference
                    foreach (var record in apiResult.Data)
                    {
                        record.SymbolId = symbol.Id;
                    }

                    // Validate data quality
                    var validRecords = apiResult.Data.Where(r => ValidateRecord(r, result)).ToList();

                    if (validRecords.Any())
                    {
                        if (existingData != null)
                        {
                            _dbContext.HistoricalMarketData.Remove(existingData);
                        }

                        await _dbContext.HistoricalMarketData.AddRangeAsync(validRecords, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        _logger.LogDebug("Saved {RecordCount} records for {Symbol}",
                            validRecords.Count, symbol.Ticker);

                        result.SuccessfulSymbols++;
                        result.TotalRecordsProcessed += validRecords.Count;
                    }
                    else
                    {
                        _logger.LogWarning("No valid records for {Symbol} after validation", symbol.Ticker);
                        result.FailedSymbols++;
                    }
                }
                else
                {
                    _logger.LogDebug("No data returned for {Symbol} on {Date}",
                        symbol.Ticker, syncDate.ToString("yyyy-MM-dd"));
                    result.NoDataSymbols++;
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

                _logger.LogError(ex, "Failed to process {Symbol} after {RetryCount} retries",
                    symbol.Ticker, retryCount);
                result.FailedSymbols++;
                result.Errors.Add($"{symbol.Ticker}: {ex.Message}");
                return;
            }
        }
    }

    private bool ValidateRecord(HistoricalMarketData record, DailySyncResult result)
    {
        var errors = new List<string>();

        // Basic data validation
        if (!record.ClosePrice.HasValue || record.ClosePrice <= 0)
        {
            errors.Add("Invalid close price");
        }

        if (record.Volume.HasValue && record.Volume < 0)
        {
            errors.Add("Negative volume");
        }

        if (record.HighPrice.HasValue && record.LowPrice.HasValue && record.HighPrice < record.LowPrice)
        {
            errors.Add("High price less than low price");
        }

        if (errors.Any())
        {
            _logger.LogWarning("Data validation failed for {Symbol} on {Date}: {Errors}",
                record.SymbolTicker, record.TradeDate, string.Join(", ", errors));
            result.ValidationErrors.AddRange(errors);
            return false;
        }

        return true;
    }

    private async Task<List<Symbol>> GetActiveSymbolsForMarketAsync(string market, CancellationToken cancellationToken)
    {
        var query = _dbContext.Symbols.Where(s => s.IsActive);

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

        return await query.ToListAsync(cancellationToken);
    }

    private DateTime GetDefaultSyncDate(string market)
    {
        var now = DateTime.UtcNow;

        return market.ToUpper() switch
        {
            "BIST" => now.Date.AddDays(-1), // Previous day (after 18:30 TR time)
            "NASDAQ" or "NYSE" => now.Date.AddDays(-1), // Previous day (after 16:30 ET)
            "CRYPTO" => now.Date.AddDays(-1), // Previous day (daily at 00:01)
            _ => now.Date.AddDays(-1)
        };
    }

    private bool IsMarketTradingDay(string market, DateTime date)
    {
        var dayOfWeek = date.DayOfWeek;

        return market.ToUpper() switch
        {
            "BIST" => dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday,
            "NASDAQ" or "NYSE" => dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday,
            "CRYPTO" => true, // 24/7
            _ => dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday
        };
    }

    private async Task PerformPostSyncValidationAsync(string market, DateTime syncDate, DailySyncResult result, CancellationToken cancellationToken)
    {
        try
        {
            // Check data completeness
            var expectedSymbols = await GetActiveSymbolsForMarketAsync(market, cancellationToken);
            var actualRecords = await _dbContext.HistoricalMarketData
                .Where(h => h.MarketCode == market &&
                           h.TradeDate == DateOnly.FromDateTime(syncDate) &&
                           h.DataSource == "YAHOO")
                .CountAsync(cancellationToken);

            result.DataCompletenessPercent = expectedSymbols.Count > 0 ?
                (decimal)actualRecords / expectedSymbols.Count * 100 : 100;

            _logger.LogInformation("Data completeness for {Market} on {Date}: {Percentage:F1}% ({Actual}/{Expected})",
                market, syncDate.ToString("yyyy-MM-dd"), result.DataCompletenessPercent, actualRecords, expectedSymbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during post-sync validation for {Market}", market);
        }
    }

    private async Task<List<DataGap>> DetectSymbolGapsAsync(
        string symbolTicker,
        string market,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var gaps = new List<DataGap>();

        var existingDates = await _dbContext.HistoricalMarketData
            .Where(h => h.SymbolTicker == symbolTicker &&
                       h.MarketCode == market &&
                       h.TradeDate >= DateOnly.FromDateTime(startDate) &&
                       h.TradeDate <= DateOnly.FromDateTime(endDate))
            .Select(h => h.TradeDate)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            if (IsMarketTradingDay(market, currentDate) &&
                !existingDates.Contains(DateOnly.FromDateTime(currentDate)))
            {
                gaps.Add(new DataGap
                {
                    SymbolTicker = symbolTicker,
                    Market = market,
                    MissingDate = currentDate
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        return gaps;
    }

    private async Task<bool> FillDataGapAsync(DataGap gap, CancellationToken cancellationToken)
    {
        try
        {
            var apiResult = await _yahooApiService.GetHistoricalDataAsync(
                gap.SymbolTicker, gap.MissingDate, gap.MissingDate, gap.Market, cancellationToken);

            if (apiResult.Success && apiResult.Data?.Any() == true)
            {
                await _dbContext.HistoricalMarketData.AddRangeAsync(apiResult.Data, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Filled data gap for {Symbol} on {Date}",
                    gap.SymbolTicker, gap.MissingDate.ToString("yyyy-MM-dd"));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filling data gap for {Symbol} on {Date}",
                gap.SymbolTicker, gap.MissingDate.ToString("yyyy-MM-dd"));
            return false;
        }
    }
}

/// <summary>
/// Configuration for Yahoo Finance daily data service
/// </summary>
public class YahooFinanceDailyConfiguration
{
    public int BatchSize { get; set; } = 10;
    public int BatchDelayMs { get; set; } = 1000;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 2000;
    public bool OverwriteExistingData { get; set; } = false;
    public bool AutoFillGaps { get; set; } = true;
    public int MaxGapsToFill { get; set; } = 100;
    public decimal MinDataCompletenessPercent { get; set; } = 90.0m;
}

/// <summary>
/// Result of daily synchronization operation
/// </summary>
public class DailySyncResult
{
    public string Market { get; set; } = string.Empty;
    public DateTime? SyncDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SkippedReason { get; set; }

    public int TotalBatches { get; set; }
    public int ProcessedBatches { get; set; }
    public int SuccessfulSymbols { get; set; }
    public int FailedSymbols { get; set; }
    public int SkippedSymbols { get; set; }
    public int NoDataSymbols { get; set; }
    public int TotalRecordsProcessed { get; set; }

    public decimal DataCompletenessPercent { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// Result of data gap detection and filling
/// </summary>
public class DataGapResult
{
    public string Market { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int GapsDetected { get; set; }
    public int GapsFilled { get; set; }
    public int GapsFailed { get; set; }
}

/// <summary>
/// Represents a missing data point
/// </summary>
public class DataGap
{
    public string SymbolTicker { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public DateTime MissingDate { get; set; }
}