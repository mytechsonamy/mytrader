using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyTrader.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Api.Controllers;

/// <summary>
/// Controller for managing Yahoo Finance daily sync operations
/// Provides manual triggers, monitoring, and configuration endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for sync operations
public class YahooFinanceSyncController : ControllerBase
{
    private readonly YahooFinanceDailyDataService _dailyDataService;
    private readonly DataQualityValidationService _qualityService;
    private readonly YahooFinanceErrorHandlingService _errorHandlingService;
    private readonly ILogger<YahooFinanceSyncController> _logger;

    public YahooFinanceSyncController(
        YahooFinanceDailyDataService dailyDataService,
        DataQualityValidationService qualityService,
        YahooFinanceErrorHandlingService errorHandlingService,
        ILogger<YahooFinanceSyncController> logger)
    {
        _dailyDataService = dailyDataService;
        _qualityService = qualityService;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger daily sync for a specific market
    /// </summary>
    [HttpPost("sync/{market}")]
    public async Task<IActionResult> TriggerMarketSync(
        [FromRoute] string market,
        [FromQuery] DateTime? specificDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual sync triggered for market {Market} by user {UserId}",
                market, User.Identity?.Name);

            var result = await _dailyDataService.SyncMarketDataAsync(market, specificDate, cancellationToken);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Sync completed successfully",
                    data = new
                    {
                        market = result.Market,
                        syncDate = result.SyncDate,
                        duration = result.Duration,
                        statistics = new
                        {
                            successful = result.SuccessfulSymbols,
                            failed = result.FailedSymbols,
                            skipped = result.SkippedSymbols,
                            noData = result.NoDataSymbols,
                            totalRecords = result.TotalRecordsProcessed,
                            completeness = result.DataCompletenessPercent
                        },
                        errors = result.Errors.Take(10).ToList() // Limit error details
                    }
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage ?? "Sync failed",
                    market = result.Market,
                    errors = result.Errors
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual sync for market {Market}", market);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during sync operation",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Trigger sync for all markets
    /// </summary>
    [HttpPost("sync-all")]
    public async Task<IActionResult> TriggerAllMarketsSync(
        [FromQuery] DateTime? specificDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Full market sync triggered by user {UserId}", User.Identity?.Name);

            var markets = new[] { "BIST", "NYSE", "NASDAQ", "CRYPTO" };
            var results = new List<object>();

            foreach (var market in markets)
            {
                try
                {
                    var result = await _dailyDataService.SyncMarketDataAsync(market, specificDate, cancellationToken);
                    results.Add(new
                    {
                        market = market,
                        success = result.Success,
                        message = result.Success ? "Completed" : result.ErrorMessage,
                        statistics = new
                        {
                            successful = result.SuccessfulSymbols,
                            failed = result.FailedSymbols,
                            duration = result.Duration
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing market {Market}", market);
                    results.Add(new
                    {
                        market = market,
                        success = false,
                        message = ex.Message
                    });
                }

                // Add delay between markets to avoid overwhelming the API
                if (market != markets.Last())
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            }

            var overallSuccess = results.All(r => (bool)r.GetType().GetProperty("success")!.GetValue(r)!);

            return Ok(new
            {
                success = overallSuccess,
                message = $"Sync completed for {results.Count} markets",
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full market sync");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during sync operation",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Detect and fill data gaps for a market
    /// </summary>
    [HttpPost("fill-gaps/{market}")]
    public async Task<IActionResult> FillDataGaps(
        [FromRoute] string market,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow.AddDays(-1);

            _logger.LogInformation("Gap filling triggered for market {Market} from {StartDate} to {EndDate}",
                market, start, end);

            var result = await _dailyDataService.DetectAndFillGapsAsync(market, start, end, cancellationToken);

            return Ok(new
            {
                success = result.Success,
                message = result.Success ? "Gap filling completed" : result.ErrorMessage,
                data = new
                {
                    market = result.Market,
                    dateRange = new { start = result.StartDate, end = result.EndDate },
                    statistics = new
                    {
                        gapsDetected = result.GapsDetected,
                        gapsFilled = result.GapsFilled,
                        gapsFailed = result.GapsFailed
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during gap filling for market {Market}", market);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during gap filling",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Validate data quality for a specific market and date
    /// </summary>
    [HttpPost("validate/{market}")]
    public async Task<IActionResult> ValidateDataQuality(
        [FromRoute] string market,
        [FromQuery] DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationDate = DateOnly.FromDateTime(date ?? DateTime.UtcNow.AddDays(-1));

            _logger.LogInformation("Data quality validation triggered for market {Market} on {Date}",
                market, validationDate);

            var report = await _qualityService.ValidateMarketDataAsync(market, validationDate, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Data quality validation completed",
                data = new
                {
                    market = report.Market,
                    validationDate = report.ValidationDate,
                    overallScore = report.OverallScore,
                    totalRecords = report.TotalRecordsValidated,
                    duration = report.Duration,
                    issues = report.Issues.Select(i => new
                    {
                        severity = i.Severity.ToString(),
                        category = i.Category.ToString(),
                        message = i.Message,
                        affectedSymbolsCount = i.AffectedSymbolsCount,
                        affectedSymbols = i.AffectedSymbols.Take(10).ToList() // Limit for response size
                    }).ToList(),
                    recommendations = report.Recommendations
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data quality validation for market {Market}", market);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during validation",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get data completeness report for a market over a date range
    /// </summary>
    [HttpGet("completeness/{market}")]
    public async Task<IActionResult> GetDataCompleteness(
        [FromRoute] string market,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = DateOnly.FromDateTime(startDate ?? DateTime.UtcNow.AddDays(-30));
            var end = DateOnly.FromDateTime(endDate ?? DateTime.UtcNow.AddDays(-1));

            var report = await _qualityService.ValidateDataCompletenessAsync(market, start, end, cancellationToken);

            return Ok(new
            {
                success = true,
                data = new
                {
                    market = report.Market,
                    dateRange = new { start = report.StartDate, end = report.EndDate },
                    averageCompleteness = report.AverageCompleteness,
                    incompleteDaysCount = report.IncompleteDays.Count,
                    incompleteDays = report.IncompleteDays.Take(10).ToList(),
                    dailyCompleteness = report.DailyCompleteness.OrderByDescending(d => d.Date).Take(30).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data completeness for market {Market}", market);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during completeness check",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get sync status and error statistics
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetSyncStatus()
    {
        try
        {
            var errorStats = _errorHandlingService.GetErrorStatistics();

            return Ok(new
            {
                success = true,
                data = new
                {
                    timestamp = DateTime.UtcNow,
                    circuitBreakers = errorStats.CircuitBreakers.Select(cb => new
                    {
                        operation = cb.Key,
                        state = cb.Value.State.ToString(),
                        failureCount = cb.Value.FailureCount,
                        lastFailure = cb.Value.LastFailureTime,
                        lastSuccess = cb.Value.LastSuccessTime
                    }).ToList(),
                    retryStatistics = errorStats.RetryContexts.Select(rc => new
                    {
                        operation = rc.Key,
                        totalAttempts = rc.Value.TotalAttempts,
                        lastErrorType = rc.Value.LastErrorType?.ToString(),
                        lastErrorTime = rc.Value.LastErrorTime
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error getting status",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test Yahoo Finance API connectivity for a symbol
    /// </summary>
    [HttpGet("test/{market}/{symbol}")]
    public async Task<IActionResult> TestApiConnectivity(
        [FromRoute] string market,
        [FromRoute] string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var yahooApiService = HttpContext.RequestServices.GetRequiredService<YahooFinanceApiService>();

            var testDate = DateTime.UtcNow.AddDays(-1);
            var result = await yahooApiService.GetHistoricalDataAsync(symbol, testDate, testDate, market, cancellationToken);

            return Ok(new
            {
                success = result.Success,
                message = result.Success ? "API connectivity test successful" : "API connectivity test failed",
                data = new
                {
                    symbol = result.Symbol,
                    market = market,
                    recordsReturned = result.RecordsCount,
                    errorMessage = result.ErrorMessage,
                    isRetryable = result.IsRetryable,
                    requestTime = result.RequestTime
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API connectivity for {Market}:{Symbol}", market, symbol);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during API test",
                error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for sync operations
/// </summary>
public class SyncRequest
{
    [Required]
    public string Market { get; set; } = string.Empty;

    public DateTime? SpecificDate { get; set; }

    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// Request model for gap filling operations
/// </summary>
public class GapFillRequest
{
    [Required]
    public string Market { get; set; } = string.Empty;

    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(-1);

    public int MaxGapsToFill { get; set; } = 100;
}