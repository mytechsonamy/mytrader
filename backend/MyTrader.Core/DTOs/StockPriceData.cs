using System.ComponentModel.DataAnnotations;
using MyTrader.Core.Enums;
using MyTrader.Core.Interfaces;

namespace MyTrader.Core.Models;

/// <summary>
/// Unified stock price data structure used by both Alpaca and Yahoo services.
/// Ensures frontend receives consistent data regardless of active source.
/// </summary>
public class StockPriceData
{
    // === CORE IDENTIFICATION ===

    /// <summary>
    /// Stock symbol (e.g., "AAPL", "GOOGL")
    /// </summary>
    [Required]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Asset class (always "STOCK" for this integration)
    /// </summary>
    public AssetClassCode AssetClass { get; set; } = AssetClassCode.STOCK;

    /// <summary>
    /// Market venue (NASDAQ, NYSE)
    /// </summary>
    public string Market { get; set; } = string.Empty;

    /// <summary>
    /// Exchange where this asset is traded
    /// </summary>
    public Exchange Exchange { get; set; } = Exchange.UNKNOWN;

    // === PRICE DATA ===

    /// <summary>
    /// Current/last trade price
    /// </summary>
    [Required]
    public decimal Price { get; set; }

    /// <summary>
    /// Previous day's closing price (for calculating change)
    /// </summary>
    public decimal? PreviousClose { get; set; }

    /// <summary>
    /// Price change from previous close (Price - PreviousClose)
    /// </summary>
    public decimal PriceChange { get; set; }

    /// <summary>
    /// Price change percentage ((PriceChange / PreviousClose) * 100)
    /// </summary>
    public decimal PriceChangePercent { get; set; }

    // === EXTENDED PRICE DATA (Optional) ===

    /// <summary>
    /// Session open price
    /// </summary>
    public decimal? OpenPrice { get; set; }

    /// <summary>
    /// Session high price
    /// </summary>
    public decimal? HighPrice { get; set; }

    /// <summary>
    /// Session low price
    /// </summary>
    public decimal? LowPrice { get; set; }

    /// <summary>
    /// Best bid price (Alpaca quotes only)
    /// </summary>
    public decimal? BidPrice { get; set; }

    /// <summary>
    /// Best ask price (Alpaca quotes only)
    /// </summary>
    public decimal? AskPrice { get; set; }

    // === VOLUME DATA ===

    /// <summary>
    /// Trading volume
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// Number of trades (Alpaca bars only)
    /// </summary>
    public int? TradeCount { get; set; }

    // === METADATA ===

    /// <summary>
    /// Data timestamp (from provider)
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Data source: "ALPACA", "YAHOO_FALLBACK"
    /// </summary>
    [Required]
    public string Source { get; set; } = "ALPACA";

    /// <summary>
    /// Whether this is real-time data (Alpaca) or delayed (Yahoo)
    /// </summary>
    public bool IsRealTime => Source == "ALPACA";

    /// <summary>
    /// Data quality score (0-100)
    /// - 100: Real-time Alpaca data
    /// - 80: Fallback Yahoo data (within 5% of expected value)
    /// - 60: Fallback Yahoo data (>5% deviation detected)
    /// </summary>
    public int QualityScore { get; set; } = 100;

    // === MARKET STATUS FIELDS ===

    /// <summary>
    /// Current market status (OPEN, CLOSED, PRE_MARKET, etc.)
    /// </summary>
    public Enums.MarketStatus MarketStatus { get; set; } = Enums.MarketStatus.UNKNOWN;

    /// <summary>
    /// Last time the market status was updated
    /// </summary>
    public DateTime LastUpdateTime { get; set; }

    /// <summary>
    /// Next time the market opens (null for 24/7 markets like crypto)
    /// </summary>
    public DateTime? NextOpenTime { get; set; }

    /// <summary>
    /// Next time the market closes (null for 24/7 markets)
    /// </summary>
    public DateTime? NextCloseTime { get; set; }

    /// <summary>
    /// If market is closed, the reason (e.g., "Weekend", "Holiday", "After-hours")
    /// </summary>
    public string? MarketClosureReason { get; set; }
}
