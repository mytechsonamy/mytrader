using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// BIST market overview for dashboard
/// </summary>
public class BistMarketOverviewDto
{
    public int TotalStocks { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalMarketCap { get; set; }
    public decimal AvgChangePercent { get; set; }
    public int GainersCount { get; set; }
    public int LosersCount { get; set; }
    public int UnchangedCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public string MarketStatus { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
}

/// <summary>
/// BIST top movers data
/// </summary>
public class BistTopMoversDto
{
    public List<BistMoverDto> Gainers { get; set; } = new();
    public List<BistMoverDto> Losers { get; set; } = new();
    public List<BistMoverDto> MostActive { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Individual BIST mover data
/// </summary>
public class BistMoverDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal Volume { get; set; }
    public decimal? MarketCap { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Currency { get; set; } = "TRY";
}

/// <summary>
/// BIST sector performance data
/// </summary>
public class BistSectorPerformanceDto
{
    public string Sector { get; set; } = string.Empty;
    public int StockCount { get; set; }
    public decimal AvgChangePercent { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalMarketCap { get; set; }
    public int Gainers { get; set; }
    public int Losers { get; set; }
    public int PerformanceRank { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// BIST stock search result
/// </summary>
public class BistStockSearchResultDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? ChangePercent { get; set; }
    public decimal? Volume24h { get; set; }
    public decimal? MarketCap { get; set; }
    public float SearchRank { get; set; }
}

/// <summary>
/// BIST historical data response
/// </summary>
public class BistHistoricalDataDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public List<BistCandlestickDto> Candles { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CandleCount { get; set; }
    public string Currency { get; set; } = "TRY";
}

/// <summary>
/// BIST candlestick data point
/// </summary>
public class BistCandlestickDto
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal? TradingValue { get; set; }
    public long? TransactionCount { get; set; }
}

/// <summary>
/// BIST market status
/// </summary>
public class BistMarketStatusDto
{
    public bool IsOpen { get; set; }
    public string Status { get; set; } = string.Empty; // OPEN, CLOSED, PRE_MARKET, AFTER_HOURS
    public DateTime? NextOpen { get; set; }
    public DateTime? NextClose { get; set; }
    public string Timezone { get; set; } = "Europe/Istanbul";
    public DateTime LastUpdated { get; set; }
    public string? StatusMessage { get; set; }
}

/// <summary>
/// BIST cache refresh result
/// </summary>
public class BistCacheRefreshResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SymbolsUpdated { get; set; }
    public TimeSpan RefreshDuration { get; set; }
    public DateTime RefreshTime { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// BIST cache health metrics
/// </summary>
public class BistCacheHealthDto
{
    public bool IsHealthy { get; set; }
    public DateTime LastRefresh { get; set; }
    public TimeSpan DataAge { get; set; }
    public double CacheHitRatio { get; set; }
    public long TotalCacheHits { get; set; }
    public long TotalCacheMisses { get; set; }
    public int CachedSymbolsCount { get; set; }
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// BIST market data update event arguments
/// </summary>
public class BistMarketDataUpdateEventArgs : EventArgs
{
    public List<string> UpdatedSymbols { get; set; } = new();
    public DateTime UpdateTime { get; set; }
    public string UpdateType { get; set; } = string.Empty; // PRICE, OVERVIEW, TOP_MOVERS, SECTORS
    public int AffectedSymbolsCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// BIST-specific market data DTO that matches the unified format
/// This ensures compatibility with existing frontend code
/// </summary>
public class BistMarketDataDto : MarketDataDto
{
    /// <summary>
    /// BIST specific fields
    /// </summary>
    public decimal? TradingValue { get; set; }
    public long? TransactionCount { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public decimal? FreeFloatMarketCap { get; set; }
    public decimal? PreviousClose { get; set; }

    /// <summary>
    /// Turkish localized names
    /// </summary>
    public string? NameTurkish { get; set; }

    /// <summary>
    /// Constructor that sets BIST-specific defaults
    /// </summary>
    public BistMarketDataDto()
    {
        AssetClass = "BIST";
        Currency = "TRY";
    }

    /// <summary>
    /// Create from base MarketDataDto
    /// </summary>
    public static BistMarketDataDto FromMarketDataDto(MarketDataDto baseDto)
    {
        return new BistMarketDataDto
        {
            Symbol = baseDto.Symbol,
            Name = baseDto.Name,
            Price = baseDto.Price,
            Change = baseDto.Change,
            ChangePercent = baseDto.ChangePercent,
            Volume = baseDto.Volume,
            High24h = baseDto.High24h,
            Low24h = baseDto.Low24h,
            LastUpdated = baseDto.LastUpdated,
            MarketCap = baseDto.MarketCap,
            AssetClass = "BIST",
            Currency = "TRY"
        };
    }
}

/// <summary>
/// BIST API endpoint request/response DTOs
/// </summary>
public class BistMarketDataRequest
{
    public List<string>? Symbols { get; set; }
    public int Limit { get; set; } = 50;
    public bool IncludeExtendedData { get; set; } = false;
    public string? SortBy { get; set; } // volume, change, name
    public string? SortDirection { get; set; } = "desc"; // asc, desc
    public string? SectorFilter { get; set; }
}

/// <summary>
/// BIST search request
/// </summary>
public class BistSearchRequest
{
    [Required]
    [MinLength(2)]
    public string SearchTerm { get; set; } = string.Empty;

    public int Limit { get; set; } = 20;
    public string? SectorFilter { get; set; }
    public decimal? MinVolume { get; set; }
    public decimal? MinMarketCap { get; set; }
}

/// <summary>
/// BIST historical data request
/// </summary>
public class BistHistoricalDataRequest
{
    [Required]
    public string Symbol { get; set; } = string.Empty;

    public string Period { get; set; } = "1m"; // 1d, 5d, 1m, 3m, 6m, 1y, 2y, 5y
    public string Interval { get; set; } = "1d"; // 1d, 1w, 1m
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IncludeVolume { get; set; } = true;
    public bool IncludeExtendedData { get; set; } = false;
}