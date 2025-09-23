using MyTrader.Core.DTOs;
using MyTrader.Core.Models;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Orchestrates data requests across multiple data providers
/// Routes requests to appropriate providers based on asset class, market, and availability
/// </summary>
public interface IDataProviderOrchestrator
{
    /// <summary>
    /// Register a data provider
    /// </summary>
    void RegisterProvider(IDataProvider provider);

    /// <summary>
    /// Get all registered providers
    /// </summary>
    IEnumerable<IDataProvider> GetProviders();

    /// <summary>
    /// Get providers for a specific asset class
    /// </summary>
    IEnumerable<IDataProvider> GetProvidersForAssetClass(string assetClass);

    /// <summary>
    /// Get providers for a specific market
    /// </summary>
    IEnumerable<IDataProvider> GetProvidersForMarket(string market);

    /// <summary>
    /// Get the best provider for a symbol based on priority, availability, and health
    /// </summary>
    Task<IDataProvider?> GetBestProviderAsync(Symbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market data with automatic provider selection and failover
    /// </summary>
    Task<UnifiedMarketDataDto?> GetMarketDataAsync(Symbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get batch market data with optimal provider distribution
    /// </summary>
    Task<BatchMarketDataDto> GetBatchMarketDataAsync(IEnumerable<Symbol> symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical data with provider selection
    /// </summary>
    Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(
        Symbol symbol,
        string interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market statistics with provider failover
    /// </summary>
    Task<MarketStatisticsDto?> GetMarketStatisticsAsync(Symbol symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize all providers
    /// </summary>
    Task InitializeAllProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Health check for all providers
    /// </summary>
    Task<Dictionary<string, ComponentHealth>> GetAllProvidersHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event for provider status changes
    /// </summary>
    event EventHandler<ProviderStatusChangedEventArgs> OnProviderStatusChanged;

    /// <summary>
    /// Event for market data updates from real-time providers
    /// </summary>
    event EventHandler<MarketDataUpdateDto> OnMarketDataUpdate;
}

/// <summary>
/// Provider status changed event arguments
/// </summary>
public class ProviderStatusChangedEventArgs : EventArgs
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public ComponentHealth Health { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Provider selection criteria
/// </summary>
public class ProviderSelectionCriteria
{
    public string? PreferredProviderId { get; set; }
    public bool RequireRealTime { get; set; } = false;
    public int MaxDelayMinutes { get; set; } = 15;
    public bool RequireFundamentalData { get; set; } = false;
    public bool RequireOrderBook { get; set; } = false;
    public List<string> ExcludeProviders { get; set; } = new();
    public decimal? MaxCostPer1kCalls { get; set; }
}

/// <summary>
/// Provider performance metrics
/// </summary>
public class ProviderMetrics
{
    public string ProviderId { get; set; } = string.Empty;
    public TimeSpan AverageResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public DateTime LastRequestTime { get; set; }
    public DateTime MetricsStartTime { get; set; }
    public List<string> RecentErrors { get; set; } = new();
}

/// <summary>
/// Data provider configuration
/// </summary>
public class DataProviderConfig
{
    public string ProviderId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public Dictionary<string, object> Settings { get; set; } = new();
    public ProviderSelectionCriteria SelectionCriteria { get; set; } = new();
}