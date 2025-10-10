namespace MyTrader.Core.Services.ETL;

/// <summary>
/// Main orchestrator for data integrity ETL operations
/// Coordinates symbol synchronization, enrichment, and market data bootstrapping
/// </summary>
public interface IDataIntegrityETLService
{
    /// <summary>
    /// Execute complete data integrity pipeline
    /// </summary>
    Task<DataIntegrityETLResult> ExecuteFullPipelineAsync(
        DataIntegrityETLOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute symbol synchronization only
    /// </summary>
    Task<DataIntegrityETLResult> ExecuteSymbolSyncOnlyAsync(
        SymbolSyncOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute asset enrichment only
    /// </summary>
    Task<DataIntegrityETLResult> ExecuteEnrichmentOnlyAsync(
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute reference data bootstrap only
    /// </summary>
    Task<DataIntegrityETLResult> ExecuteBootstrapOnlyAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get comprehensive status of data integrity across the system
    /// </summary>
    Task<DataIntegrityStatus> GetDataIntegrityStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule recurring ETL operations
    /// </summary>
    Task<string> ScheduleRecurringETLAsync(
        string cronExpression,
        DataIntegrityETLOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get ETL execution history and metrics
    /// </summary>
    Task<ETLExecutionHistory> GetExecutionHistoryAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for the complete ETL pipeline
/// </summary>
public class DataIntegrityETLOptions
{
    // Pipeline control
    public bool ExecuteSymbolSync { get; set; } = true;
    public bool ExecuteAssetEnrichment { get; set; } = true;
    public bool ExecuteReferenceDataBootstrap { get; set; } = true;
    public bool ValidateAfterExecution { get; set; } = true;

    // Component options
    public SymbolSyncOptions? SymbolSyncOptions { get; set; }
    public EnrichmentOptions? EnrichmentOptions { get; set; }
    public bool OverwriteExistingReferenceData { get; set; } = false;

    // Pipeline behavior
    public bool ContinueOnComponentFailure { get; set; } = true;
    public bool ExecuteInParallel { get; set; } = false;
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromHours(4);

    // Notification settings
    public bool SendNotificationOnCompletion { get; set; } = true;
    public bool SendNotificationOnFailure { get; set; } = true;
    public List<string> NotificationRecipients { get; set; } = new();
}

/// <summary>
/// Result of complete ETL pipeline execution
/// </summary>
public class DataIntegrityETLResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }

    // Component results
    public SymbolSyncResult? SymbolSyncResult { get; set; }
    public EnrichmentBatchResult? EnrichmentResult { get; set; }
    public CompleteBootstrapResult? BootstrapResult { get; set; }
    public ReferenceDataValidationResult? ValidationResult { get; set; }

    // Overall statistics
    public int TotalSymbolsProcessed { get; set; }
    public int TotalSymbolsAdded { get; set; }
    public int TotalSymbolsEnriched { get; set; }
    public int TotalReferenceDataItemsCreated { get; set; }

    // Performance metrics
    public Dictionary<string, TimeSpan> ComponentDurations { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();

    // Health and quality indicators
    public decimal DataQualityScore { get; set; }
    public decimal DataCompletenessScore { get; set; }
    public List<string> QualityIssues { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// Current data integrity status across the system
/// </summary>
public class DataIntegrityStatus
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsSystemHealthy { get; set; }
    public string? HealthSummary { get; set; }

    // Component statuses
    public SymbolSyncStatus SymbolSyncStatus { get; set; } = new();
    public EnrichmentStatus EnrichmentStatus { get; set; } = new();
    public BootstrapStatus BootstrapStatus { get; set; } = new();

    // Overall metrics
    public int TotalSymbols { get; set; }
    public int OrphanedMarketDataRecords { get; set; }
    public int SymbolsWithoutMarketData { get; set; }
    public int UnenrichedActiveSymbols { get; set; }

    // Data quality indicators
    public decimal OverallDataQuality { get; set; }
    public decimal SymbolCoverage { get; set; }
    public decimal EnrichmentCoverage { get; set; }
    public decimal ReferenceDataCompleteness { get; set; }

    // Recent activity
    public DateTime? LastFullETLRun { get; set; }
    public DateTime? LastSymbolSync { get; set; }
    public DateTime? LastEnrichmentRun { get; set; }
    public DateTime? NextScheduledRun { get; set; }

    // Issues and recommendations
    public List<DataIntegrityIssue> CriticalIssues { get; set; } = new();
    public List<DataIntegrityIssue> Warnings { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// Data integrity issue detected in the system
/// </summary>
public class DataIntegrityIssue
{
    public string IssueType { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? RecommendedAction { get; set; }
    public bool IsAutoFixable { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// ETL execution history and metrics
/// </summary>
public class ETLExecutionHistory
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<ETLExecutionRecord> ExecutionRecords { get; set; } = new();

    // Summary statistics
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public decimal SuccessRate => TotalExecutions > 0 ? (decimal)SuccessfulExecutions / TotalExecutions * 100 : 100;

    public TimeSpan AverageExecutionTime { get; set; }
    public TimeSpan ShortestExecutionTime { get; set; }
    public TimeSpan LongestExecutionTime { get; set; }

    // Trend analysis
    public List<PerformanceTrend> PerformanceTrends { get; set; } = new();
    public List<string> FrequentIssues { get; set; } = new();
}

/// <summary>
/// Individual ETL execution record
/// </summary>
public class ETLExecutionRecord
{
    public string ExecutionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Execution details
    public string TriggerType { get; set; } = string.Empty; // Manual, Scheduled, API
    public string? TriggeredBy { get; set; }
    public DataIntegrityETLOptions Options { get; set; } = new();

    // Results summary
    public int SymbolsProcessed { get; set; }
    public int SymbolsAdded { get; set; }
    public int SymbolsEnriched { get; set; }
    public int ReferenceItemsCreated { get; set; }

    // Performance metrics
    public Dictionary<string, TimeSpan> ComponentDurations { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Performance trend analysis
/// </summary>
public class PerformanceTrend
{
    public string MetricName { get; set; } = string.Empty;
    public string TrendDirection { get; set; } = string.Empty; // Improving, Declining, Stable
    public decimal TrendPercentage { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<DataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// Data point for trend analysis
/// </summary>
public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public string? Label { get; set; }
}