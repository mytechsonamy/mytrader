using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Unified market data DTO for all asset classes
/// </summary>
public class UnifiedMarketDataDto
{
    public Guid SymbolId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string AssetClassCode { get; set; } = string.Empty;
    public string MarketCode { get; set; } = string.Empty;

    /// <summary>
    /// Current price
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Previous day's closing price
    /// </summary>
    public decimal? PreviousClose { get; set; }

    /// <summary>
    /// Opening price for current session
    /// </summary>
    public decimal? OpenPrice { get; set; }

    /// <summary>
    /// Highest price in current session
    /// </summary>
    public decimal? HighPrice { get; set; }

    /// <summary>
    /// Lowest price in current session
    /// </summary>
    public decimal? LowPrice { get; set; }

    /// <summary>
    /// Trading volume in current session
    /// </summary>
    public decimal? Volume { get; set; }

    /// <summary>
    /// Trading volume in quote currency
    /// </summary>
    public decimal? VolumeQuote { get; set; }

    /// <summary>
    /// Average trading price (VWAP)
    /// </summary>
    public decimal? VolumeWeightedAvgPrice { get; set; }

    /// <summary>
    /// Price change from previous close
    /// </summary>
    public decimal? PriceChange { get; set; }

    /// <summary>
    /// Price change percentage from previous close
    /// </summary>
    public decimal? PriceChangePercent { get; set; }

    /// <summary>
    /// 24h price change percentage
    /// </summary>
    public decimal? PriceChange24h { get; set; }

    /// <summary>
    /// 24h trading volume
    /// </summary>
    public decimal? Volume24h { get; set; }

    /// <summary>
    /// Best bid price
    /// </summary>
    public decimal? BidPrice { get; set; }

    /// <summary>
    /// Best bid quantity
    /// </summary>
    public decimal? BidQuantity { get; set; }

    /// <summary>
    /// Best ask price
    /// </summary>
    public decimal? AskPrice { get; set; }

    /// <summary>
    /// Best ask quantity
    /// </summary>
    public decimal? AskQuantity { get; set; }

    /// <summary>
    /// Bid-ask spread
    /// </summary>
    public decimal? Spread { get; set; }

    /// <summary>
    /// Market capitalization (for stocks/crypto)
    /// </summary>
    public decimal? MarketCap { get; set; }

    /// <summary>
    /// Number of trades in current session
    /// </summary>
    public long? TradeCount { get; set; }

    /// <summary>
    /// Last trade timestamp
    /// </summary>
    public DateTime? LastTradeTime { get; set; }

    /// <summary>
    /// Data timestamp from the data provider
    /// </summary>
    public DateTime DataTimestamp { get; set; }

    /// <summary>
    /// Local timestamp when data was received
    /// </summary>
    public DateTime ReceivedTimestamp { get; set; }

    /// <summary>
    /// Data provider that supplied this data
    /// </summary>
    public string? DataProvider { get; set; }

    /// <summary>
    /// Whether this is real-time or delayed data
    /// </summary>
    public bool IsRealTime { get; set; }

    /// <summary>
    /// Data delay in minutes (0 for real-time)
    /// </summary>
    public int DataDelayMinutes { get; set; }

    /// <summary>
    /// Market status (OPEN, CLOSED, PRE_MARKET, etc.)
    /// </summary>
    public string MarketStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the market is currently open
    /// </summary>
    public bool IsMarketOpen { get; set; }

    /// <summary>
    /// Currency of the price data
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Display precision for prices
    /// </summary>
    public int PricePrecision { get; set; } = 8;

    /// <summary>
    /// Display precision for quantities
    /// </summary>
    public int QuantityPrecision { get; set; } = 8;
}

/// <summary>
/// Batch market data DTO for dashboard updates
/// </summary>
public class BatchMarketDataDto
{
    public List<UnifiedMarketDataDto> MarketData { get; set; } = new();
    public DateTime RequestTimestamp { get; set; }
    public DateTime ResponseTimestamp { get; set; }
    public int TotalSymbols { get; set; }
    public int SuccessfulSymbols { get; set; }
    public int FailedSymbols { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Real-time market data update DTO for SignalR
/// </summary>
public class MarketDataUpdateDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? PriceChange { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public decimal? Volume { get; set; }
    public DateTime Timestamp { get; set; }
    public string? DataProvider { get; set; }
}

/// <summary>
/// Historical market data DTO
/// </summary>
public class HistoricalMarketDataDto
{
    public Guid SymbolId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty; // 1m, 5m, 1h, 1d, etc.
    public List<CandlestickDataDto> Candles { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int CandleCount { get; set; }
}

/// <summary>
/// Candlestick data DTO
/// </summary>
public class CandlestickDataDto
{
    public DateTime OpenTime { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateTime CloseTime { get; set; }
    public decimal? VolumeQuote { get; set; }
    public long? TradeCount { get; set; }
}

/// <summary>
/// Market data request DTO
/// </summary>
public class MarketDataRequest
{
    [Required]
    public List<Guid> SymbolIds { get; set; } = new();

    public bool IncludeOrderBook { get; set; } = false;
    public bool IncludeTradeHistory { get; set; } = false;
    public bool IncludeExtendedHours { get; set; } = false;
    public string? DataProvider { get; set; }
}

/// <summary>
/// Historical data request DTO
/// </summary>
public class HistoricalDataRequest
{
    [Required]
    public Guid SymbolId { get; set; }

    [Required]
    public string Interval { get; set; } = "1d"; // 1m, 5m, 15m, 1h, 4h, 1d, 1w, 1M

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Limit { get; set; } = 500;
    public string? DataProvider { get; set; }
}

/// <summary>
/// Market statistics DTO
/// </summary>
public class MarketStatisticsDto
{
    public Guid SymbolId { get; set; }
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// 52-week high
    /// </summary>
    public decimal? High52Week { get; set; }

    /// <summary>
    /// 52-week low
    /// </summary>
    public decimal? Low52Week { get; set; }

    /// <summary>
    /// Average daily volume (30 days)
    /// </summary>
    public decimal? AvgVolume30D { get; set; }

    /// <summary>
    /// Price volatility (30 days)
    /// </summary>
    public decimal? Volatility30D { get; set; }

    /// <summary>
    /// Beta coefficient (for stocks)
    /// </summary>
    public decimal? Beta { get; set; }

    /// <summary>
    /// Price-to-earnings ratio (for stocks)
    /// </summary>
    public decimal? PERatio { get; set; }

    /// <summary>
    /// Dividend yield (for stocks)
    /// </summary>
    public decimal? DividendYield { get; set; }

    /// <summary>
    /// Market dominance (for crypto)
    /// </summary>
    public decimal? MarketDominance { get; set; }

    /// <summary>
    /// Circulating supply (for crypto)
    /// </summary>
    public decimal? CirculatingSupply { get; set; }

    /// <summary>
    /// Total supply (for crypto)
    /// </summary>
    public decimal? TotalSupply { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }
}