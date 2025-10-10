namespace MyTrader.Core.Services.ETL;

/// <summary>
/// Service for comprehensive ETL monitoring, alerting, and SLA management
/// </summary>
public interface IETLMonitoringService
{
    /// <summary>
    /// Record ETL job execution metrics
    /// </summary>
    Task RecordJobExecutionAsync(
        string jobId,
        string jobType,
        JobExecutionMetrics metrics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check for SLA breaches and trigger alerts
    /// </summary>
    Task<List<SLABreachAlert>> CheckSLABreachesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get comprehensive monitoring dashboard data
    /// </summary>
    Task<ETLMonitoringDashboard> GetMonitoringDashboardAsync(
        TimeRange timeRange = TimeRange.Last24Hours,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send alert notifications
    /// </summary>
    Task SendAlertAsync(
        AlertSeverity severity,
        string alertType,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get system performance metrics
    /// </summary>
    Task<SystemPerformanceMetrics> GetSystemPerformanceMetricsAsync(
        TimeRange timeRange = TimeRange.Last24Hours,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate operational health report
    /// </summary>
    Task<OperationalHealthReport> GenerateHealthReportAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alert history
    /// </summary>
    Task<List<AlertRecord>> GetAlertHistoryAsync(
        int limit = 100,
        AlertSeverity? severity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configure SLA thresholds
    /// </summary>
    Task ConfigureSLAThresholdsAsync(
        Dictionary<string, SLAThreshold> thresholds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Job execution metrics for monitoring
/// </summary>
public class JobExecutionMetrics
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Performance metrics
    public int RecordsProcessed { get; set; }
    public int RecordsPerSecond => Duration.TotalSeconds > 0 ? (int)(RecordsProcessed / Duration.TotalSeconds) : 0;
    public long MemoryUsedMB { get; set; }
    public double CpuUsagePercent { get; set; }

    // Business metrics
    public int SymbolsAdded { get; set; }
    public int SymbolsEnriched { get; set; }
    public int SymbolsSkipped { get; set; }
    public int ErrorCount { get; set; }
    public int RetryCount { get; set; }

    // Quality metrics
    public decimal DataQualityScore { get; set; }
    public List<string> DataIssues { get; set; } = new();

    // External API metrics
    public int TotalApiCalls { get; set; }
    public int FailedApiCalls { get; set; }
    public Dictionary<string, int> ApiCallsBySource { get; set; } = new();

    // Custom metadata
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// SLA breach alert
/// </summary>
public class SLABreachAlert
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string SLAType { get; set; } = string.Empty; // Duration, DataQuality, Availability
    public DateTime BreachTime { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    // Breach details
    public object ExpectedValue { get; set; } = new();
    public object ActualValue { get; set; } = new();
    public string ThresholdType { get; set; } = string.Empty; // Max, Min, Exact
    public TimeSpan BreachDuration { get; set; }

    // Resolution
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// ETL monitoring dashboard data
/// </summary>
public class ETLMonitoringDashboard
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeRange TimeRange { get; set; }

    // Job execution overview
    public JobExecutionOverview JobOverview { get; set; } = new();

    // Performance metrics
    public PerformanceMetrics Performance { get; set; } = new();

    // Data quality metrics
    public DataQualityMetrics DataQuality { get; set; } = new();

    // System health
    public SystemHealthMetrics SystemHealth { get; set; } = new();

    // Alerts and issues
    public AlertSummary AlertSummary { get; set; } = new();

    // Trends and insights
    public List<TrendInsight> TrendInsights { get; set; } = new();
}

/// <summary>
/// Job execution overview for dashboard
/// </summary>
public class JobExecutionOverview
{
    public int TotalJobs { get; set; }
    public int SuccessfulJobs { get; set; }
    public int FailedJobs { get; set; }
    public int RunningJobs { get; set; }
    public decimal SuccessRate => TotalJobs > 0 ? (decimal)SuccessfulJobs / TotalJobs * 100 : 100;

    // Job type breakdown
    public Dictionary<string, int> JobsByType { get; set; } = new();

    // Recent job activity
    public List<RecentJobExecution> RecentJobs { get; set; } = new();

    // SLA compliance
    public decimal SLAComplianceRate { get; set; }
    public int SLABreaches { get; set; }
}

/// <summary>
/// Performance metrics for dashboard
/// </summary>
public class PerformanceMetrics
{
    public TimeSpan AverageExecutionTime { get; set; }
    public TimeSpan MedianExecutionTime { get; set; }
    public TimeSpan P95ExecutionTime { get; set; }
    public long TotalRecordsProcessed { get; set; }
    public int AverageRecordsPerSecond { get; set; }

    // Resource utilization
    public double AverageCpuUsage { get; set; }
    public long AverageMemoryUsageMB { get; set; }
    public double PeakCpuUsage { get; set; }
    public long PeakMemoryUsageMB { get; set; }

    // External API performance
    public Dictionary<string, ApiPerformanceMetrics> ApiMetrics { get; set; } = new();
}

/// <summary>
/// Data quality metrics for dashboard
/// </summary>
public class DataQualityMetrics
{
    public decimal AverageDataQualityScore { get; set; }
    public int TotalDataIssues { get; set; }
    public Dictionary<string, int> IssuesByType { get; set; } = new();

    // Symbol data quality
    public int TotalSymbols { get; set; }
    public int HighQualitySymbols { get; set; }
    public int LowQualitySymbols { get; set; }
    public decimal SymbolQualityRate => TotalSymbols > 0 ? (decimal)HighQualitySymbols / TotalSymbols * 100 : 100;

    // Enrichment coverage
    public int EnrichedSymbols { get; set; }
    public int UnenrichedSymbols { get; set; }
    public decimal EnrichmentCoverage => TotalSymbols > 0 ? (decimal)EnrichedSymbols / TotalSymbols * 100 : 100;
}

/// <summary>
/// System health metrics for dashboard
/// </summary>
public class SystemHealthMetrics
{
    public bool IsHealthy { get; set; }
    public string HealthSummary { get; set; } = string.Empty;
    public decimal OverallHealthScore { get; set; }

    // Component health
    public Dictionary<string, ComponentHealth> ComponentHealth { get; set; } = new();

    // Database connectivity
    public bool DatabaseConnectivity { get; set; }
    public TimeSpan DatabaseResponseTime { get; set; }

    // External service connectivity
    public Dictionary<string, ExternalServiceHealth> ExternalServices { get; set; } = new();
}

/// <summary>
/// Alert summary for dashboard
/// </summary>
public class AlertSummary
{
    public int TotalActiveAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public int InfoAlerts { get; set; }

    // Alert trends
    public List<AlertTrend> AlertTrends { get; set; } = new();

    // Recent alerts
    public List<AlertRecord> RecentAlerts { get; set; } = new();
}

/// <summary>
/// System performance metrics
/// </summary>
public class SystemPerformanceMetrics
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeRange TimeRange { get; set; }

    // Job throughput metrics
    public int JobsPerHour { get; set; }
    public long RecordsPerHour { get; set; }
    public double AverageJobDurationMinutes { get; set; }

    // Error rates
    public decimal JobFailureRate { get; set; }
    public decimal DataErrorRate { get; set; }
    public decimal ApiErrorRate { get; set; }

    // Resource efficiency
    public double ResourceUtilizationScore { get; set; }
    public Dictionary<string, ResourceMetric> ResourceMetrics { get; set; } = new();

    // Performance trends
    public List<PerformanceTrend> Trends { get; set; } = new();
}

/// <summary>
/// Operational health report
/// </summary>
public class OperationalHealthReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ReportPeriod { get; set; } = string.Empty;

    // Executive summary
    public string ExecutiveSummary { get; set; } = string.Empty;
    public decimal OverallHealthScore { get; set; }
    public List<string> KeyFindings { get; set; } = new();

    // Detailed metrics
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();

    // Issues and recommendations
    public List<OperationalIssue> CriticalIssues { get; set; } = new();
    public List<OperationalIssue> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    // Performance summary
    public PerformanceSummary PerformanceSummary { get; set; } = new();

    // Data quality summary
    public DataQualitySummary DataQualitySummary { get; set; } = new();

    // SLA compliance summary
    public SLAComplianceSummary SLAComplianceSummary { get; set; } = new();
}

/// <summary>
/// SLA threshold configuration
/// </summary>
public class SLAThreshold
{
    public string Name { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty; // Duration, DataQuality, ErrorRate
    public object ThresholdValue { get; set; } = new();
    public string ComparisonOperator { get; set; } = string.Empty; // GreaterThan, LessThan, Equals
    public AlertSeverity AlertSeverity { get; set; } = AlertSeverity.Warning;
    public bool IsEnabled { get; set; } = true;
    public TimeSpan EvaluationWindow { get; set; } = TimeSpan.FromMinutes(5);
    public int RequiredBreachCount { get; set; } = 1; // Consecutive breaches before alert
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Time range for metrics
/// </summary>
public enum TimeRange
{
    LastHour,
    Last6Hours,
    Last24Hours,
    LastWeek,
    LastMonth,
    Custom
}

/// <summary>
/// Alert record for history
/// </summary>
public class AlertRecord
{
    public string AlertId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public AlertSeverity Severity { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public string? Component { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

// Supporting data structures
public class RecentJobExecution
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public int RecordsProcessed { get; set; }
}

public class ApiPerformanceMetrics
{
    public string SourceName { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public int SuccessfulCalls { get; set; }
    public int FailedCalls { get; set; }
    public decimal SuccessRate => TotalCalls > 0 ? (decimal)SuccessfulCalls / TotalCalls * 100 : 100;
    public TimeSpan AverageResponseTime { get; set; }
    public int RateLimitHits { get; set; }
}

public class ComponentHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class ExternalServiceHealth
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AlertTrend
{
    public DateTime Timestamp { get; set; }
    public int AlertCount { get; set; }
    public AlertSeverity Severity { get; set; }
}

public class TrendInsight
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = string.Empty; // Improving, Declining, Stable
    public decimal Impact { get; set; } // 0-100 scale
    public string Recommendation { get; set; } = string.Empty;
}

public class ResourceMetric
{
    public string MetricName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double AverageValue { get; set; }
    public double PeakValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class OperationalIssue
{
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public string? Component { get; set; }
    public string? RecommendedAction { get; set; }
    public bool IsResolved { get; set; }
}

public class PerformanceSummary
{
    public int TotalJobsExecuted { get; set; }
    public decimal SuccessRate { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public long TotalRecordsProcessed { get; set; }
    public int AverageThroughput { get; set; } // Records per hour
}

public class DataQualitySummary
{
    public decimal AverageQualityScore { get; set; }
    public int TotalDataIssuesFound { get; set; }
    public int TotalDataIssuesFixed { get; set; }
    public decimal IssueResolutionRate { get; set; }
    public List<string> TopIssueTypes { get; set; } = new();
}

public class SLAComplianceSummary
{
    public decimal OverallComplianceRate { get; set; }
    public int TotalSLABreaches { get; set; }
    public Dictionary<string, decimal> ComplianceByJobType { get; set; } = new();
    public TimeSpan AverageSLAMissMargin { get; set; }
    public List<string> MostFrequentBreachTypes { get; set; } = new();
}