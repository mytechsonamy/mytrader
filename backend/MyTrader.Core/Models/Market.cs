using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

/// <summary>
/// Market/Exchange information for different trading venues
/// Supports Binance, BIST, NASDAQ, NYSE, etc.
/// </summary>
[Table("markets")]
public class Market
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Market code (e.g., BINANCE, BIST, NASDAQ, NYSE)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full market name (e.g., "Binance", "Borsa Istanbul", "NASDAQ Global Select Market")
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Localized name for Turkish market support
    /// </summary>
    [MaxLength(200)]
    [Column("name_tr")]
    public string? NameTurkish { get; set; }

    /// <summary>
    /// Market description
    /// </summary>
    [MaxLength(1000)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Associated asset class
    /// </summary>
    [Required]
    public Guid AssetClassId { get; set; }

    /// <summary>
    /// Country code where the market is located (e.g., US, TR, UK)
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("country_code")]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Market timezone (e.g., "America/New_York", "Europe/Istanbul")
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("timezone")]
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Primary trading currency for this market
    /// </summary>
    [Required]
    [MaxLength(12)]
    [Column("primary_currency")]
    public string PrimaryCurrency { get; set; } = "USD";

    /// <summary>
    /// Market maker identifier (for internal routing)
    /// </summary>
    [MaxLength(50)]
    [Column("market_maker")]
    public string? MarketMaker { get; set; }

    /// <summary>
    /// Base API URL for this market
    /// </summary>
    [MaxLength(200)]
    [Column("api_base_url")]
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// WebSocket endpoint URL
    /// </summary>
    [MaxLength(200)]
    [Column("websocket_url")]
    public string? WebSocketUrl { get; set; }

    /// <summary>
    /// Default commission rate for trades (as percentage)
    /// </summary>
    [Column("default_commission_rate", TypeName = "decimal(10,6)")]
    public decimal? DefaultCommissionRate { get; set; }

    /// <summary>
    /// Minimum commission amount
    /// </summary>
    [Column("min_commission", TypeName = "decimal(18,8)")]
    public decimal? MinCommission { get; set; }

    /// <summary>
    /// Market specific configuration as JSON
    /// </summary>
    [Column("market_config", TypeName = "jsonb")]
    public string? MarketConfig { get; set; }

    /// <summary>
    /// Market status (OPEN, CLOSED, PRE_MARKET, AFTER_HOURS, HOLIDAY)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "UNKNOWN";

    /// <summary>
    /// Last status update timestamp
    /// </summary>
    [Column("status_updated_at")]
    public DateTime? StatusUpdatedAt { get; set; }

    /// <summary>
    /// Whether this market is active and available for trading
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether real-time data is available for this market
    /// </summary>
    [Column("has_realtime_data")]
    public bool HasRealtimeData { get; set; } = true;

    /// <summary>
    /// Data delay in minutes (0 for real-time)
    /// </summary>
    [Column("data_delay_minutes")]
    public int DataDelayMinutes { get; set; } = 0;

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("AssetClassId")]
    public AssetClass AssetClass { get; set; } = null!;

    public ICollection<TradingSession> TradingSessions { get; set; } = new List<TradingSession>();
    public ICollection<Symbol> Symbols { get; set; } = new List<Symbol>();
    public ICollection<DataProvider> DataProviders { get; set; } = new List<DataProvider>();
}

/// <summary>
/// Enum for market status values
/// </summary>
public enum MarketStatus
{
    UNKNOWN,
    OPEN,
    CLOSED,
    PRE_MARKET,
    AFTER_HOURS,
    HOLIDAY,
    MAINTENANCE
}