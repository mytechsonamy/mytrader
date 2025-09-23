using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

/// <summary>
/// Data provider configuration for different data sources
/// Supports Binance WebSocket, BIST data feeds, NASDAQ/US market data, etc.
/// </summary>
[Table("data_providers")]
public class DataProvider
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Provider code (e.g., BINANCE_WS, BIST_REALTIME, NASDAQ_BASIC, YAHOO_FINANCE)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Provider display name
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider description
    /// </summary>
    [MaxLength(1000)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Associated market
    /// </summary>
    [Required]
    public Guid MarketId { get; set; }

    /// <summary>
    /// Provider type (REALTIME, DELAYED, HISTORICAL, FUNDAMENTAL)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("provider_type")]
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Data feed type (REST_API, WEBSOCKET, FTP, DATABASE, FILE)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("feed_type")]
    public string FeedType { get; set; } = string.Empty;

    /// <summary>
    /// Primary endpoint URL
    /// </summary>
    [MaxLength(500)]
    [Column("endpoint_url")]
    public string? EndpointUrl { get; set; }

    /// <summary>
    /// WebSocket endpoint URL
    /// </summary>
    [MaxLength(500)]
    [Column("websocket_url")]
    public string? WebSocketUrl { get; set; }

    /// <summary>
    /// Backup/failover endpoint URL
    /// </summary>
    [MaxLength(500)]
    [Column("backup_endpoint_url")]
    public string? BackupEndpointUrl { get; set; }

    /// <summary>
    /// Authentication type (NONE, API_KEY, OAUTH, BASIC_AUTH, CERTIFICATE)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("auth_type")]
    public string AuthType { get; set; } = "NONE";

    /// <summary>
    /// API key or authentication credential (encrypted)
    /// </summary>
    [MaxLength(500)]
    [Column("api_key")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// API secret (encrypted)
    /// </summary>
    [MaxLength(500)]
    [Column("api_secret")]
    public string? ApiSecret { get; set; }

    /// <summary>
    /// Additional authentication parameters as JSON
    /// </summary>
    [Column("auth_config", TypeName = "jsonb")]
    public string? AuthConfig { get; set; }

    /// <summary>
    /// Rate limit per minute
    /// </summary>
    [Column("rate_limit_per_minute")]
    public int? RateLimitPerMinute { get; set; }

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Column("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    [Column("max_retries")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    [Column("retry_delay_ms")]
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Data delay in minutes (0 for real-time)
    /// </summary>
    [Column("data_delay_minutes")]
    public int DataDelayMinutes { get; set; } = 0;

    /// <summary>
    /// Supported data types as JSON array (e.g., ["OHLCV", "TRADES", "ORDERBOOK", "TICKER"])
    /// </summary>
    [Column("supported_data_types", TypeName = "jsonb")]
    public string? SupportedDataTypes { get; set; }

    /// <summary>
    /// Provider specific configuration as JSON
    /// </summary>
    [Column("provider_config", TypeName = "jsonb")]
    public string? ProviderConfig { get; set; }

    /// <summary>
    /// Connection status (CONNECTED, DISCONNECTED, ERROR, MAINTENANCE)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("connection_status")]
    public string ConnectionStatus { get; set; } = "DISCONNECTED";

    /// <summary>
    /// Last successful connection timestamp
    /// </summary>
    [Column("last_connected_at")]
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    [MaxLength(1000)]
    [Column("last_error")]
    public string? LastError { get; set; }

    /// <summary>
    /// Error count in the last hour
    /// </summary>
    [Column("error_count_hourly")]
    public int ErrorCountHourly { get; set; } = 0;

    /// <summary>
    /// Whether this provider is active and available for use
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is the primary provider for the market
    /// </summary>
    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Priority order for provider selection (lower number = higher priority)
    /// </summary>
    [Column("priority")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Cost per 1000 API calls (for usage tracking)
    /// </summary>
    [Column("cost_per_1k_calls", TypeName = "decimal(10,6)")]
    public decimal? CostPer1kCalls { get; set; }

    /// <summary>
    /// Monthly usage limit
    /// </summary>
    [Column("monthly_limit")]
    public int? MonthlyLimit { get; set; }

    /// <summary>
    /// Current monthly usage count
    /// </summary>
    [Column("monthly_usage")]
    public int MonthlyUsage { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("MarketId")]
    public Market Market { get; set; } = null!;

    // Helper methods
    public bool IsConnected => ConnectionStatus == "CONNECTED";

    public bool CanMakeRequest()
    {
        if (!IsActive || !IsConnected)
            return false;

        if (MonthlyLimit.HasValue && MonthlyUsage >= MonthlyLimit.Value)
            return false;

        return true;
    }

    public void IncrementUsage()
    {
        MonthlyUsage++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetMonthlyUsage()
    {
        MonthlyUsage = 0;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Enums for data provider configuration
/// </summary>
public enum DataProviderType
{
    REALTIME,
    DELAYED,
    HISTORICAL,
    FUNDAMENTAL,
    NEWS,
    ANALYTICS
}

public enum FeedType
{
    REST_API,
    WEBSOCKET,
    FTP,
    DATABASE,
    FILE,
    STREAM
}

public enum AuthenticationType
{
    NONE,
    API_KEY,
    OAUTH,
    BASIC_AUTH,
    CERTIFICATE,
    TOKEN
}

public enum ConnectionStatus
{
    CONNECTED,
    DISCONNECTED,
    ERROR,
    MAINTENANCE,
    RATE_LIMITED
}