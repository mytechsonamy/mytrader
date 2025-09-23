using MyTrader.Core.Enums;

namespace MyTrader.Core.Models;

/// <summary>
/// Represents a price update for any asset class with comprehensive market data
/// </summary>
public class MultiAssetPriceUpdate
{
    /// <summary>
    /// The type of update message
    /// </summary>
    public string Type { get; set; } = "PriceUpdate";

    /// <summary>
    /// The asset class this update belongs to
    /// </summary>
    public AssetClassCode AssetClass { get; set; }

    /// <summary>
    /// The symbol/ticker of the asset
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Current price of the asset
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 24-hour price change percentage
    /// </summary>
    public decimal Change24h { get; set; }

    /// <summary>
    /// 24-hour trading volume
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// Current market status
    /// </summary>
    public Enums.MarketStatus MarketStatus { get; set; }

    /// <summary>
    /// Timestamp when the price was recorded
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The exchange or data provider source
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata specific to the asset class
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a market status update for a specific market
/// </summary>
public class MarketStatusUpdate
{
    /// <summary>
    /// The type of update message
    /// </summary>
    public string Type { get; set; } = "MarketStatus";

    /// <summary>
    /// The market identifier (e.g., "NASDAQ", "BIST", "BINANCE")
    /// </summary>
    public string Market { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the market
    /// </summary>
    public MarketStatus Status { get; set; }

    /// <summary>
    /// Next time the market will open (if currently closed)
    /// </summary>
    public DateTime? NextOpen { get; set; }

    /// <summary>
    /// Next time the market will close (if currently open)
    /// </summary>
    public DateTime? NextClose { get; set; }

    /// <summary>
    /// Timezone of the market
    /// </summary>
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the status update
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents a bulk update containing multiple price updates for efficient broadcasting
/// </summary>
public class BulkPriceUpdate
{
    /// <summary>
    /// The type of update message
    /// </summary>
    public string Type { get; set; } = "BulkUpdate";

    /// <summary>
    /// The asset class for all updates in this bulk
    /// </summary>
    public AssetClassCode AssetClass { get; set; }

    /// <summary>
    /// Collection of price updates
    /// </summary>
    public List<MultiAssetPriceUpdate> Updates { get; set; } = new();

    /// <summary>
    /// Timestamp when the bulk update was created
    /// </summary>
    public DateTime Timestamp { get; set; }
}