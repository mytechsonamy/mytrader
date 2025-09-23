namespace MyTrader.Core.DTOs;

/// <summary>
/// Request for market-specific data import job
/// </summary>
public class MarketImportJobRequest
{
    public required string Market { get; set; }
    public required string DataPath { get; set; }
    public int BatchSize { get; set; } = 1000;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan SlaTarget { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxConcurrency { get; set; } = 4;
    public bool EnableDuplicateCleanup { get; set; } = true;
    public List<string>? IncludeFiles { get; set; }
    public List<string>? ExcludeFiles { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? JobPriority { get; set; } = "Normal";
}

/// <summary>
/// Request for all markets parallel import
/// </summary>
public class AllMarketsImportRequest
{
    public required string StockScrapperDataPath { get; set; }
    public Dictionary<string, MarketConfig> MarketConfigs { get; set; } = new();
    public bool RunMarketsInParallel { get; set; } = true;
    public int GlobalMaxConcurrency { get; set; } = 8;
    public TimeSpan GlobalSlaTarget { get; set; } = TimeSpan.FromHours(2);
    public bool EnableGlobalCleanup { get; set; } = true;
    public string? JobPriority { get; set; } = "High";
}

/// <summary>
/// Market-specific configuration
/// </summary>
public class MarketConfig
{
    public int BatchSize { get; set; } = 1000;
    public int MaxConcurrency { get; set; } = 4;
    public TimeSpan SlaTarget { get; set; } = TimeSpan.FromMinutes(30);
    public int ExpectedRecordsPerSecond { get; set; } = 1000;
    public bool EnableDuplicateCleanup { get; set; } = true;
    public List<string>? IncludeFiles { get; set; }
    public List<string>? ExcludeFiles { get; set; }
}

/// <summary>
/// Batch job execution result
/// </summary>
public class BatchJobBatchResult
{
    public List<string> JobIds { get; set; } = new();
    public string? ParentJobId { get; set; }
    public Dictionary<string, string> MarketJobIds { get; set; } = new();
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DateTime ScheduledAt { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
}

/// <summary>
/// Job status and progress information
/// </summary>
public class BatchJobStatus
{
    public required string JobId { get; set; }
    public required string JobType { get; set; }
    public required JobState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt - StartedAt;
    public int ProgressPercentage { get; set; }
    public string? CurrentOperation { get; set; }
    public long RecordsProcessed { get; set; }
    public long RecordsTotal { get; set; }
    public decimal ProcessingRate { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public TimeSpan? SlaTarget { get; set; }
    public bool IsSlaBreached { get; set; }
    public Dictionary<string, object> JobMetadata { get; set; } = new();
    public List<string> ChildJobIds { get; set; } = new();
    public string? ParentJobId { get; set; }
}

/// <summary>
/// Job states
/// </summary>
public enum JobState
{
    Enqueued,
    Processing,
    Succeeded,
    Failed,
    Cancelled,
    Retrying,
    Scheduled,
    SlaBreached
}

/// <summary>
/// Comprehensive job monitoring statistics
/// </summary>
public class BatchJobMonitoringStats
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalJobs { get; set; }
    public int SuccessfulJobs { get; set; }
    public int FailedJobs { get; set; }
    public int RetryingJobs { get; set; }
    public int SlaBreachedJobs { get; set; }
    public decimal SuccessRate => TotalJobs > 0 ? (decimal)SuccessfulJobs / TotalJobs * 100 : 0;
    public decimal SlaComplianceRate => TotalJobs > 0 ? (decimal)(TotalJobs - SlaBreachedJobs) / TotalJobs * 100 : 0;
    public TimeSpan AverageJobDuration { get; set; }
    public TimeSpan P95JobDuration { get; set; }
    public long TotalRecordsProcessed { get; set; }
    public decimal AverageProcessingRate { get; set; }
    public Dictionary<string, int> JobsByMarket { get; set; } = new();
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public List<SlaBreachAlert> RecentSlaBreaches { get; set; } = new();
    public ResourceUtilization ResourceStats { get; set; } = new();
}

/// <summary>
/// SLA breach alert information
/// </summary>
public class SlaBreachAlert
{
    public required string JobId { get; set; }
    public required string JobType { get; set; }
    public required string Market { get; set; }
    public DateTime BreachTime { get; set; }
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan SlaTarget { get; set; }
    public TimeSpan BreachAmount => ActualDuration - SlaTarget;
    public string? Reason { get; set; }
    public bool AlertSent { get; set; }
}

/// <summary>
/// Resource utilization statistics
/// </summary>
public class ResourceUtilization
{
    public decimal AverageCpuUsage { get; set; }
    public decimal PeakCpuUsage { get; set; }
    public long AverageMemoryUsage { get; set; }
    public long PeakMemoryUsage { get; set; }
    public int ConcurrentJobsPeak { get; set; }
    public decimal DatabaseConnectionUsage { get; set; }
}

/// <summary>
/// Failed job information for dead letter processing
/// </summary>
public class BatchJobFailure
{
    public required string JobId { get; set; }
    public required string JobType { get; set; }
    public required string Market { get; set; }
    public DateTime FailedAt { get; set; }
    public required string ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public int RetryCount { get; set; }
    public bool IsRetryable { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public Dictionary<string, object> JobParameters { get; set; } = new();
    public List<string> ErrorHistory { get; set; } = new();
}

/// <summary>
/// Job retry options
/// </summary>
public class RetryJobOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool ResetRetryCount { get; set; } = false;
    public Dictionary<string, object>? UpdatedParameters { get; set; }
}

/// <summary>
/// Job priority levels
/// </summary>
public static class JobPriority
{
    public const string Critical = "Critical";
    public const string High = "High";
    public const string Normal = "Normal";
    public const string Low = "Low";
    public const string Background = "Background";
}