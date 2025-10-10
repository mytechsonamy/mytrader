using MyTrader.Core.Models;

namespace MyTrader.Core.Services.ETL;

/// <summary>
/// Service for discovering and synchronizing symbols across different data sources
/// Ensures symbols table is always in sync with market data sources
/// </summary>
public interface ISymbolSynchronizationService
{
    /// <summary>
    /// Discover missing symbols from market_data table and add them to symbols table
    /// This is the core synchronization operation for missing tickers
    /// </summary>
    Task<SymbolSyncResult> SynchronizeMissingSymbolsAsync(
        SymbolSyncOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discover symbols from external API sources (Yahoo Finance, Alpha Vantage, etc.)
    /// and add new ones to the symbols table
    /// </summary>
    Task<SymbolDiscoveryResult> DiscoverSymbolsFromExternalSourcesAsync(
        List<string> sources,
        SymbolDiscoveryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate existing symbols data quality and fix inconsistencies
    /// </summary>
    Task<SymbolValidationResult> ValidateAndCleanSymbolsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get synchronization status and metrics
    /// </summary>
    Task<SymbolSyncStatus> GetSyncStatusAsync();
}

/// <summary>
/// Configuration options for symbol synchronization
/// </summary>
public class SymbolSyncOptions
{
    public int BatchSize { get; set; } = 1000;
    public int MaxConcurrency { get; set; } = 5;
    public bool AutoEnrichMetadata { get; set; } = true;
    public bool SkipExistingSymbols { get; set; } = true;
    public List<string>? AssetClassFilter { get; set; }
    public List<string>? VenueFilter { get; set; }
    public TimeSpan MaxProcessingTime { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Configuration for symbol discovery from external sources
/// </summary>
public class SymbolDiscoveryOptions
{
    public int MaxSymbolsPerSource { get; set; } = 5000;
    public int RateLimitDelayMs { get; set; } = 100;
    public bool IncludeInactiveSymbols { get; set; } = false;
    public decimal MinMarketCapThreshold { get; set; } = 1000000; // $1M minimum
    public decimal MinVolumeThreshold { get; set; } = 10000; // Minimum daily volume
    public List<string> AssetClassFilter { get; set; } = new();
}

/// <summary>
/// Result of symbol synchronization operation
/// </summary>
public class SymbolSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }

    // Statistics
    public int SymbolsDiscovered { get; set; }
    public int SymbolsAdded { get; set; }
    public int SymbolsUpdated { get; set; }
    public int SymbolsSkipped { get; set; }
    public int TotalMarketDataRecordsProcessed { get; set; }

    // Asset class breakdown
    public Dictionary<string, int> SymbolsByAssetClass { get; set; } = new();

    // Error details
    public List<string> Warnings { get; set; } = new();
    public List<SymbolSyncError> Errors { get; set; } = new();
}

/// <summary>
/// Result of symbol discovery from external sources
/// </summary>
public class SymbolDiscoveryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }

    // Source breakdown
    public Dictionary<string, SourceDiscoveryResult> SourceResults { get; set; } = new();

    // Overall statistics
    public int TotalSymbolsDiscovered { get; set; }
    public int TotalSymbolsAdded { get; set; }
    public int TotalDuplicatesSkipped { get; set; }
}

/// <summary>
/// Discovery result for a specific source
/// </summary>
public class SourceDiscoveryResult
{
    public string SourceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int SymbolsDiscovered { get; set; }
    public int SymbolsAdded { get; set; }
    public int ApiCallsMade { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Symbol validation and cleanup result
/// </summary>
public class SymbolValidationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalSymbolsValidated { get; set; }
    public int SymbolsWithIssues { get; set; }
    public int SymbolsFixed { get; set; }
    public int SymbolsDeactivated { get; set; }

    public List<ValidationIssue> ValidationIssues { get; set; } = new();
}

/// <summary>
/// Individual symbol synchronization error
/// </summary>
public class SymbolSyncError
{
    public string Symbol { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsRetryable { get; set; }
}

/// <summary>
/// Symbol validation issue
/// </summary>
public class ValidationIssue
{
    public Guid SymbolId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool WasFixed { get; set; }
    public string? FixAction { get; set; }
}

/// <summary>
/// Current synchronization status
/// </summary>
public class SymbolSyncStatus
{
    public DateTime LastSyncAt { get; set; }
    public bool IsCurrentlyRunning { get; set; }
    public string? CurrentOperation { get; set; }
    public int ProgressPercentage { get; set; }

    // Statistics
    public int TotalSymbols { get; set; }
    public int TotalActiveSymbols { get; set; }
    public int TotalTrackedSymbols { get; set; }
    public int SymbolsWithoutMarketData { get; set; }
    public int MarketDataRecordsWithoutSymbols { get; set; }

    // Health indicators
    public bool IsHealthy => MarketDataRecordsWithoutSymbols == 0 && SymbolsWithoutMarketData < (TotalSymbols * 0.1);
    public decimal SyncHealthScore => TotalSymbols > 0 ?
        ((decimal)(TotalSymbols - SymbolsWithoutMarketData) / TotalSymbols * 100) : 100;
}