using MyTrader.Core.Models;

namespace MyTrader.Core.Services.ETL;

/// <summary>
/// Service for bootstrapping and populating market and asset class reference data
/// </summary>
public interface IMarketDataBootstrapService
{
    /// <summary>
    /// Initialize asset classes with standard definitions
    /// </summary>
    Task<BootstrapResult> InitializeAssetClassesAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize markets/exchanges with standard definitions
    /// </summary>
    Task<BootstrapResult> InitializeMarketsAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize trading sessions for all markets
    /// </summary>
    Task<BootstrapResult> InitializeTradingSessionsAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize data providers configuration
    /// </summary>
    Task<BootstrapResult> InitializeDataProvidersAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete bootstrap of all reference data
    /// </summary>
    Task<CompleteBootstrapResult> BootstrapAllReferenceDataAsync(
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate reference data consistency
    /// </summary>
    Task<ReferenceDataValidationResult> ValidateReferenceDataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current bootstrap status
    /// </summary>
    Task<BootstrapStatus> GetBootstrapStatusAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a bootstrap operation
/// </summary>
public class BootstrapResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }

    // Statistics
    public int ItemsCreated { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsSkipped { get; set; }
    public int TotalItems { get; set; }

    // Details
    public List<string> CreatedItems { get; set; } = new();
    public List<string> UpdatedItems { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result of complete bootstrap operation
/// </summary>
public class CompleteBootstrapResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }

    // Individual operation results
    public BootstrapResult AssetClassesResult { get; set; } = new();
    public BootstrapResult MarketsResult { get; set; } = new();
    public BootstrapResult TradingSessionsResult { get; set; } = new();
    public BootstrapResult DataProvidersResult { get; set; } = new();

    // Overall statistics
    public int TotalItemsCreated => AssetClassesResult.ItemsCreated + MarketsResult.ItemsCreated +
                                   TradingSessionsResult.ItemsCreated + DataProvidersResult.ItemsCreated;

    public int TotalItemsUpdated => AssetClassesResult.ItemsUpdated + MarketsResult.ItemsUpdated +
                                   TradingSessionsResult.ItemsUpdated + DataProvidersResult.ItemsUpdated;

    public List<string> AllWarnings { get; set; } = new();
    public List<string> AllErrors { get; set; } = new();
}

/// <summary>
/// Reference data validation result
/// </summary>
public class ReferenceDataValidationResult
{
    public bool IsValid { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    // Validation issues
    public List<ValidationIssue> Issues { get; set; } = new();

    // Statistics
    public int AssetClassCount { get; set; }
    public int MarketCount { get; set; }
    public int TradingSessionCount { get; set; }
    public int DataProviderCount { get; set; }

    // Relationships
    public int OrphanedMarkets { get; set; } // Markets without asset classes
    public int OrphanedSessions { get; set; } // Trading sessions without markets
    public int OrphanedProviders { get; set; } // Data providers without markets
}

/// <summary>
/// Current bootstrap status
/// </summary>
public class BootstrapStatus
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsFullyBootstrapped { get; set; }
    public DateTime? LastBootstrapAt { get; set; }

    // Component status
    public bool AssetClassesInitialized { get; set; }
    public bool MarketsInitialized { get; set; }
    public bool TradingSessionsInitialized { get; set; }
    public bool DataProvidersInitialized { get; set; }

    // Counts
    public int AssetClassCount { get; set; }
    public int MarketCount { get; set; }
    public int TradingSessionCount { get; set; }
    public int DataProviderCount { get; set; }

    // Health indicators
    public List<string> MissingComponents { get; set; } = new();
    public List<string> HealthWarnings { get; set; } = new();
}