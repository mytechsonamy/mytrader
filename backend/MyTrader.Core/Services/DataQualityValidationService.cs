using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.Data;
using MyTrader.Core.Models;

namespace MyTrader.Core.Services;

/// <summary>
/// Data quality validation and monitoring service for market data
/// Provides comprehensive validation, scoring, and alerting capabilities
/// </summary>
public class DataQualityValidationService
{
    private readonly ITradingDbContext _dbContext;
    private readonly ILogger<DataQualityValidationService> _logger;
    private readonly DataQualityConfiguration _config;

    public DataQualityValidationService(
        ITradingDbContext dbContext,
        ILogger<DataQualityValidationService> logger,
        IOptions<DataQualityConfiguration> configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _config = configuration.Value;
    }

    /// <summary>
    /// Validate market data quality for a specific date and market
    /// </summary>
    public async Task<DataQualityReport> ValidateMarketDataAsync(
        string market,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var report = new DataQualityReport
        {
            Market = market,
            ValidationDate = date,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting data quality validation for {Market} on {Date}",
                market, date.ToString("yyyy-MM-dd"));

            // Get all records for the date and market
            var records = await _dbContext.HistoricalMarketData
                .Where(h => h.MarketCode == market && h.TradeDate == date)
                .ToListAsync(cancellationToken);

            if (!records.Any())
            {
                report.Issues.Add(new DataQualityIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = IssueCategory.Completeness,
                    Message = "No data found for the specified date and market",
                    AffectedSymbolsCount = 0
                });

                report.OverallScore = 0;
                return report;
            }

            report.TotalRecordsValidated = records.Count;

            // Run all validation checks
            await ValidateCompletenessAsync(report, records, market, date, cancellationToken);
            ValidateAccuracy(report, records);
            ValidateConsistency(report, records);
            ValidateIntegrity(report, records);
            await ValidateTimeliness(report, records, cancellationToken);
            await ValidateDuplicates(report, records, cancellationToken);

            // Calculate overall score
            report.OverallScore = CalculateOverallScore(report);
            report.EndTime = DateTime.UtcNow;
            report.Duration = report.EndTime - report.StartTime;

            // Generate recommendations
            GenerateRecommendations(report);

            _logger.LogInformation("Data quality validation completed for {Market} on {Date}. " +
                "Score: {Score}, Issues: {IssueCount}, Duration: {Duration}",
                market, date.ToString("yyyy-MM-dd"), report.OverallScore, report.Issues.Count, report.Duration);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data quality validation for {Market} on {Date}", market, date);
            report.Issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Critical,
                Category = IssueCategory.SystemError,
                Message = $"Validation failed: {ex.Message}"
            });
            report.OverallScore = 0;
            return report;
        }
    }

    /// <summary>
    /// Validate data completeness across multiple dates
    /// </summary>
    public async Task<DataCompletenessReport> ValidateDataCompletenessAsync(
        string market,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var report = new DataCompletenessReport
        {
            Market = market,
            StartDate = startDate,
            EndDate = endDate
        };

        try
        {
            // Get expected symbols for this market
            var expectedSymbols = await GetExpectedSymbolsAsync(market, cancellationToken);
            var expectedSymbolCount = expectedSymbols.Count;

            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                if (IsExpectedTradingDay(market, currentDate))
                {
                    var actualCount = await _dbContext.HistoricalMarketData
                        .Where(h => h.MarketCode == market && h.TradeDate == currentDate)
                        .CountAsync(cancellationToken);

                    var completeness = expectedSymbolCount > 0 ?
                        (decimal)actualCount / expectedSymbolCount * 100 : 100;

                    report.DailyCompleteness.Add(new DailyCompletenessEntry
                    {
                        Date = currentDate,
                        ExpectedRecords = expectedSymbolCount,
                        ActualRecords = actualCount,
                        CompletenessPercent = completeness
                    });

                    if (completeness < _config.MinCompletenessThreshold)
                    {
                        report.IncompleteDays.Add(currentDate);
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            report.AverageCompleteness = report.DailyCompleteness.Any() ?
                report.DailyCompleteness.Average(d => d.CompletenessPercent) : 0;

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data completeness for {Market}", market);
            throw;
        }
    }

    private async Task ValidateCompletenessAsync(
        DataQualityReport report,
        List<HistoricalMarketData> records,
        string market,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        // Check symbol coverage
        var expectedSymbols = await GetExpectedSymbolsAsync(market, cancellationToken);
        var actualSymbols = records.Select(r => r.SymbolTicker).Distinct().ToList();
        var missingSymbols = expectedSymbols.Where(s => !actualSymbols.Contains(s.Ticker)).ToList();

        if (missingSymbols.Any())
        {
            var severity = missingSymbols.Count > expectedSymbols.Count * 0.1 ?
                IssueSeverity.Critical : IssueSeverity.Warning;

            report.Issues.Add(new DataQualityIssue
            {
                Severity = severity,
                Category = IssueCategory.Completeness,
                Message = $"Missing data for {missingSymbols.Count} symbols",
                AffectedSymbols = missingSymbols.Select(s => s.Ticker).ToList(),
                AffectedSymbolsCount = missingSymbols.Count
            });
        }

        // Check field completeness
        var incompleteRecords = records.Where(r =>
            !r.ClosePrice.HasValue || !r.Volume.HasValue ||
            !r.OpenPrice.HasValue || !r.HighPrice.HasValue || !r.LowPrice.HasValue).ToList();

        if (incompleteRecords.Any())
        {
            report.Issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Completeness,
                Message = $"Incomplete OHLCV data for {incompleteRecords.Count} records",
                AffectedSymbols = incompleteRecords.Select(r => r.SymbolTicker).Distinct().ToList(),
                AffectedSymbolsCount = incompleteRecords.Count
            });
        }
    }

    private void ValidateAccuracy(DataQualityReport report, List<HistoricalMarketData> records)
    {
        var issues = new List<DataQualityIssue>();

        // Price validation
        var invalidPrices = records.Where(r =>
            r.ClosePrice <= 0 || r.OpenPrice <= 0 ||
            r.HighPrice <= 0 || r.LowPrice <= 0).ToList();

        if (invalidPrices.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Critical,
                Category = IssueCategory.Accuracy,
                Message = $"Invalid prices (≤ 0) found in {invalidPrices.Count} records",
                AffectedSymbols = invalidPrices.Select(r => r.SymbolTicker).Distinct().ToList(),
                AffectedSymbolsCount = invalidPrices.Count
            });
        }

        // Volume validation
        var negativeVolumes = records.Where(r => r.Volume < 0).ToList();
        if (negativeVolumes.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Accuracy,
                Message = $"Negative volume found in {negativeVolumes.Count} records",
                AffectedSymbols = negativeVolumes.Select(r => r.SymbolTicker).Distinct().ToList(),
                AffectedSymbolsCount = negativeVolumes.Count
            });
        }

        // Extreme price movements (> 50% in a day)
        var extremeMovements = records.Where(r =>
            r.PriceChangePercent.HasValue && Math.Abs(r.PriceChangePercent.Value) > 50).ToList();

        if (extremeMovements.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Info,
                Category = IssueCategory.Accuracy,
                Message = $"Extreme price movements (>50%) in {extremeMovements.Count} records",
                AffectedSymbols = extremeMovements.Select(r => r.SymbolTicker).Distinct().ToList(),
                AffectedSymbolsCount = extremeMovements.Count
            });
        }

        report.Issues.AddRange(issues);
    }

    private void ValidateConsistency(DataQualityReport report, List<HistoricalMarketData> records)
    {
        var issues = new List<DataQualityIssue>();

        // OHLC consistency (High ≥ Low, Open/Close within High/Low range)
        var inconsistentOHLC = records.Where(r =>
            r.HighPrice < r.LowPrice ||
            r.OpenPrice > r.HighPrice || r.OpenPrice < r.LowPrice ||
            r.ClosePrice > r.HighPrice || r.ClosePrice < r.LowPrice).ToList();

        if (inconsistentOHLC.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Critical,
                Category = IssueCategory.Consistency,
                Message = $"OHLC inconsistencies found in {inconsistentOHLC.Count} records",
                AffectedSymbols = inconsistentOHLC.Select(r => r.SymbolTicker).Distinct().ToList(),
                AffectedSymbolsCount = inconsistentOHLC.Count
            });
        }

        // Volume-Price relationship anomalies
        var zeroVolumeWithPriceChange = records.Where(r =>
            r.Volume == 0 && r.PriceChange.HasValue && r.PriceChange != 0).ToList();

        if (zeroVolumeWithPriceChange.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Consistency,
                Message = $"Zero volume with price changes in {zeroVolumeWithPriceChange.Count} records",
                AffectedSymbols = zeroVolumeWithPriceChange.Select(r => r.SymbolTicker).Distinct().ToList(),
                AffectedSymbolsCount = zeroVolumeWithPriceChange.Count
            });
        }

        report.Issues.AddRange(issues);
    }

    private void ValidateIntegrity(DataQualityReport report, List<HistoricalMarketData> records)
    {
        var issues = new List<DataQualityIssue>();

        // Check for records with missing symbol IDs
        var missingSymbolIds = records.Where(r => r.SymbolId == Guid.Empty).ToList();
        if (missingSymbolIds.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Critical,
                Category = IssueCategory.Integrity,
                Message = $"Missing symbol IDs in {missingSymbolIds.Count} records",
                AffectedSymbolsCount = missingSymbolIds.Count
            });
        }

        // Check for invalid data sources
        var invalidDataSources = records.Where(r =>
            string.IsNullOrEmpty(r.DataSource) ||
            !_config.ValidDataSources.Contains(r.DataSource)).ToList();

        if (invalidDataSources.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Integrity,
                Message = $"Invalid data sources in {invalidDataSources.Count} records",
                AffectedSymbolsCount = invalidDataSources.Count
            });
        }

        report.Issues.AddRange(issues);
    }

    private async Task ValidateTimeliness(
        DataQualityReport report,
        List<HistoricalMarketData> records,
        CancellationToken cancellationToken)
    {
        var issues = new List<DataQualityIssue>();

        // Check data freshness
        var staleRecords = records.Where(r =>
            DateTime.UtcNow - r.DataCollectedAt > TimeSpan.FromHours(_config.MaxDataAgeHours)).ToList();

        if (staleRecords.Any())
        {
            issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Timeliness,
                Message = $"Stale data (>{_config.MaxDataAgeHours}h old) in {staleRecords.Count} records",
                AffectedSymbols = staleRecords.Select(r => r.SymbolTicker).Distinct().ToList(),
                AffectedSymbolsCount = staleRecords.Count
            });
        }

        report.Issues.AddRange(issues);
    }

    private async Task ValidateDuplicates(
        DataQualityReport report,
        List<HistoricalMarketData> records,
        CancellationToken cancellationToken)
    {
        // Group by symbol and check for duplicates
        var duplicateGroups = records
            .GroupBy(r => new { r.SymbolTicker, r.TradeDate, r.DataSource })
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Any())
        {
            var duplicateCount = duplicateGroups.Sum(g => g.Count() - 1); // Subtract 1 to get extra copies

            report.Issues.Add(new DataQualityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Duplicates,
                Message = $"Found {duplicateCount} duplicate records across {duplicateGroups.Count} symbols",
                AffectedSymbols = duplicateGroups.Select(g => g.Key.SymbolTicker).ToList(),
                AffectedSymbolsCount = duplicateGroups.Count
            });
        }
    }

    private async Task<List<Symbol>> GetExpectedSymbolsAsync(string market, CancellationToken cancellationToken)
    {
        var query = _dbContext.Symbols.Where(s => s.IsActive);

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

    private bool IsExpectedTradingDay(string market, DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;

        return market.ToUpper() switch
        {
            "CRYPTO" => true, // 24/7
            _ => dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday
        };
    }

    private decimal CalculateOverallScore(DataQualityReport report)
    {
        if (!report.Issues.Any())
            return 100;

        var criticalPenalty = report.Issues.Count(i => i.Severity == IssueSeverity.Critical) * 25;
        var warningPenalty = report.Issues.Count(i => i.Severity == IssueSeverity.Warning) * 10;
        var infoPenalty = report.Issues.Count(i => i.Severity == IssueSeverity.Info) * 2;

        var totalPenalty = criticalPenalty + warningPenalty + infoPenalty;
        return Math.Max(0, 100 - totalPenalty);
    }

    private void GenerateRecommendations(DataQualityReport report)
    {
        var recommendations = new List<string>();

        var criticalIssues = report.Issues.Where(i => i.Severity == IssueSeverity.Critical).ToList();
        var warningIssues = report.Issues.Where(i => i.Severity == IssueSeverity.Warning).ToList();

        if (criticalIssues.Any(i => i.Category == IssueCategory.Completeness))
        {
            recommendations.Add("Investigate data source reliability and implement backup data providers");
        }

        if (criticalIssues.Any(i => i.Category == IssueCategory.Accuracy))
        {
            recommendations.Add("Implement real-time data validation at ingestion point");
        }

        if (warningIssues.Any(i => i.Category == IssueCategory.Duplicates))
        {
            recommendations.Add("Run duplicate cleanup process and review data ingestion logic");
        }

        if (report.OverallScore < 80)
        {
            recommendations.Add("Schedule immediate data quality review and remediation");
        }
        else if (report.OverallScore < 95)
        {
            recommendations.Add("Monitor data quality trends and address recurring issues");
        }

        report.Recommendations = recommendations;
    }
}

/// <summary>
/// Configuration for data quality validation
/// </summary>
public class DataQualityConfiguration
{
    public decimal MinCompletenessThreshold { get; set; } = 95.0m;
    public int MaxDataAgeHours { get; set; } = 48;
    public List<string> ValidDataSources { get; set; } = new() { "YAHOO", "BIST", "BINANCE", "ALPHA_VANTAGE" };
    public decimal MinOverallScore { get; set; } = 80.0m;
    public bool EnableAutoRemediation { get; set; } = false;
}

/// <summary>
/// Data quality validation report
/// </summary>
public class DataQualityReport
{
    public string Market { get; set; } = string.Empty;
    public DateOnly ValidationDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }

    public int TotalRecordsValidated { get; set; }
    public decimal OverallScore { get; set; }

    public List<DataQualityIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Data completeness report across multiple dates
/// </summary>
public class DataCompletenessReport
{
    public string Market { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public List<DailyCompletenessEntry> DailyCompleteness { get; set; } = new();
    public List<DateOnly> IncompleteDays { get; set; } = new();
    public decimal AverageCompleteness { get; set; }
}

/// <summary>
/// Daily completeness entry
/// </summary>
public class DailyCompletenessEntry
{
    public DateOnly Date { get; set; }
    public int ExpectedRecords { get; set; }
    public int ActualRecords { get; set; }
    public decimal CompletenessPercent { get; set; }
}

/// <summary>
/// Data quality issue
/// </summary>
public class DataQualityIssue
{
    public IssueSeverity Severity { get; set; }
    public IssueCategory Category { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> AffectedSymbols { get; set; } = new();
    public int AffectedSymbolsCount { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Issue severity levels
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Issue categories
/// </summary>
public enum IssueCategory
{
    Completeness,
    Accuracy,
    Consistency,
    Integrity,
    Timeliness,
    Duplicates,
    SystemError
}