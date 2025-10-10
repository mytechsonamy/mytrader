using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyTrader.Core.Services.ETL;

/// <summary>
/// Service interface for correcting AssetClass values in the market_data table
/// by mapping them from the corresponding symbols table entries
/// </summary>
public interface IMarketDataAssetClassCorrectionService
{
    /// <summary>
    /// Execute the complete AssetClass correction pipeline with comprehensive monitoring
    /// </summary>
    Task<AssetClassCorrectionResult> ExecuteAssetClassCorrectionAsync(
        AssetClassCorrectionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze the current AssetClass data integrity issues
    /// </summary>
    Task<AssetClassIntegrityReport> AnalyzeAssetClassIntegrityAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview what corrections would be made without actually applying them
    /// </summary>
    Task<AssetClassCorrectionPreview> PreviewCorrectionsAsync(
        AssetClassCorrectionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate the corrections after they have been applied
    /// </summary>
    Task<AssetClassValidationResult> ValidateCorrectionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of the last correction operation
    /// </summary>
    Task<AssetClassCorrectionStatus> GetCorrectionStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the last correction operation if needed
    /// </summary>
    Task<AssetClassRollbackResult> RollbackLastCorrectionAsync(
        string correctionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for AssetClass correction operations
/// </summary>
public class AssetClassCorrectionOptions
{
    /// <summary>
    /// Batch size for processing market data records
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Maximum number of records to process in a single operation
    /// </summary>
    public int? MaxRecordsToProcess { get; set; }

    /// <summary>
    /// Whether to create a backup before making corrections
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Specific symbols to focus the correction on (null for all symbols)
    /// </summary>
    public List<string>? TargetSymbols { get; set; }

    /// <summary>
    /// Whether to process only records with null/empty AssetClass values
    /// </summary>
    public bool OnlyProcessNullAssetClass { get; set; } = true;

    /// <summary>
    /// Whether to overwrite existing non-null AssetClass values
    /// </summary>
    public bool OverwriteExistingValues { get; set; } = false;

    /// <summary>
    /// Retry configuration for failed operations
    /// </summary>
    public RetryConfiguration RetryConfig { get; set; } = new();

    /// <summary>
    /// Whether to run in dry-run mode (no actual changes)
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Timeout for the entire operation
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Whether to validate data integrity after each batch
    /// </summary>
    public bool ValidateAfterEachBatch { get; set; } = true;
}

/// <summary>
/// Retry configuration for failed operations
/// </summary>
public class RetryConfiguration
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
}

/// <summary>
/// Result of the AssetClass correction operation
/// </summary>
public class AssetClassCorrectionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrectionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    // Processing statistics
    public int TotalRecordsAnalyzed { get; set; }
    public int RecordsNeedingCorrection { get; set; }
    public int RecordsSuccessfullyProcessed { get; set; }
    public int RecordsFailed { get; set; }
    public int RecordsSkipped { get; set; }
    public int BatchesProcessed { get; set; }
    public int BatchesFailed { get; set; }

    // Quality metrics
    public decimal DataQualityScoreBefore { get; set; }
    public decimal DataQualityScoreAfter { get; set; }
    public decimal ImprovementPercentage =>
        DataQualityScoreBefore > 0 ?
        ((DataQualityScoreAfter - DataQualityScoreBefore) / DataQualityScoreBefore) * 100 : 0;

    // Error details
    public List<CorrectionError> Errors { get; set; } = new();
    public Dictionary<string, int> ErrorsByCategory { get; set; } = new();

    // Performance metrics
    public Dictionary<string, TimeSpan> PhaseTimings { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();

    // Backup information
    public string? BackupTableName { get; set; }
    public DateTime? BackupCreatedAt { get; set; }

    // Audit trail
    public List<CorrectionAuditEntry> AuditTrail { get; set; } = new();
}

/// <summary>
/// Data integrity analysis report for AssetClass fields
/// </summary>
public class AssetClassIntegrityReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan AnalysisDuration { get; set; }

    // Overall statistics
    public int TotalMarketDataRecords { get; set; }
    public int RecordsWithNullAssetClass { get; set; }
    public int RecordsWithEmptyAssetClass { get; set; }
    public int RecordsWithValidAssetClass { get; set; }
    public int RecordsWithMismatchedAssetClass { get; set; }

    // Symbol mapping analysis
    public int TotalSymbolsReferenced { get; set; }
    public int SymbolsWithValidAssetClass { get; set; }
    public int OrphanedMarketDataRecords { get; set; } // Market data without corresponding symbols
    public int SymbolsWithoutMarketData { get; set; }

    // Data quality metrics
    public decimal AssetClassCompleteness =>
        TotalMarketDataRecords > 0 ?
        (decimal)RecordsWithValidAssetClass / TotalMarketDataRecords * 100 : 100;

    public decimal SymbolMappingIntegrity =>
        TotalMarketDataRecords > 0 ?
        (decimal)(TotalMarketDataRecords - OrphanedMarketDataRecords) / TotalMarketDataRecords * 100 : 100;

    // Issues by category
    public Dictionary<string, List<DataIntegrityIssue>> IssuesByCategory { get; set; } = new();
    public List<AssetClassMismatch> AssetClassMismatches { get; set; } = new();

    // Recommendations
    public List<string> Recommendations { get; set; } = new();
    public EstimatedEffort CorrectionEffort { get; set; } = new();
}

/// <summary>
/// Preview of corrections that would be made
/// </summary>
public class AssetClassCorrectionPreview
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int EstimatedRecordsToProcess { get; set; }
    public int EstimatedBatches { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public List<AssetClassCorrectionItem> SampleCorrections { get; set; } = new();
    public Dictionary<string, int> CorrectionsByAssetClass { get; set; } = new();
    public List<PotentialRisk> IdentifiedRisks { get; set; } = new();
}

/// <summary>
/// Current status of the correction service
/// </summary>
public class AssetClassCorrectionStatus
{
    public bool IsRunning { get; set; }
    public string? CurrentOperationId { get; set; }
    public DateTime? LastOperationStart { get; set; }
    public DateTime? LastOperationEnd { get; set; }
    public bool LastOperationSuccess { get; set; }
    public string? LastOperationError { get; set; }
    public int? CurrentProgress { get; set; }
    public string? CurrentPhase { get; set; }
    public List<AssetClassCorrectionResult> RecentOperations { get; set; } = new();
}

/// <summary>
/// Validation result after corrections have been applied
/// </summary>
public class AssetClassValidationResult
{
    public bool ValidationPassed { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    public int RecordsValidated { get; set; }
    public int ValidationErrors { get; set; }
    public decimal DataQualityScore { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public Dictionary<string, object> ValidationMetrics { get; set; } = new();
}

/// <summary>
/// Result of a rollback operation
/// </summary>
public class AssetClassRollbackResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime RollbackTime { get; set; } = DateTime.UtcNow;
    public string CorrectionIdRolledBack { get; set; } = string.Empty;
    public int RecordsRolledBack { get; set; }
    public string? BackupTableUsed { get; set; }
}

/// <summary>
/// Individual correction error details
/// </summary>
public class CorrectionError
{
    public string ErrorId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MarketDataRecordId { get; set; }
    public string? Symbol { get; set; }
    public string? BatchId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public bool IsRetryable { get; set; }
}

/// <summary>
/// Asset class mismatch information
/// </summary>
public class AssetClassMismatch
{
    public string Symbol { get; set; } = string.Empty;
    public string? MarketDataAssetClass { get; set; }
    public string? SymbolAssetClass { get; set; }
    public int AffectedRecords { get; set; }
    public DateTime FirstDetected { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual correction item for preview
/// </summary>
public class AssetClassCorrectionItem
{
    public Guid MarketDataId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? CurrentAssetClass { get; set; }
    public string ProposedAssetClass { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CorrectionReason { get; set; } = string.Empty;
}

/// <summary>
/// Potential risk identified during preview
/// </summary>
public class PotentialRisk
{
    public string RiskType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Mitigation { get; set; } = string.Empty;
    public int AffectedRecords { get; set; }
}

/// <summary>
/// Estimated effort for corrections
/// </summary>
public class EstimatedEffort
{
    public TimeSpan EstimatedDuration { get; set; }
    public int EstimatedBatches { get; set; }
    public string ComplexityLevel { get; set; } = "Medium"; // Low, Medium, High
    public List<string> RequiredResources { get; set; } = new();
}

/// <summary>
/// Audit trail entry for corrections
/// </summary>
public class CorrectionAuditEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;
    public Guid? MarketDataId { get; set; }
    public string? Symbol { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Validation error details
/// </summary>
public class ValidationError
{
    public string ErrorType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? RecordId { get; set; }
    public string? Symbol { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}