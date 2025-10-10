using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs;

/// <summary>
/// Optimized market overview DTO for dashboard - minimal data for fast loading
/// </summary>
public class OptimizedMarketOverviewDto
{
    public int TotalSymbols { get; set; }
    public int ActiveMarkets { get; set; }
    public decimal TotalMarketCap { get; set; }
    public decimal TotalVolume24h { get; set; }

    public TopMoverDto TopGainer { get; set; } = new();
    public TopMoverDto TopLoser { get; set; } = new();

    public string MarketStatus { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// Compressed asset class summary
    /// </summary>
    public List<AssetClassSummaryDto> AssetClasses { get; set; } = new();
}

/// <summary>
/// Compressed top mover data
/// </summary>
public class TopMoverDto
{
    public string Ticker { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal Volume { get; set; }
}

/// <summary>
/// Asset class summary for dashboard
/// </summary>
public class AssetClassSummaryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SymbolCount { get; set; }
    public decimal AvgChangePercent { get; set; }
    public decimal TotalVolume { get; set; }
    public int GainersCount { get; set; }
    public int LosersCount { get; set; }
}

/// <summary>
/// Paginated symbols response with search and filtering
/// </summary>
public class PaginatedSymbolsResponse
{
    public List<CompactSymbolDto> Symbols { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
    public SymbolFilterMetadata Filters { get; set; } = new();
    public DateTime CachedAt { get; set; }
    public int CacheValidSeconds { get; set; } = 300; // 5 minutes default
}

/// <summary>
/// Compact symbol data for lists and dropdowns
/// </summary>
public class CompactSymbolDto
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }

    // Current market data (optional for performance)
    public decimal? Price { get; set; }
    public decimal? ChangePercent { get; set; }
    public decimal? Volume24h { get; set; }
    public decimal? MarketCap { get; set; }

    // Frontend formatting hints
    public int PricePrecision { get; set; } = 2;
    public int QuantityPrecision { get; set; } = 2;
    public string Currency { get; set; } = "USD";

    // Trading metadata
    public bool IsActive { get; set; } = true;
    public bool IsPopular { get; set; } = false;
    public string? TradingViewSymbol { get; set; }
}


/// <summary>
/// Symbol filtering metadata
/// </summary>
public class SymbolFilterMetadata
{
    public List<string> AvailableAssetClasses { get; set; } = new();
    public List<string> AvailableMarkets { get; set; } = new();
    public List<string> AvailableSectors { get; set; } = new();
    public decimal MinMarketCap { get; set; }
    public decimal MaxMarketCap { get; set; }
    public decimal MinVolume24h { get; set; }
    public decimal MaxVolume24h { get; set; }
}

/// <summary>
/// Request DTO for paginated symbol queries
/// </summary>
public class SymbolQueryRequest
{
    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }
    public string? AssetClass { get; set; }
    public string? Market { get; set; }
    public string? Sector { get; set; }

    // Price and volume filters
    public decimal? MinMarketCap { get; set; }
    public decimal? MaxMarketCap { get; set; }
    public decimal? MinVolume24h { get; set; }
    public decimal? MaxVolume24h { get; set; }

    // Sorting options
    public string SortBy { get; set; } = "popularity"; // popularity, ticker, price, volume, marketcap, change
    public string SortDirection { get; set; } = "desc"; // asc, desc

    // Include market data
    public bool IncludeMarketData { get; set; } = true;

    // Popular symbols only
    public bool PopularOnly { get; set; } = false;

    // Active symbols only
    public bool ActiveOnly { get; set; } = true;
}

/// <summary>
/// Optimized batch market data response
/// </summary>
public class OptimizedBatchMarketDataDto
{
    public List<CompactMarketDataDto> MarketData { get; set; } = new();
    public BatchMetadata Metadata { get; set; } = new();
    public DateTime CachedAt { get; set; }
    public int CacheValidSeconds { get; set; } = 30; // 30 seconds for real-time data
}

/// <summary>
/// Compact market data for batch responses
/// </summary>
public class CompactMarketDataDto
{
    public Guid SymbolId { get; set; }
    public string Ticker { get; set; } = string.Empty;

    // Essential price data only
    public decimal? Price { get; set; }
    public decimal? PriceChange { get; set; }
    public decimal? PriceChangePercent { get; set; }
    public decimal? Volume { get; set; }

    // Optional extended data (include only when requested)
    public decimal? High24h { get; set; }
    public decimal? Low24h { get; set; }
    public decimal? MarketCap { get; set; }

    public DateTime LastUpdate { get; set; }
    public string DataProvider { get; set; } = string.Empty;
    public bool IsRealTime { get; set; } = true;
}

/// <summary>
/// Batch operation metadata
/// </summary>
public class BatchMetadata
{
    public int RequestedSymbols { get; set; }
    public int SuccessfulSymbols { get; set; }
    public int FailedSymbols { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public DateTime ResponseTimestamp { get; set; }
}

/// <summary>
/// Top movers response optimized for dashboard
/// </summary>
public class OptimizedTopMoversDto
{
    public List<CompactMarketDataDto> Gainers { get; set; } = new();
    public List<CompactMarketDataDto> Losers { get; set; } = new();
    public List<CompactMarketDataDto> MostActive { get; set; } = new();

    public DateTime LastUpdate { get; set; }
    public int CacheValidSeconds { get; set; } = 300; // 5 minutes

    // Asset class breakdown
    public Dictionary<string, AssetClassMoversDto> ByAssetClass { get; set; } = new();
}

/// <summary>
/// Asset class specific movers
/// </summary>
public class AssetClassMoversDto
{
    public List<CompactMarketDataDto> TopGainers { get; set; } = new();
    public List<CompactMarketDataDto> TopLosers { get; set; } = new();
    public decimal AvgChangePercent { get; set; }
    public int TotalSymbols { get; set; }
}

/// <summary>
/// Symbol search response with ranking
/// </summary>
public class SymbolSearchResponse
{
    public List<SearchResultDto> Results { get; set; } = new();
    public SearchMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Search result with relevance ranking
/// </summary>
public class SearchResultDto
{
    public Guid SymbolId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }

    // Current market data
    public decimal? Price { get; set; }
    public decimal? ChangePercent { get; set; }
    public decimal? Volume24h { get; set; }

    // Search relevance
    public float SearchRank { get; set; }
    public List<string> MatchedFields { get; set; } = new();

    // Popularity indicators
    public bool IsPopular { get; set; }
    public int PopularityRank { get; set; }
}

/// <summary>
/// Search metadata
/// </summary>
public class SearchMetadata
{
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public TimeSpan SearchTime { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public bool HasMore { get; set; }
}

/// <summary>
/// News feed optimized for dashboard
/// </summary>
public class OptimizedNewsDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string? ImageUrl { get; set; }
    public string? Url { get; set; }
    public List<string> Tags { get; set; } = new();

    // Related symbols
    public List<string> RelatedSymbols { get; set; } = new();

    // Content priority for dashboard
    public int Priority { get; set; } = 0; // 0=normal, 1=important, 2=breaking
    public bool IsBreaking { get; set; } = false;
}

/// <summary>
/// Leaderboard data optimized for dashboard
/// </summary>
public class OptimizedLeaderboardDto
{
    public List<LeaderboardEntryDto> Entries { get; set; } = new();
    public LeaderboardMetadata Metadata { get; set; } = new();
    public DateTime LastUpdate { get; set; }
    public int CacheValidSeconds { get; set; } = 3600; // 1 hour
}

/// <summary>
/// Leaderboard entry
/// </summary>
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int TradesCount { get; set; }
    public decimal WinRate { get; set; }
    public decimal SharpeRatio { get; set; }

    // Period performance
    public decimal DailyReturn { get; set; }
    public decimal WeeklyReturn { get; set; }
    public decimal MonthlyReturn { get; set; }

    // Badge/achievements
    public List<string> Badges { get; set; } = new();
    public bool IsVerified { get; set; } = false;
}

/// <summary>
/// Leaderboard metadata
/// </summary>
public class LeaderboardMetadata
{
    public string Period { get; set; } = "monthly"; // daily, weekly, monthly, yearly, all-time
    public int TotalParticipants { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal MinTradesRequired { get; set; } = 5;
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Cache control headers for API responses
/// </summary>
public class CacheControlOptions
{
    public int MaxAgeSeconds { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool MustRevalidate { get; set; } = false;
    public string? ETag { get; set; }
    public DateTime? LastModified { get; set; }
    public bool EnableCompression { get; set; } = true;
    public string CompressionType { get; set; } = "gzip"; // gzip, brotli
}