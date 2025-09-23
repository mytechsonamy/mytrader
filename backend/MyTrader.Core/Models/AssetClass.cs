using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

/// <summary>
/// Asset class metadata for categorizing different types of financial instruments
/// Supports CRYPTO, STOCK_BIST, STOCK_NASDAQ, FOREX, COMMODITY, INDICES
/// </summary>
[Table("asset_classes")]
public class AssetClass
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Asset class code (e.g., CRYPTO, STOCK_BIST, STOCK_NASDAQ, FOREX, COMMODITY, INDICES)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the asset class (e.g., "Cryptocurrency", "Turkish Stocks", "US NASDAQ Stocks")
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Localized name for Turkish market support
    /// </summary>
    [MaxLength(100)]
    [Column("name_tr")]
    public string? NameTurkish { get; set; }

    /// <summary>
    /// Detailed description of the asset class
    /// </summary>
    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Primary currency used for this asset class (e.g., USD, TRY, EUR)
    /// </summary>
    [Required]
    [MaxLength(12)]
    [Column("primary_currency")]
    public string PrimaryCurrency { get; set; } = "USD";

    /// <summary>
    /// Default price precision for this asset class
    /// </summary>
    [Column("default_price_precision")]
    public int DefaultPricePrecision { get; set; } = 8;

    /// <summary>
    /// Default quantity precision for this asset class
    /// </summary>
    [Column("default_quantity_precision")]
    public int DefaultQuantityPrecision { get; set; } = 8;

    /// <summary>
    /// Whether this asset class supports 24/7 trading (like crypto)
    /// </summary>
    [Column("supports_24_7_trading")]
    public bool Supports24x7Trading { get; set; } = false;

    /// <summary>
    /// Whether fractional shares/units are supported
    /// </summary>
    [Column("supports_fractional")]
    public bool SupportsFractional { get; set; } = true;

    /// <summary>
    /// Minimum trade amount in primary currency
    /// </summary>
    [Column("min_trade_amount", TypeName = "decimal(18,8)")]
    public decimal? MinTradeAmount { get; set; }

    /// <summary>
    /// Asset class specific configuration as JSON
    /// </summary>
    [Column("configuration", TypeName = "jsonb")]
    public string? Configuration { get; set; }

    /// <summary>
    /// Regulatory classification (e.g., "regulated", "unregulated", "mifid", "cysec")
    /// </summary>
    [MaxLength(50)]
    [Column("regulatory_class")]
    public string? RegulatoryClass { get; set; }

    /// <summary>
    /// Whether this asset class is active and available for trading
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

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
    public ICollection<Market> Markets { get; set; } = new List<Market>();
    public ICollection<Symbol> Symbols { get; set; } = new List<Symbol>();
}

/// <summary>
/// Enum for commonly used asset class codes
/// </summary>
public enum AssetClassCode
{
    CRYPTO,
    STOCK_BIST,
    STOCK_NASDAQ,
    STOCK_NYSE,
    FOREX,
    COMMODITY,
    INDICES,
    BOND,
    FUND,
    ETF
}