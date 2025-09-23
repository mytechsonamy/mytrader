using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("symbols")]
public class Symbol
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Symbol ticker/code (e.g., "BTCUSDT", "THYAO", "AAPL")
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("ticker")]
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Legacy venue field - kept for backward compatibility
    /// Use MarketId for new implementations
    /// </summary>
    [MaxLength(50)]
    [Column("venue")]
    public string Venue { get; set; } = string.Empty;

    /// <summary>
    /// Legacy asset class field - kept for backward compatibility
    /// Use AssetClassId for new implementations
    /// </summary>
    [MaxLength(20)]
    [Column("asset_class")]
    public string AssetClass { get; set; } = "CRYPTO";

    /// <summary>
    /// Associated asset class (new relationship-based approach)
    /// </summary>
    [Column("asset_class_id")]
    public Guid? AssetClassId { get; set; }

    /// <summary>
    /// Associated market/exchange (new relationship-based approach)
    /// </summary>
    [Column("market_id")]
    public Guid? MarketId { get; set; }

    /// <summary>
    /// Base currency (e.g., "BTC" in BTCUSDT, "TRY" for BIST stocks)
    /// </summary>
    [MaxLength(12)]
    [Column("base_currency")]
    public string? BaseCurrency { get; set; }

    /// <summary>
    /// Quote currency (e.g., "USDT" in BTCUSDT, "TRY" for BIST stocks)
    /// </summary>
    [MaxLength(12)]
    [Column("quote_currency")]
    public string? QuoteCurrency { get; set; }

    /// <summary>
    /// Full company/asset name (e.g., "Turkish Airlines", "Apple Inc.", "Bitcoin")
    /// </summary>
    [MaxLength(200)]
    [Column("full_name")]
    public string? FullName { get; set; }

    /// <summary>
    /// Turkish name for localization support
    /// </summary>
    [MaxLength(200)]
    [Column("full_name_tr")]
    public string? FullNameTurkish { get; set; }

    /// <summary>
    /// Display name for UI
    /// </summary>
    [MaxLength(100)]
    [Column("display")]
    public string? Display { get; set; }

    /// <summary>
    /// Short description or category
    /// </summary>
    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Industry sector (for stocks) or category (for other assets)
    /// </summary>
    [MaxLength(100)]
    [Column("sector")]
    public string? Sector { get; set; }

    /// <summary>
    /// Industry group within sector
    /// </summary>
    [MaxLength(100)]
    [Column("industry")]
    public string? Industry { get; set; }

    /// <summary>
    /// Country of incorporation/listing (e.g., "TR", "US")
    /// </summary>
    [MaxLength(10)]
    [Column("country")]
    public string? Country { get; set; }

    /// <summary>
    /// ISIN code for stocks and bonds
    /// </summary>
    [MaxLength(20)]
    [Column("isin")]
    public string? ISIN { get; set; }

    /// <summary>
    /// Whether the symbol is active and tradeable
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the symbol is tracked for data collection
    /// </summary>
    [Column("is_tracked")]
    public bool IsTracked { get; set; } = false;

    /// <summary>
    /// Whether this symbol is popular/featured
    /// </summary>
    [Column("is_popular")]
    public bool IsPopular { get; set; } = false;

    /// <summary>
    /// Price precision (decimal places for price display)
    /// </summary>
    [Column("price_precision")]
    public int? PricePrecision { get; set; }

    /// <summary>
    /// Quantity precision (decimal places for quantity display)
    /// </summary>
    [Column("quantity_precision")]
    public int? QuantityPrecision { get; set; }

    /// <summary>
    /// Minimum price movement (tick size)
    /// </summary>
    [Column("tick_size", TypeName = "decimal(38,18)")]
    public decimal? TickSize { get; set; }

    /// <summary>
    /// Minimum quantity increment
    /// </summary>
    [Column("step_size", TypeName = "decimal(38,18)")]
    public decimal? StepSize { get; set; }

    /// <summary>
    /// Minimum order value
    /// </summary>
    [Column("min_order_value", TypeName = "decimal(18,8)")]
    public decimal? MinOrderValue { get; set; }

    /// <summary>
    /// Maximum order value
    /// </summary>
    [Column("max_order_value", TypeName = "decimal(18,8)")]
    public decimal? MaxOrderValue { get; set; }

    /// <summary>
    /// 24h trading volume (for ranking/filtering)
    /// </summary>
    [Column("volume_24h", TypeName = "decimal(38,18)")]
    public decimal? Volume24h { get; set; }

    /// <summary>
    /// Market capitalization (for stocks/crypto)
    /// </summary>
    [Column("market_cap", TypeName = "decimal(38,18)")]
    public decimal? MarketCap { get; set; }

    /// <summary>
    /// Current price (cached for performance)
    /// </summary>
    [Column("current_price", TypeName = "decimal(18,8)")]
    public decimal? CurrentPrice { get; set; }

    /// <summary>
    /// 24h price change percentage
    /// </summary>
    [Column("price_change_24h", TypeName = "decimal(10,4)")]
    public decimal? PriceChange24h { get; set; }

    /// <summary>
    /// Last price update timestamp
    /// </summary>
    [Column("price_updated_at")]
    public DateTime? PriceUpdatedAt { get; set; }

    /// <summary>
    /// Symbol-specific metadata as JSON
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Trading configuration as JSON (fees, limits, etc.)
    /// </summary>
    [Column("trading_config", TypeName = "jsonb")]
    public string? TradingConfig { get; set; }

    /// <summary>
    /// Display order for sorting in UI
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("AssetClassId")]
    public AssetClass? AssetClassEntity { get; set; }

    [ForeignKey("MarketId")]
    public Market? Market { get; set; }

    // Helper methods for symbol display and formatting
    public string GetDisplayName()
    {
        return Display ?? FullName ?? Ticker;
    }

    public string GetLocalizedName(string? language = null)
    {
        if (language == "tr" && !string.IsNullOrEmpty(FullNameTurkish))
            return FullNameTurkish;

        return GetDisplayName();
    }

    public bool IsCrypto()
    {
        return AssetClass == "CRYPTO" || AssetClassEntity?.Code == "CRYPTO";
    }

    public bool IsStock()
    {
        return AssetClass?.StartsWith("STOCK") == true ||
               AssetClassEntity?.Code?.StartsWith("STOCK") == true;
    }

    public bool IsTurkishStock()
    {
        return AssetClass == "STOCK_BIST" || AssetClassEntity?.Code == "STOCK_BIST";
    }

    public bool IsUSStock()
    {
        return AssetClass?.StartsWith("STOCK_") == true &&
               (AssetClass.Contains("NASDAQ") || AssetClass.Contains("NYSE")) ||
               AssetClassEntity?.Code?.Contains("NASDAQ") == true ||
               AssetClassEntity?.Code?.Contains("NYSE") == true;
    }

    public decimal? GetFormattedPrice(decimal? price = null)
    {
        var priceValue = price ?? CurrentPrice;
        if (!priceValue.HasValue || !PricePrecision.HasValue)
            return priceValue;

        return Math.Round(priceValue.Value, PricePrecision.Value);
    }
}
