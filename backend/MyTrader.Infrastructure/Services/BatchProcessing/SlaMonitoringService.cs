using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Services.BatchProcessing;

namespace MyTrader.Infrastructure.Services.BatchProcessing;

/// <summary>
/// Background service for continuous SLA monitoring and alerting
/// Monitors job execution times, sends alerts for SLA breaches, and maintains SLA compliance metrics
/// </summary>
public class SlaMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaMonitoringService> _logger;
    private readonly SlaMonitoringConfiguration _configuration;

    // SLA thresholds for different job types
    private static readonly Dictionary<string, TimeSpan> DefaultSlaThresholds = new()
    {
        ["MarketImport"] = TimeSpan.FromMinutes(30),
        ["AllMarketsImport"] = TimeSpan.FromHours(2),
        ["RetryJob"] = TimeSpan.FromMinutes(15),
        ["Cleanup"] = TimeSpan.FromMinutes(10)
    };

    public SlaMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<SlaMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = new SlaMonitoringConfiguration(); // This would come from config
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitoring Service starting with check interval {Interval}",
            _configuration.CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSlaCheckAsync(stoppingToken);

                await Task.Delay(_configuration.CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SLA monitoring check");

                // Wait shorter interval on error to retry sooner
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Perform comprehensive SLA check and alerting
    /// </summary>
    private async Task PerformSlaCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var batchJobOrchestrator = scope.ServiceProvider.GetRequiredService<IBatchJobOrchestrator>();

        try
        {
            _logger.LogDebug("Starting SLA monitoring check");

            // Get current monitoring stats
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(_configuration.MonitoringWindow);

            var stats = await batchJobOrchestrator.GetJobMonitoringStatsAsync(startTime, endTime);

            // Check for new SLA breaches
            await CheckForSlaBreachesAsync(stats, batchJobOrchestrator);

            // Update SLA compliance metrics
            await UpdateSlaComplianceMetricsAsync(stats);

            // Check system health and performance
            await CheckSystemHealthAsync(stats);

            _logger.LogDebug("Completed SLA monitoring check. Compliance rate: {ComplianceRate:F1}%",
                stats.SlaComplianceRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SLA check");
        }
    }

    /// <summary>
    /// Check for SLA breaches and send alerts
    /// </summary>
    private async Task CheckForSlaBreachesAsync(
        BatchJobMonitoringStats stats,
        IBatchJobOrchestrator batchJobOrchestrator)
    {
        foreach (var breach in stats.RecentSlaBreaches.Where(b => !b.AlertSent))
        {
            try
            {
                await SendSlaBreachAlertAsync(breach);
                breach.AlertSent = true;

                _logger.LogWarning("SLA breach alert sent for job {JobId} in market {Market}. " +
                    "Breach amount: {BreachAmount}, Target: {SlaTarget}",
                    breach.JobId, breach.Market, breach.BreachAmount, breach.SlaTarget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SLA breach alert for job {JobId}", breach.JobId);
            }
        }

        // Check for critical SLA compliance rate
        if (stats.SlaComplianceRate < _configuration.CriticalComplianceThreshold)
        {
            await SendCriticalComplianceAlertAsync(stats);
        }
    }

    /// <summary>
    /// Send SLA breach alert
    /// </summary>
    private async Task SendSlaBreachAlertAsync(SlaBreachAlert breach)
    {
        var alert = new SlaBreachNotification
        {
            JobId = breach.JobId,
            JobType = breach.JobType,
            Market = breach.Market,
            BreachTime = breach.BreachTime,
            ActualDuration = breach.ActualDuration,
            SlaTarget = breach.SlaTarget,
            BreachAmount = breach.BreachAmount,
            Severity = CalculateBreachSeverity(breach),
            Message = $"SLA breach detected for {breach.JobType} job in {breach.Market} market. " +
                $"Job {breach.JobId} took {breach.ActualDuration:hh\\:mm\\:ss} (target: {breach.SlaTarget:hh\\:mm\\:ss})",
            ActionItems = GenerateActionItems(breach)
        };

        // Send to monitoring systems
        await SendToAlertingSystemAsync(alert);

        // Log metrics for monitoring
        _logger.LogWarning("SLA_BREACH JobId={JobId} Market={Market} BreachAmount={BreachAmount} Severity={Severity}",
            breach.JobId, breach.Market, breach.BreachAmount.TotalMinutes, alert.Severity);
    }

    /// <summary>
    /// Send critical compliance rate alert
    /// </summary>
    private async Task SendCriticalComplianceAlertAsync(BatchJobMonitoringStats stats)
    {
        var alert = new CriticalComplianceAlert
        {
            ComplianceRate = stats.SlaComplianceRate,
            Threshold = _configuration.CriticalComplianceThreshold,
            TotalJobs = stats.TotalJobs,
            SlaBreachedJobs = stats.SlaBreachedJobs,
            TimeWindow = _configuration.MonitoringWindow,
            Message = $"CRITICAL: SLA compliance rate dropped to {stats.SlaComplianceRate:F1}% " +
                $"(threshold: {_configuration.CriticalComplianceThreshold:F1}%). " +
                $"{stats.SlaBreachedJobs} out of {stats.TotalJobs} jobs breached SLA in the last {_configuration.MonitoringWindow}."
        };

        await SendToAlertingSystemAsync(alert);

        _logger.LogError("CRITICAL_SLA_COMPLIANCE Rate={ComplianceRate:F1}% Threshold={Threshold:F1}% Jobs={SlaBreachedJobs}/{TotalJobs}",
            stats.SlaComplianceRate, _configuration.CriticalComplianceThreshold, stats.SlaBreachedJobs, stats.TotalJobs);
    }

    /// <summary>
    /// Update SLA compliance metrics
    /// </summary>
    private async Task UpdateSlaComplianceMetricsAsync(BatchJobMonitoringStats stats)
    {
        // This would typically update metrics in a time-series database like InfluxDB or Prometheus
        var metrics = new SlaComplianceMetrics
        {
            Timestamp = DateTime.UtcNow,
            ComplianceRate = stats.SlaComplianceRate,
            TotalJobs = stats.TotalJobs,
            SlaBreachedJobs = stats.SlaBreachedJobs,
            AverageJobDuration = stats.AverageJobDuration,
            P95JobDuration = stats.P95JobDuration,
            ProcessingRate = stats.AverageProcessingRate,
            MarketBreakdown = stats.JobsByMarket.ToDictionary(
                kvp => kvp.Key,
                kvp => new MarketSlaMetrics
                {
                    JobCount = kvp.Value,
                    // Additional market-specific metrics would be calculated here
                })
        };

        // Store metrics (this would be implemented based on your metrics backend)
        await StoreSlaMetricsAsync(metrics);

        _logger.LogDebug("Updated SLA compliance metrics. Rate: {ComplianceRate:F1}%, Jobs: {TotalJobs}",
            stats.SlaComplianceRate, stats.TotalJobs);
    }

    /// <summary>
    /// Check system health and performance thresholds
    /// </summary>
    private async Task CheckSystemHealthAsync(BatchJobMonitoringStats stats)
    {
        // Check processing rate degradation
        if (stats.AverageProcessingRate < _configuration.MinProcessingRate)
        {
            await SendPerformanceDegradationAlertAsync(stats);
        }

        // Check error rate
        var errorRate = stats.TotalJobs > 0 ? (decimal)stats.FailedJobs / stats.TotalJobs * 100 : 0;
        if (errorRate > _configuration.MaxErrorRate)
        {
            await SendHighErrorRateAlertAsync(stats, errorRate);
        }

        // Check resource utilization
        if (stats.ResourceStats.PeakCpuUsage > _configuration.MaxCpuUsage ||
            stats.ResourceStats.PeakMemoryUsage > _configuration.MaxMemoryUsage)
        {
            await SendResourceUtilizationAlertAsync(stats);
        }
    }

    #region Alert Sending Methods

    private async Task SendToAlertingSystemAsync(object alert)
    {
        // This would integrate with your alerting system (Slack, email, PagerDuty, etc.)
        await Task.Delay(100); // Simulate alert sending

        var alertType = alert.GetType().Name;
        _logger.LogInformation("Sent {AlertType} to alerting system", alertType);
    }

    private async Task SendPerformanceDegradationAlertAsync(BatchJobMonitoringStats stats)
    {
        var alert = new PerformanceDegradationAlert
        {
            CurrentProcessingRate = stats.AverageProcessingRate,
            MinThreshold = _configuration.MinProcessingRate,
            Message = $"Processing rate degradation detected: {stats.AverageProcessingRate:F1} records/sec " +
                $"(threshold: {_configuration.MinProcessingRate:F1})"
        };

        await SendToAlertingSystemAsync(alert);
    }

    private async Task SendHighErrorRateAlertAsync(BatchJobMonitoringStats stats, decimal errorRate)
    {
        var alert = new HighErrorRateAlert
        {
            ErrorRate = errorRate,
            MaxThreshold = _configuration.MaxErrorRate,
            FailedJobs = stats.FailedJobs,
            TotalJobs = stats.TotalJobs,
            Message = $"High error rate detected: {errorRate:F1}% (threshold: {_configuration.MaxErrorRate:F1}%)"
        };

        await SendToAlertingSystemAsync(alert);
    }

    private async Task SendResourceUtilizationAlertAsync(BatchJobMonitoringStats stats)
    {
        var alert = new ResourceUtilizationAlert
        {
            CpuUsage = stats.ResourceStats.PeakCpuUsage,
            MemoryUsage = stats.ResourceStats.PeakMemoryUsage,
            Message = $"High resource utilization: CPU {stats.ResourceStats.PeakCpuUsage:F1}%, " +
                $"Memory {stats.ResourceStats.PeakMemoryUsage / (1024 * 1024 * 1024):F1}GB"
        };

        await SendToAlertingSystemAsync(alert);
    }

    #endregion

    #region Helper Methods

    private string CalculateBreachSeverity(SlaBreachAlert breach)
    {
        var breachPercentage = breach.BreachAmount.TotalMilliseconds / breach.SlaTarget.TotalMilliseconds * 100;

        return breachPercentage switch
        {
            >= 100 => "Critical", // Breach is 100%+ of target
            >= 50 => "High",      // Breach is 50-100% of target
            >= 25 => "Medium",    // Breach is 25-50% of target
            _ => "Low"            // Breach is less than 25% of target
        };
    }

    private List<string> GenerateActionItems(SlaBreachAlert breach)
    {
        var actions = new List<string>();

        switch (breach.JobType)
        {
            case "MarketImport":
                actions.Add("Check data file size and complexity");
                actions.Add("Verify database connection performance");
                actions.Add("Consider increasing batch size or worker count");
                break;

            case "AllMarketsImport":
                actions.Add("Review parallel processing configuration");
                actions.Add("Check individual market job performance");
                actions.Add("Consider staggered execution timing");
                break;

            default:
                actions.Add("Review job logs for performance bottlenecks");
                actions.Add("Check system resource utilization");
                break;
        }

        return actions;
    }

    private async Task StoreSlaMetricsAsync(SlaComplianceMetrics metrics)
    {
        // This would store metrics in your time-series database
        await Task.Delay(50); // Simulate storage operation
    }

    #endregion
}

#region Configuration and Data Models

/// <summary>
/// Configuration for SLA monitoring service
/// </summary>
public class SlaMonitoringConfiguration
{
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MonitoringWindow { get; set; } = TimeSpan.FromHours(1);
    public decimal CriticalComplianceThreshold { get; set; } = 80.0m; // 80%
    public decimal MinProcessingRate { get; set; } = 500; // records/sec
    public decimal MaxErrorRate { get; set; } = 10.0m; // 10%
    public decimal MaxCpuUsage { get; set; } = 80.0m; // 80%
    public long MaxMemoryUsage { get; set; } = 8L * 1024 * 1024 * 1024; // 8GB
}

/// <summary>
/// SLA compliance metrics for time-series storage
/// </summary>
public class SlaComplianceMetrics
{
    public DateTime Timestamp { get; set; }
    public decimal ComplianceRate { get; set; }
    public int TotalJobs { get; set; }
    public int SlaBreachedJobs { get; set; }
    public TimeSpan AverageJobDuration { get; set; }
    public TimeSpan P95JobDuration { get; set; }
    public decimal ProcessingRate { get; set; }
    public Dictionary<string, MarketSlaMetrics> MarketBreakdown { get; set; } = new();
}

/// <summary>
/// Market-specific SLA metrics
/// </summary>
public class MarketSlaMetrics
{
    public int JobCount { get; set; }
    public decimal ComplianceRate { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public int BreachedJobs { get; set; }
}

#endregion

#region Alert Models

public class SlaBreachNotification
{
    public required string JobId { get; set; }
    public required string JobType { get; set; }
    public required string Market { get; set; }
    public DateTime BreachTime { get; set; }
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan SlaTarget { get; set; }
    public TimeSpan BreachAmount { get; set; }
    public required string Severity { get; set; }
    public required string Message { get; set; }
    public List<string> ActionItems { get; set; } = new();
}

public class CriticalComplianceAlert
{
    public decimal ComplianceRate { get; set; }
    public decimal Threshold { get; set; }
    public int TotalJobs { get; set; }
    public int SlaBreachedJobs { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public required string Message { get; set; }
}

public class PerformanceDegradationAlert
{
    public decimal CurrentProcessingRate { get; set; }
    public decimal MinThreshold { get; set; }
    public required string Message { get; set; }
}

public class HighErrorRateAlert
{
    public decimal ErrorRate { get; set; }
    public decimal MaxThreshold { get; set; }
    public int FailedJobs { get; set; }
    public int TotalJobs { get; set; }
    public required string Message { get; set; }
}

public class ResourceUtilizationAlert
{
    public decimal CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public required string Message { get; set; }
}

#endregion