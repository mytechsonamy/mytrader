using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.Services;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Background service for scheduled 5-minute intraday data collection
/// Runs continuously during market hours for real-time data collection
/// </summary>
public class YahooFinanceIntradayScheduledService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<YahooFinanceIntradayScheduledService> _logger;
    private readonly YahooFinanceIntradayScheduleConfiguration _config;
    private readonly SemaphoreSlim _executionSemaphore;

    public YahooFinanceIntradayScheduledService(
        IServiceProvider serviceProvider,
        ILogger<YahooFinanceIntradayScheduledService> logger,
        IOptions<YahooFinanceIntradayScheduleConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = configuration.Value;
        _executionSemaphore = new SemaphoreSlim(1, 1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yahoo Finance 5-minute intraday scheduled service started");

        // Initial delay to avoid startup conflicts
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                // Calculate next execution time (every 5 minutes on the 5-minute mark)
                var nextExecution = CalculateNextExecutionTime(startTime);
                var delay = nextExecution - startTime;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug("Next 5-minute sync scheduled for {NextExecution} (in {Delay})",
                        nextExecution.ToString("yyyy-MM-dd HH:mm:ss UTC"), delay);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested) break;

                // Execute the sync with timeout protection
                await ExecuteIntradaySyncWithTimeoutAsync(stoppingToken);

                // Small buffer to prevent immediate re-execution
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in 5-minute intraday scheduled service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retry
            }
        }

        _logger.LogInformation("Yahoo Finance 5-minute intraday scheduled service stopped");
    }

    /// <summary>
    /// Execute intraday sync with timeout protection
    /// </summary>
    private async Task ExecuteIntradaySyncWithTimeoutAsync(CancellationToken stoppingToken)
    {
        var acquired = await _executionSemaphore.WaitAsync(100, stoppingToken);
        if (!acquired)
        {
            _logger.LogWarning("Previous 5-minute sync still running, skipping this cycle");
            return;
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(_config.MaxExecutionDuration);

            await ExecuteIntradaySyncAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("5-minute sync cancelled due to service shutdown");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("5-minute sync timed out after {Timeout}", _config.MaxExecutionDuration);
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    /// <summary>
    /// Execute the actual intraday synchronization
    /// </summary>
    private async Task ExecuteIntradaySyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var intradayService = scope.ServiceProvider.GetRequiredService<YahooFinanceIntradayDataService>();

        var syncStartTime = DateTime.UtcNow;
        _logger.LogDebug("Starting 5-minute intraday sync at {Time}", syncStartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));

        try
        {
            var result = await intradayService.SyncIntradayDataAsync(cancellationToken: cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("5-minute sync completed successfully. " +
                    "Markets: {Markets}, Processed: {Processed}, Successful: {Successful}, " +
                    "Failed: {Failed}, Records: {Records}, Duration: {Duration}",
                    string.Join(", ", result.Markets), result.TotalSymbolsProcessed, result.SuccessfulSymbols,
                    result.FailedSymbols, result.TotalRecordsProcessed, result.Duration);

                // Update last successful sync time
                LastSuccessfulSync = syncStartTime;
                ConsecutiveFailures = 0;

                // Log detailed market results if enabled
                if (_config.LogDetailedResults)
                {
                    LogDetailedMarketResults(result);
                }
            }
            else
            {
                ConsecutiveFailures++;
                _logger.LogError("5-minute sync failed: {Error}. Consecutive failures: {Failures}",
                    result.ErrorMessage, ConsecutiveFailures);

                // Send alert if consecutive failures exceed threshold
                if (ConsecutiveFailures >= _config.AlertAfterConsecutiveFailures)
                {
                    await SendFailureAlertAsync(result, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            ConsecutiveFailures++;
            _logger.LogError(ex, "Error during 5-minute sync execution. Consecutive failures: {Failures}",
                ConsecutiveFailures);

            if (ConsecutiveFailures >= _config.AlertAfterConsecutiveFailures)
            {
                await SendFailureAlertAsync(null, cancellationToken, ex.Message);
            }
        }
    }

    /// <summary>
    /// Calculate the next execution time (every 5 minutes on the 5-minute mark)
    /// </summary>
    private DateTime CalculateNextExecutionTime(DateTime currentTime)
    {
        // Round up to the next 5-minute mark
        var minutes = currentTime.Minute;
        var nextMinutes = ((minutes / 5) + 1) * 5;

        var nextExecution = new DateTime(
            currentTime.Year,
            currentTime.Month,
            currentTime.Day,
            currentTime.Hour,
            0,
            0,
            DateTimeKind.Utc
        ).AddMinutes(nextMinutes);

        // If we've gone past the current hour, move to next hour
        if (nextExecution.Hour != currentTime.Hour && nextMinutes >= 60)
        {
            nextExecution = nextExecution.AddHours(1 - (nextMinutes / 60));
        }

        return nextExecution;
    }

    /// <summary>
    /// Log detailed market results for monitoring
    /// </summary>
    private void LogDetailedMarketResults(IntradaySyncResult result)
    {
        foreach (var marketResult in result.MarketResults)
        {
            if (marketResult.Success)
            {
                _logger.LogDebug("Market {Market}: Success={Success}, Symbols={Symbols}, " +
                    "Records={Records}, Duration={Duration}, Reason={Reason}",
                    marketResult.Market, marketResult.Success, marketResult.SuccessfulSymbols,
                    marketResult.TotalRecordsProcessed, marketResult.Duration, marketResult.SkippedReason ?? "Completed");
            }
            else
            {
                _logger.LogWarning("Market {Market}: Failed - {Error}", marketResult.Market, marketResult.ErrorMessage);
            }

            // Log validation errors if any
            if (marketResult.ValidationErrors.Any())
            {
                _logger.LogWarning("Market {Market} validation errors: {Errors}",
                    marketResult.Market, string.Join("; ", marketResult.ValidationErrors.Take(5)));
            }
        }
    }

    /// <summary>
    /// Send failure alert when consecutive failures exceed threshold
    /// </summary>
    private async Task SendFailureAlertAsync(IntradaySyncResult? result, CancellationToken cancellationToken, string? additionalError = null)
    {
        try
        {
            var errorMessage = additionalError ?? result?.ErrorMessage ?? "Unknown error";
            var alertMessage = $"5-minute intraday sync failed {ConsecutiveFailures} consecutive times. " +
                             $"Last error: {errorMessage}. " +
                             $"Last successful sync: {LastSuccessfulSync?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Never"}";

            _logger.LogCritical("ALERT: {AlertMessage}", alertMessage);

            // Here you could integrate with your alerting system:
            // - Send email notification
            // - Post to Slack/Teams
            // - Send to monitoring system
            // - Write to alert queue

            if (!string.IsNullOrEmpty(_config.AlertWebhookUrl))
            {
                await SendWebhookAlertAsync(alertMessage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send failure alert");
        }
    }

    /// <summary>
    /// Send alert via webhook
    /// </summary>
    private async Task SendWebhookAlertAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var payload = new { text = message, timestamp = DateTime.UtcNow };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_config.AlertWebhookUrl, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Alert sent successfully via webhook");
            }
            else
            {
                _logger.LogWarning("Failed to send alert via webhook: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook alert");
        }
    }

    /// <summary>
    /// Get service health status
    /// </summary>
    public IntradayServiceHealthStatus GetHealthStatus()
    {
        var now = DateTime.UtcNow;

        return new IntradayServiceHealthStatus
        {
            IsHealthy = ConsecutiveFailures < _config.AlertAfterConsecutiveFailures,
            LastSuccessfulSync = LastSuccessfulSync,
            ConsecutiveFailures = ConsecutiveFailures,
            TimeSinceLastSuccess = LastSuccessfulSync.HasValue ? now - LastSuccessfulSync.Value : null,
            IsCurrentlyExecuting = _executionSemaphore.CurrentCount == 0,
            NextScheduledExecution = CalculateNextExecutionTime(now)
        };
    }

    // Service state tracking
    public DateTime? LastSuccessfulSync { get; private set; }
    public int ConsecutiveFailures { get; private set; }

    public override void Dispose()
    {
        _executionSemaphore?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Configuration for the intraday scheduled service
/// </summary>
public class YahooFinanceIntradayScheduleConfiguration
{
    public TimeSpan MaxExecutionDuration { get; set; } = TimeSpan.FromMinutes(4); // Must complete within 4 minutes
    public int AlertAfterConsecutiveFailures { get; set; } = 3;
    public bool LogDetailedResults { get; set; } = false; // Enable for debugging
    public string? AlertWebhookUrl { get; set; }
    public bool EnableHealthChecks { get; set; } = true;

    // Market-specific settings
    public bool EnableDuringPremarket { get; set; } = false;
    public bool EnableDuringAfterHours { get; set; } = false;

    // Performance settings
    public bool EnablePerformanceLogging { get; set; } = true;
    public TimeSpan PerformanceLogThreshold { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Health status of the intraday service
/// </summary>
public class IntradayServiceHealthStatus
{
    public bool IsHealthy { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public int ConsecutiveFailures { get; set; }
    public TimeSpan? TimeSinceLastSuccess { get; set; }
    public bool IsCurrentlyExecuting { get; set; }
    public DateTime NextScheduledExecution { get; set; }
    public string Status => IsHealthy ? "Healthy" : $"Unhealthy ({ConsecutiveFailures} failures)";
}