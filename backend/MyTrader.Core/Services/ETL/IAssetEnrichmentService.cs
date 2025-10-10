using MyTrader.Core.Models;

namespace MyTrader.Core.Services.ETL;

/// <summary>
/// Service for enriching symbol data with metadata from external sources
/// Handles rate limiting, error recovery, and data quality validation
/// </summary>
public interface IAssetEnrichmentService
{
    /// <summary>
    /// Enrich multiple symbols with metadata from external sources
    /// </summary>
    Task<EnrichmentBatchResult> EnrichSymbolsAsync(
        List<Guid> symbolIds,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrich a single symbol with metadata
    /// </summary>
    Task<EnrichmentResult> EnrichSymbolAsync(
        Guid symbolId,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get enrichment status for symbols
    /// </summary>
    Task<EnrichmentStatus> GetEnrichmentStatusAsync(
        List<Guid>? symbolIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh stale enrichment data
    /// </summary>
    Task<EnrichmentBatchResult> RefreshStaleEnrichmentsAsync(
        TimeSpan staleThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available enrichment sources and their health status
    /// </summary>
    Task<List<EnrichmentSourceStatus>> GetSourceStatusAsync();
}

/// <summary>
/// Configuration for enrichment operations
/// </summary>
public class EnrichmentOptions
{
    public List<string> EnabledSources { get; set; } = new() { "COINMARKETCAP", "ALPHA_VANTAGE", "YAHOO_FINANCE" };
    public int MaxConcurrency { get; set; } = 3;
    public int BatchSize { get; set; } = 50;
    public bool OverwriteExistingData { get; set; } = false;
    public TimeSpan RateLimitDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    // Data selection
    public bool IncludeCompanyInfo { get; set; } = true;
    public bool IncludeMarketCap { get; set; } = true;
    public bool IncludeSectorInfo { get; set; } = true;
    public bool IncludeTradingInfo { get; set; } = true;
    public bool IncludePriceInfo { get; set; } = true;
}

/// <summary>
/// Result of enriching multiple symbols
/// </summary>
public class EnrichmentBatchResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }

    // Statistics
    public int TotalSymbols { get; set; }
    public int SuccessfullyEnriched { get; set; }
    public int PartiallyEnriched { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    // Source breakdown
    public Dictionary<string, SourceEnrichmentResult> SourceResults { get; set; } = new();

    // Detailed results
    public List<EnrichmentResult> SymbolResults { get; set; } = new();

    // Rate limiting and API usage
    public int TotalApiCalls { get; set; }
    public Dictionary<string, int> ApiCallsBySource { get; set; } = new();
    public List<string> RateLimitWarnings { get; set; } = new();
}

/// <summary>
/// Result of enriching a single symbol
/// </summary>
public class EnrichmentResult
{
    public Guid SymbolId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime EnrichedAt { get; set; } = DateTime.UtcNow;

    // What was enriched
    public Dictionary<string, bool> FieldsEnriched { get; set; } = new();
    public Dictionary<string, EnrichmentSourceResult> SourceResults { get; set; } = new();

    // Quality metrics
    public int DataQualityScore { get; set; } = 0; // 0-100
    public List<string> QualityIssues { get; set; } = new();
    public bool NeedsManualReview { get; set; }
}

/// <summary>
/// Result from a specific enrichment source
/// </summary>
public class EnrichmentSourceResult
{
    public string SourceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> EnrichedData { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public bool WasRateLimited { get; set; }
    public int RetryAttempts { get; set; }
}

/// <summary>
/// Aggregated result for a source across multiple symbols
/// </summary>
public class SourceEnrichmentResult
{
    public string SourceName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int RateLimitedRequests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public decimal AverageResponseTime { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Current enrichment status across all symbols
/// </summary>
public class EnrichmentStatus
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsCurrentlyRunning { get; set; }
    public string? CurrentOperation { get; set; }

    // Statistics
    public int TotalSymbols { get; set; }
    public int FullyEnrichedSymbols { get; set; }
    public int PartiallyEnrichedSymbols { get; set; }
    public int UnenrichedSymbols { get; set; }
    public int StaleEnrichments { get; set; }

    // By asset class
    public Dictionary<string, EnrichmentByAssetClass> EnrichmentByAssetClass { get; set; } = new();

    // Health metrics
    public decimal EnrichmentCompleteness => TotalSymbols > 0 ?
        ((decimal)(FullyEnrichedSymbols + PartiallyEnrichedSymbols) / TotalSymbols * 100) : 100;

    public DateTime? LastEnrichmentRun { get; set; }
    public DateTime? NextScheduledRun { get; set; }
}

/// <summary>
/// Enrichment status for a specific asset class
/// </summary>
public class EnrichmentByAssetClass
{
    public string AssetClass { get; set; } = string.Empty;
    public int TotalSymbols { get; set; }
    public int EnrichedSymbols { get; set; }
    public int UnenrichedSymbols { get; set; }
    public decimal CompletionPercentage => TotalSymbols > 0 ? (decimal)EnrichedSymbols / TotalSymbols * 100 : 100;
    public List<string> CommonMissingFields { get; set; } = new();
}

/// <summary>
/// Status of an enrichment source
/// </summary>
public class EnrichmentSourceStatus
{
    public string SourceName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public bool IsEnabled { get; set; }
    public string? HealthMessage { get; set; }
    public DateTime LastSuccessfulCall { get; set; }
    public DateTime LastFailedCall { get; set; }
    public int RemainingRateLimit { get; set; }
    public DateTime RateLimitResetTime { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public decimal SuccessRate { get; set; } // Last 24 hours
    public int TotalCallsToday { get; set; }
    public string? ApiKeyStatus { get; set; }
    public List<string> SupportedAssetClasses { get; set; } = new();
    public Dictionary<string, object> SourceSpecificMetrics { get; set; } = new();
}

/// <summary>
/// Configuration for external data sources
/// </summary>
public class EnrichmentSourceConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public int RateLimitPerMinute { get; set; } = 60;
    public int RateLimitPerHour { get; set; } = 1000;
    public int RateLimitPerDay { get; set; } = 10000;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 10; // Lower = higher priority
    public List<string> SupportedAssetClasses { get; set; } = new();
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Standardized asset metadata structure
/// </summary>
public class AssetMetadata
{
    // Basic Information
    public string? FullName { get; set; }
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    // Market Information
    public decimal? MarketCap { get; set; }
    public decimal? Volume24h { get; set; }
    public decimal? CirculatingSupply { get; set; }
    public decimal? TotalSupply { get; set; }
    public decimal? MaxSupply { get; set; }

    // Trading Information
    public decimal? CurrentPrice { get; set; }
    public decimal? PriceChange24h { get; set; }
    public decimal? PriceChangePercent24h { get; set; }
    public decimal? High24h { get; set; }
    public decimal? Low24h { get; set; }

    // Company Information (for stocks)
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public string? Country { get; set; }
    public string? Exchange { get; set; }
    public DateTime? FoundedDate { get; set; }
    public DateTime? IpoDate { get; set; }

    // Technical Information
    public int? PricePrecision { get; set; }
    public int? QuantityPrecision { get; set; }
    public decimal? TickSize { get; set; }
    public decimal? LotSize { get; set; }
    public decimal? MinTradeAmount { get; set; }
    public decimal? MaxTradeAmount { get; set; }

    // Metadata
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string DataSource { get; set; } = string.Empty;
    public int DataQualityScore { get; set; } = 0;
}