namespace MyTrader.Core.Configuration;

/// <summary>
/// Configuration for market data providers
/// </summary>
public class MarketDataProvidersConfiguration
{
    public Dictionary<string, MarketProviderConfig> Providers { get; set; } = new();
}

/// <summary>
/// Configuration for a specific market provider
/// </summary>
public class MarketProviderConfig
{
    /// <summary>
    /// Provider name (e.g., YahooFinance, Binance, Alpaca)
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Update interval in seconds
    /// </summary>
    public int UpdateIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Fallback provider name if primary fails
    /// </summary>
    public string? FallbackProvider { get; set; }

    /// <summary>
    /// Provider-specific configuration
    /// </summary>
    public ProviderSpecificConfig Configuration { get; set; } = new();
}

/// <summary>
/// Provider-specific configuration options
/// </summary>
public class ProviderSpecificConfig
{
    /// <summary>
    /// Data delay in minutes (0 for real-time)
    /// </summary>
    public int DataDelayMinutes { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Additional custom settings
    /// </summary>
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}
