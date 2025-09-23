using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MyTrader.Core.Models;

/// <summary>
/// Comprehensive historical market data model supporting both standard OHLCV and BIST detailed formats
/// Optimized for time-series queries with proper indexing and partitioning support
/// </summary>
[Table("historical_market_data")]
public class HistoricalMarketData
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to symbol entity for normalization
    /// </summary>
    [Required]
    [Column("symbol_id")]
    public Guid SymbolId { get; set; }

    /// <summary>
    /// Denormalized symbol ticker for query performance (indexed)
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("symbol_ticker")]
    public string SymbolTicker { get; set; } = string.Empty;

    /// <summary>
    /// Data source identifier (BIST, BINANCE, YAHOO, etc.)
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("data_source")]
    public string DataSource { get; set; } = string.Empty;

    /// <summary>
    /// Market identifier for the data (BIST, NASDAQ, etc.)
    /// </summary>
    [MaxLength(20)]
    [Column("market_code")]
    public string? MarketCode { get; set; }

    /// <summary>
    /// Trading date (date component only, timezone normalized)
    /// Primary partition key for time-based partitioning
    /// </summary>
    [Required]
    [Column("trade_date")]
    public DateOnly TradeDate { get; set; }

    /// <summary>
    /// Timeframe for the data (DAILY, WEEKLY, MONTHLY, INTRADAY)
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("timeframe")]
    public string Timeframe { get; set; } = "DAILY";

    /// <summary>
    /// Specific timestamp for intraday data
    /// </summary>
    [Column("timestamp")]
    public DateTime? Timestamp { get; set; }

    // === STANDARD OHLCV DATA (All markets) ===

    /// <summary>
    /// Opening price
    /// </summary>
    [Column("open_price", TypeName = "decimal(18,8)")]
    public decimal? OpenPrice { get; set; }

    /// <summary>
    /// Highest price during period
    /// </summary>
    [Column("high_price", TypeName = "decimal(18,8)")]
    public decimal? HighPrice { get; set; }

    /// <summary>
    /// Lowest price during period
    /// </summary>
    [Column("low_price", TypeName = "decimal(18,8)")]
    public decimal? LowPrice { get; set; }

    /// <summary>
    /// Closing price (unadjusted)
    /// </summary>
    [Column("close_price", TypeName = "decimal(18,8)")]
    public decimal? ClosePrice { get; set; }

    /// <summary>
    /// Adjusted closing price (for splits, dividends)
    /// </summary>
    [Column("adjusted_close_price", TypeName = "decimal(18,8)")]
    public decimal? AdjustedClosePrice { get; set; }

    /// <summary>
    /// Trading volume
    /// </summary>
    [Column("volume", TypeName = "decimal(38,18)")]
    public decimal? Volume { get; set; }

    /// <summary>
    /// Value Weighted Average Price
    /// </summary>
    [Column("vwap", TypeName = "decimal(18,8)")]
    public decimal? VWAP { get; set; }

    // === BIST SPECIFIC DETAILED DATA ===

    /// <summary>
    /// BIST specific code (HGDG_HS_KODU equivalent)
    /// </summary>
    [MaxLength(20)]
    [Column("bist_code")]
    public string? BistCode { get; set; }

    /// <summary>
    /// Previous day's closing price
    /// </summary>
    [Column("previous_close", TypeName = "decimal(18,8)")]
    public decimal? PreviousClose { get; set; }

    /// <summary>
    /// Daily price change amount
    /// </summary>
    [Column("price_change", TypeName = "decimal(18,8)")]
    public decimal? PriceChange { get; set; }

    /// <summary>
    /// Daily price change percentage
    /// </summary>
    [Column("price_change_percent", TypeName = "decimal(10,4)")]
    public decimal? PriceChangePercent { get; set; }

    /// <summary>
    /// Trading value in primary currency
    /// </summary>
    [Column("trading_value", TypeName = "decimal(38,18)")]
    public decimal? TradingValue { get; set; }

    /// <summary>
    /// Number of transactions/trades
    /// </summary>
    [Column("transaction_count")]
    public long? TransactionCount { get; set; }

    /// <summary>
    /// Market capitalization at close
    /// </summary>
    [Column("market_cap", TypeName = "decimal(38,18)")]
    public decimal? MarketCap { get; set; }

    /// <summary>
    /// Free float market cap
    /// </summary>
    [Column("free_float_market_cap", TypeName = "decimal(38,18)")]
    public decimal? FreeFloatMarketCap { get; set; }

    /// <summary>
    /// Outstanding shares count
    /// </summary>
    [Column("shares_outstanding", TypeName = "decimal(38,18)")]
    public decimal? SharesOutstanding { get; set; }

    /// <summary>
    /// Free float shares count
    /// </summary>
    [Column("free_float_shares", TypeName = "decimal(38,18)")]
    public decimal? FreeFloatShares { get; set; }

    // === INDEX AND CURRENCY DATA ===

    /// <summary>
    /// Relevant index value (BIST100, etc.)
    /// </summary>
    [Column("index_value", TypeName = "decimal(18,8)")]
    public decimal? IndexValue { get; set; }

    /// <summary>
    /// Index change percentage
    /// </summary>
    [Column("index_change_percent", TypeName = "decimal(10,4)")]
    public decimal? IndexChangePercent { get; set; }

    /// <summary>
    /// USD/TRY rate for the date (for BIST stocks)
    /// </summary>
    [Column("usd_try_rate", TypeName = "decimal(18,8)")]
    public decimal? UsdTryRate { get; set; }

    /// <summary>
    /// EUR/TRY rate for the date (for BIST stocks)
    /// </summary>
    [Column("eur_try_rate", TypeName = "decimal(18,8)")]
    public decimal? EurTryRate { get; set; }

    // === TECHNICAL INDICATORS (PRE-CALCULATED) ===

    /// <summary>
    /// RSI value for the period
    /// </summary>
    [Column("rsi", TypeName = "decimal(10,4)")]
    public decimal? RSI { get; set; }

    /// <summary>
    /// MACD line value
    /// </summary>
    [Column("macd", TypeName = "decimal(18,8)")]
    public decimal? MACD { get; set; }

    /// <summary>
    /// MACD signal line
    /// </summary>
    [Column("macd_signal", TypeName = "decimal(18,8)")]
    public decimal? MACDSignal { get; set; }

    /// <summary>
    /// Bollinger Band upper
    /// </summary>
    [Column("bollinger_upper", TypeName = "decimal(18,8)")]
    public decimal? BollingerUpper { get; set; }

    /// <summary>
    /// Bollinger Band lower
    /// </summary>
    [Column("bollinger_lower", TypeName = "decimal(18,8)")]
    public decimal? BollingerLower { get; set; }

    /// <summary>
    /// Simple Moving Average (configurable periods)
    /// </summary>
    [Column("sma_20", TypeName = "decimal(18,8)")]
    public decimal? SMA20 { get; set; }

    [Column("sma_50", TypeName = "decimal(18,8)")]
    public decimal? SMA50 { get; set; }

    [Column("sma_200", TypeName = "decimal(18,8)")]
    public decimal? SMA200 { get; set; }

    // === METADATA AND EXTENSIBILITY ===

    /// <summary>
    /// Currency of the price data
    /// </summary>
    [MaxLength(12)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Data quality score (0-100)
    /// </summary>
    [Column("data_quality_score")]
    public int? DataQualityScore { get; set; }

    /// <summary>
    /// Extended attributes as JSON for flexibility
    /// Stores market-specific or source-specific additional data
    /// </summary>
    [Column("extended_data", TypeName = "jsonb")]
    public JsonDocument? ExtendedData { get; set; }

    /// <summary>
    /// Data source metadata (API version, collection timestamp, etc.)
    /// </summary>
    [Column("source_metadata", TypeName = "jsonb")]
    public JsonDocument? SourceMetadata { get; set; }

    /// <summary>
    /// Flags for data characteristics
    /// Bit flags: 1=HasDividend, 2=HasSplit, 4=IsAdjusted, 8=IsHoliday, 16=IsPartialDay
    /// </summary>
    [Column("data_flags")]
    public int DataFlags { get; set; } = 0;

    /// <summary>
    /// Data source priority (1=highest, for deduplication)
    /// </summary>
    [Column("source_priority")]
    public int SourcePriority { get; set; } = 10;

    // === AUDIT FIELDS ===

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("data_collected_at")]
    public DateTime DataCollectedAt { get; set; } = DateTime.UtcNow;

    // === NAVIGATION PROPERTIES ===

    [ForeignKey("SymbolId")]
    public Symbol? Symbol { get; set; }

    // === HELPER METHODS ===

    /// <summary>
    /// Get the most appropriate close price (adjusted if available, otherwise close)
    /// </summary>
    public decimal? GetEffectiveClosePrice()
    {
        return AdjustedClosePrice ?? ClosePrice;
    }

    /// <summary>
    /// Check if this is BIST data with extended fields
    /// </summary>
    public bool IsBistData()
    {
        return DataSource == "BIST" || !string.IsNullOrEmpty(BistCode);
    }

    /// <summary>
    /// Check if this is intraday data
    /// </summary>
    public bool IsIntradayData()
    {
        return Timeframe != "DAILY" && Timeframe != "WEEKLY" && Timeframe != "MONTHLY";
    }

    /// <summary>
    /// Get data completeness percentage
    /// </summary>
    public decimal GetCompletenessScore()
    {
        var requiredFieldsCount = 0;
        var filledFieldsCount = 0;

        // Check standard OHLCV fields
        requiredFieldsCount += 5; // Open, High, Low, Close, Volume
        if (OpenPrice.HasValue) filledFieldsCount++;
        if (HighPrice.HasValue) filledFieldsCount++;
        if (LowPrice.HasValue) filledFieldsCount++;
        if (ClosePrice.HasValue) filledFieldsCount++;
        if (Volume.HasValue) filledFieldsCount++;

        // Additional checks for BIST data
        if (IsBistData())
        {
            requiredFieldsCount += 3; // TradingValue, TransactionCount, MarketCap
            if (TradingValue.HasValue) filledFieldsCount++;
            if (TransactionCount.HasValue) filledFieldsCount++;
            if (MarketCap.HasValue) filledFieldsCount++;
        }

        return requiredFieldsCount > 0 ? (decimal)filledFieldsCount / requiredFieldsCount * 100 : 100;
    }

    /// <summary>
    /// Set data flag
    /// </summary>
    public void SetFlag(HistoricalDataFlags flag)
    {
        DataFlags |= (int)flag;
    }

    /// <summary>
    /// Check if data flag is set
    /// </summary>
    public bool HasFlag(HistoricalDataFlags flag)
    {
        return (DataFlags & (int)flag) != 0;
    }
}

/// <summary>
/// Data characteristics flags
/// </summary>
[Flags]
public enum HistoricalDataFlags
{
    None = 0,
    HasDividend = 1,
    HasSplit = 2,
    IsAdjusted = 4,
    IsHoliday = 8,
    IsPartialDay = 16,
    IsVerified = 32,
    HasExtendedHours = 64,
    IsBackfilled = 128
}