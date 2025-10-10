using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Services;
using MyTrader.Core.Data;
using MyTrader.Infrastructure.Services;
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
    private readonly YahooFinanceIntradayDataService _intradayDataService;
    private readonly DataQualityValidationService _qualityService;
    private readonly YahooFinanceErrorHandlingService _errorHandlingService;
    private readonly ILogger<YahooFinanceSyncController> _logger;

    public YahooFinanceSyncController(
        YahooFinanceDailyDataService dailyDataService,
        YahooFinanceIntradayDataService intradayDataService,
        DataQualityValidationService qualityService,
        YahooFinanceErrorHandlingService errorHandlingService,
        ILogger<YahooFinanceSyncController> logger)
    {
        _dailyDataService = dailyDataService;
        _intradayDataService = intradayDataService;
        _qualityService = qualityService;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
    }

    // ==================== INTRADAY (5-MINUTE) ENDPOINTS ====================

    /// <summary>
    /// Manually trigger 5-minute intraday sync for all active markets
    /// </summary>
    [HttpPost("intraday/sync")]
    public async Task<IActionResult> TriggerIntradaySync(
        [FromQuery] string[]? markets = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual 5-minute intraday sync triggered by user {UserId} for markets: {Markets}",
                User.Identity?.Name, markets != null ? string.Join(", ", markets) : "ALL");

            var result = await _intradayDataService.SyncIntradayDataAsync(markets, cancellationToken: cancellationToken);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = "5-minute intraday sync completed successfully",
                    data = new
                    {
                        interval = result.Interval,
                        markets = result.Markets,
                        duration = result.Duration,
                        statistics = new
                        {
                            totalProcessed = result.TotalSymbolsProcessed,
                            successful = result.SuccessfulSymbols,
                            failed = result.FailedSymbols,
                            skipped = result.SkippedSymbols,
                            totalRecords = result.TotalRecordsProcessed
                        },
                        marketResults = result.MarketResults.Select(mr => new
                        {
                            market = mr.Market,
                            success = mr.Success,
                            duration = mr.Duration,
                            statistics = new
                            {
                                successful = mr.SuccessfulSymbols,
                                failed = mr.FailedSymbols,
                                skipped = mr.SkippedSymbols,
                                records = mr.TotalRecordsProcessed
                            },
                            skippedReason = mr.SkippedReason,
                            errorMessage = mr.ErrorMessage,
                            dataTimeRange = mr.DataStartTime.HasValue && mr.DataEndTime.HasValue
                                ? new { start = mr.DataStartTime, end = mr.DataEndTime }
                                : null
                        }).ToList()
                    }
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage ?? "5-minute intraday sync failed",
                    duration = result.Duration,
                    statistics = new
                    {
                        totalProcessed = result.TotalSymbolsProcessed,
                        successful = result.SuccessfulSymbols,
                        failed = result.FailedSymbols
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual 5-minute intraday sync");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during 5-minute sync operation",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get the current status of the intraday collection service
    /// </summary>
    [HttpGet("intraday/status")]
    public IActionResult GetIntradayServiceStatus()
    {
        try
        {
            var scheduledService = HttpContext.RequestServices
                .GetService<YahooFinanceIntradayScheduledService>();

            if (scheduledService == null)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        serviceStatus = "Not Available",
                        message = "Intraday scheduled service is not registered or running"
                    }
                });
            }

            var healthStatus = scheduledService.GetHealthStatus();

            return Ok(new
            {
                success = true,
                data = new
                {
                    serviceStatus = healthStatus.Status,
                    isHealthy = healthStatus.IsHealthy,
                    lastSuccessfulSync = healthStatus.LastSuccessfulSync,
                    consecutiveFailures = healthStatus.ConsecutiveFailures,
                    timeSinceLastSuccess = healthStatus.TimeSinceLastSuccess,
                    isCurrentlyExecuting = healthStatus.IsCurrentlyExecuting,
                    nextScheduledExecution = healthStatus.NextScheduledExecution,
                    currentTime = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting intraday service status");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error getting intraday service status",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Check which markets are currently in trading hours
    /// </summary>
    [HttpGet("intraday/market-hours")]
    public IActionResult GetMarketHoursStatus()
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var markets = new[] { "BIST", "NYSE", "NASDAQ", "CRYPTO" };
            var marketStatuses = new List<object>();

            foreach (var market in markets)
            {
                var isOpen = CheckMarketTradingHours(market, currentTime);
                var nextOpen = GetNextMarketOpen(market, currentTime);
                var nextClose = GetNextMarketClose(market, currentTime);

                marketStatuses.Add(new
                {
                    market = market,
                    isOpen = isOpen,
                    status = isOpen ? "OPEN" : "CLOSED",
                    currentTime = currentTime,
                    nextOpen = nextOpen,
                    nextClose = nextClose,
                    timeZone = GetMarketTimeZone(market)
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    timestamp = currentTime,
                    markets = marketStatuses
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market hours status");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error getting market hours status",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get recent 5-minute data for a specific symbol
    /// </summary>
    [HttpGet("intraday/data/{market}/{symbol}")]
    public async Task<IActionResult> GetRecentIntradayData(
        [FromRoute] string market,
        [FromRoute] string symbol,
        [FromQuery] int hours = 2,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dbContext = HttpContext.RequestServices.GetRequiredService<ITradingDbContext>();
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddHours(-hours);

            var data = await dbContext.HistoricalMarketData
                .Where(h => h.SymbolTicker == symbol &&
                           h.MarketCode == market &&
                           h.Timeframe == "5MIN" &&
                           h.DataSource == "YAHOO" &&
                           h.Timestamp >= startTime &&
                           h.Timestamp <= endTime)
                .OrderByDescending(h => h.Timestamp)
                .Take(100)
                .Select(h => new
                {
                    timestamp = h.Timestamp,
                    open = h.OpenPrice,
                    high = h.HighPrice,
                    low = h.LowPrice,
                    close = h.ClosePrice,
                    volume = h.Volume,
                    dataQualityScore = h.DataQualityScore
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                data = new
                {
                    symbol = symbol,
                    market = market,
                    timeframe = "5MIN",
                    timeRange = new { start = startTime, end = endTime },
                    recordCount = data.Count,
                    records = data
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent intraday data for {Market}:{Symbol}", market, symbol);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error getting intraday data",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test intraday API connectivity for a symbol
    /// </summary>
    [HttpGet("intraday/test/{market}/{symbol}")]
    public async Task<IActionResult> TestIntradayApiConnectivity(
        [FromRoute] string market,
        [FromRoute] string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var yahooApiService = HttpContext.RequestServices.GetRequiredService<YahooFinanceApiService>();

            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-30); // Last 30 minutes

            var result = await yahooApiService.GetIntradayDataAsync(symbol, startTime, endTime, "5m", market, cancellationToken);

            return Ok(new
            {
                success = result.Success,
                message = result.Success ? "Intraday API connectivity test successful" : "Intraday API connectivity test failed",
                data = new
                {
                    symbol = result.Symbol,
                    market = market,
                    interval = "5m",
                    timeRange = new { start = startTime, end = endTime },
                    recordsReturned = result.RecordsCount,
                    errorMessage = result.ErrorMessage,
                    isRetryable = result.IsRetryable,
                    requestTime = result.RequestTime
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing intraday API connectivity for {Market}:{Symbol}", market, symbol);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during intraday API test",
                error = ex.Message
            });
        }
    }

    // ==================== DAILY SYNC ENDPOINTS ====================

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

    // ==================== HELPER METHODS ====================

    /// <summary>
    /// Check if a market is currently in trading hours
    /// </summary>
    private bool CheckMarketTradingHours(string market, DateTime utcTime)
    {
        return market.ToUpper() switch
        {
            "BIST" => IsInBistTradingHours(utcTime),
            "NYSE" or "NASDAQ" => IsInUSMarketTradingHours(utcTime),
            "CRYPTO" => true, // 24/7
            _ => false
        };
    }

    /// <summary>
    /// Get the next market open time
    /// </summary>
    private DateTime? GetNextMarketOpen(string market, DateTime utcTime)
    {
        try
        {
            return market.ToUpper() switch
            {
                "BIST" => GetNextBistOpen(utcTime),
                "NYSE" or "NASDAQ" => GetNextUSMarketOpen(utcTime),
                "CRYPTO" => null, // Always open
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get the next market close time
    /// </summary>
    private DateTime? GetNextMarketClose(string market, DateTime utcTime)
    {
        try
        {
            return market.ToUpper() switch
            {
                "BIST" => GetNextBistClose(utcTime),
                "NYSE" or "NASDAQ" => GetNextUSMarketClose(utcTime),
                "CRYPTO" => null, // Never closes
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get the market time zone string
    /// </summary>
    private string GetMarketTimeZone(string market)
    {
        return market.ToUpper() switch
        {
            "BIST" => "Turkey Standard Time",
            "NYSE" or "NASDAQ" => "Eastern Standard Time",
            "CRYPTO" => "UTC",
            _ => "UTC"
        };
    }

    /// <summary>
    /// Check if current time is within BIST trading hours
    /// </summary>
    private bool IsInBistTradingHours(DateTime utcTime)
    {
        try
        {
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, turkeyTimeZone);

            if (turkeyTime.DayOfWeek == DayOfWeek.Saturday || turkeyTime.DayOfWeek == DayOfWeek.Sunday)
                return false;

            var marketOpen = new TimeOnly(10, 0);
            var marketClose = new TimeOnly(18, 0);
            var currentTime = TimeOnly.FromDateTime(turkeyTime);

            return currentTime >= marketOpen && currentTime <= marketClose;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if current time is within US market trading hours
    /// </summary>
    private bool IsInUSMarketTradingHours(DateTime utcTime)
    {
        try
        {
            var etTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var etTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, etTimeZone);

            if (etTime.DayOfWeek == DayOfWeek.Saturday || etTime.DayOfWeek == DayOfWeek.Sunday)
                return false;

            var marketOpen = new TimeOnly(9, 30);
            var marketClose = new TimeOnly(16, 0);
            var currentTime = TimeOnly.FromDateTime(etTime);

            return currentTime >= marketOpen && currentTime <= marketClose;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get next BIST market open time
    /// </summary>
    private DateTime GetNextBistOpen(DateTime utcTime)
    {
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, turkeyTimeZone);

        var nextOpen = turkeyTime.Date.AddHours(10); // 10:00 Turkey time

        // If it's past 10:00 today or weekend, move to next business day
        if (turkeyTime.TimeOfDay >= new TimeSpan(10, 0, 0) ||
            turkeyTime.DayOfWeek == DayOfWeek.Saturday ||
            turkeyTime.DayOfWeek == DayOfWeek.Sunday)
        {
            do
            {
                nextOpen = nextOpen.AddDays(1);
            } while (nextOpen.DayOfWeek == DayOfWeek.Saturday || nextOpen.DayOfWeek == DayOfWeek.Sunday);
        }

        return TimeZoneInfo.ConvertTimeToUtc(nextOpen, turkeyTimeZone);
    }

    /// <summary>
    /// Get next BIST market close time
    /// </summary>
    private DateTime GetNextBistClose(DateTime utcTime)
    {
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, turkeyTimeZone);

        var nextClose = turkeyTime.Date.AddHours(18); // 18:00 Turkey time

        // If it's past 18:00 today or weekend, move to next business day
        if (turkeyTime.TimeOfDay >= new TimeSpan(18, 0, 0) ||
            turkeyTime.DayOfWeek == DayOfWeek.Saturday ||
            turkeyTime.DayOfWeek == DayOfWeek.Sunday)
        {
            do
            {
                nextClose = nextClose.AddDays(1);
            } while (nextClose.DayOfWeek == DayOfWeek.Saturday || nextClose.DayOfWeek == DayOfWeek.Sunday);
        }

        return TimeZoneInfo.ConvertTimeToUtc(nextClose, turkeyTimeZone);
    }

    /// <summary>
    /// Get next US market open time
    /// </summary>
    private DateTime GetNextUSMarketOpen(DateTime utcTime)
    {
        var etTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var etTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, etTimeZone);

        var nextOpen = etTime.Date.AddHours(9).AddMinutes(30); // 9:30 ET

        // If it's past 9:30 today or weekend, move to next business day
        if (etTime.TimeOfDay >= new TimeSpan(9, 30, 0) ||
            etTime.DayOfWeek == DayOfWeek.Saturday ||
            etTime.DayOfWeek == DayOfWeek.Sunday)
        {
            do
            {
                nextOpen = nextOpen.AddDays(1);
            } while (nextOpen.DayOfWeek == DayOfWeek.Saturday || nextOpen.DayOfWeek == DayOfWeek.Sunday);
        }

        return TimeZoneInfo.ConvertTimeToUtc(nextOpen, etTimeZone);
    }

    /// <summary>
    /// Get next US market close time
    /// </summary>
    private DateTime GetNextUSMarketClose(DateTime utcTime)
    {
        var etTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var etTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, etTimeZone);

        var nextClose = etTime.Date.AddHours(16); // 16:00 ET

        // If it's past 16:00 today or weekend, move to next business day
        if (etTime.TimeOfDay >= new TimeSpan(16, 0, 0) ||
            etTime.DayOfWeek == DayOfWeek.Saturday ||
            etTime.DayOfWeek == DayOfWeek.Sunday)
        {
            do
            {
                nextClose = nextClose.AddDays(1);
            } while (nextClose.DayOfWeek == DayOfWeek.Saturday || nextClose.DayOfWeek == DayOfWeek.Sunday);
        }

        return TimeZoneInfo.ConvertTimeToUtc(nextClose, etTimeZone);
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