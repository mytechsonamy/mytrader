using MyTrader.Core.DTOs;
using MyTrader.Core.Models;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Unified service interface for all asset types (crypto, stocks, forex, etc.)
/// Provides a consistent API for market data across all asset classes
/// </summary>
public interface IMultiAssetDataService
{
    /// <summary>
    /// Get real-time market data for a symbol
    /// </summary>
    Task<UnifiedMarketDataDto?> GetMarketDataAsync(Guid symbolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time market data for multiple symbols
    /// </summary>
    Task<BatchMarketDataDto> GetBatchMarketDataAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical candlestick data
    /// </summary>
    Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(
        Guid symbolId,
        string interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market statistics and extended information
    /// </summary>
    Task<MarketStatisticsDto?> GetMarketStatisticsAsync(Guid symbolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search symbols across all asset classes
    /// </summary>
    Task<List<SymbolSearchResultDto>> SearchSymbolsAsync(SymbolSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get symbols by asset class
    /// </summary>
    Task<List<SymbolSummaryDto>> GetSymbolsByAssetClassAsync(Guid assetClassId, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get popular/trending symbols across all asset classes
    /// </summary>
    Task<List<SymbolSummaryDto>> GetPopularSymbolsAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top movers (gainers/losers) for an asset class
    /// </summary>
    Task<TopMoversDto> GetTopMoversAsync(string? assetClassCode = null, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top symbols by volume per asset class for market leaders dashboard
    /// </summary>
    Task<List<VolumeLeaderDto>> GetTopByVolumePerAssetClassAsync(int perClass = 8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market overview data for dashboard
    /// </summary>
    Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to real-time updates for symbols
    /// </summary>
    Task<bool> SubscribeToRealtimeUpdatesAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe from real-time updates
    /// </summary>
    Task<bool> UnsubscribeFromRealtimeUpdatesAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event for real-time market data updates
    /// </summary>
    event EventHandler<MarketDataUpdateDto> OnMarketDataUpdate;
}

/// <summary>
/// Service interface for asset class management
/// </summary>
public interface IAssetClassService
{
    /// <summary>
    /// Get all asset classes
    /// </summary>
    Task<List<AssetClassDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active asset classes
    /// </summary>
    Task<List<AssetClassDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get asset class by ID
    /// </summary>
    Task<AssetClassDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get asset class by code
    /// </summary>
    Task<AssetClassDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new asset class
    /// </summary>
    Task<AssetClassDto> CreateAsync(CreateAssetClassRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update asset class
    /// </summary>
    Task<AssetClassDto?> UpdateAsync(Guid id, UpdateAssetClassRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete asset class
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if asset class code is unique
    /// </summary>
    Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for market management
/// </summary>
public interface IMarketService
{
    /// <summary>
    /// Get all markets
    /// </summary>
    Task<List<MarketSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active markets
    /// </summary>
    Task<List<MarketSummaryDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get markets by asset class
    /// </summary>
    Task<List<MarketSummaryDto>> GetByAssetClassAsync(Guid assetClassId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market by ID with full details
    /// </summary>
    Task<MarketDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market by code
    /// </summary>
    Task<MarketDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market status
    /// </summary>
    Task<MarketStatusDto?> GetMarketStatusAsync(Guid marketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all market statuses
    /// </summary>
    Task<List<MarketStatusDto>> GetAllMarketStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new market
    /// </summary>
    Task<MarketDto> CreateAsync(CreateMarketRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update market
    /// </summary>
    Task<MarketDto?> UpdateAsync(Guid id, UpdateMarketRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update market status
    /// </summary>
    Task<bool> UpdateMarketStatusAsync(Guid marketId, string status, string? statusMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete market
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if market code is unique
    /// </summary>
    Task<bool> IsCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for enhanced symbol management
/// </summary>
public interface IEnhancedSymbolService
{
    /// <summary>
    /// Get symbol by ID with full details
    /// </summary>
    Task<EnhancedSymbolDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get symbol by ticker
    /// </summary>
    Task<EnhancedSymbolDto?> GetByTickerAsync(string ticker, string? marketCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get symbols with pagination and filtering
    /// </summary>
    Task<PaginatedResponse<SymbolSummaryDto>> GetSymbolsAsync(BaseListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get symbols by asset class
    /// </summary>
    Task<List<SymbolSummaryDto>> GetByAssetClassAsync(Guid assetClassId, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get symbols by market
    /// </summary>
    Task<List<SymbolSummaryDto>> GetByMarketAsync(Guid marketId, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search symbols with advanced filtering
    /// </summary>
    Task<List<SymbolSearchResultDto>> SearchAsync(SymbolSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tracked symbols
    /// </summary>
    Task<List<SymbolSummaryDto>> GetTrackedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get popular symbols
    /// </summary>
    Task<List<SymbolSummaryDto>> GetPopularAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new symbol
    /// </summary>
    Task<EnhancedSymbolDto> CreateAsync(CreateSymbolRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update symbol
    /// </summary>
    Task<EnhancedSymbolDto?> UpdateAsync(Guid id, UpdateSymbolRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update symbol tracking status
    /// </summary>
    Task<bool> UpdateTrackingStatusAsync(Guid id, bool isTracked, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update symbol tracking status
    /// </summary>
    Task<int> BulkUpdateTrackingStatusAsync(BulkUpdateSymbolTrackingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update symbol price data
    /// </summary>
    Task<bool> UpdatePriceDataAsync(Guid symbolId, decimal? currentPrice, decimal? priceChange24h, decimal? volume24h, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete symbol
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if ticker is unique within market
    /// </summary>
    Task<bool> IsTickerUniqueAsync(string ticker, Guid? marketId = null, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for market status monitoring
/// </summary>
public interface IMarketStatusService
{
    /// <summary>
    /// Get real-time status for all markets
    /// </summary>
    Task<List<MarketStatusDto>> GetAllMarketStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status for a specific market
    /// </summary>
    Task<MarketStatusDto?> GetMarketStatusAsync(string marketCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update market status
    /// </summary>
    Task<bool> UpdateMarketStatusAsync(string marketCode, string status, string? statusMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if market is currently open
    /// </summary>
    Task<bool> IsMarketOpenAsync(string marketCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get next market open/close times
    /// </summary>
    Task<MarketTimingDto?> GetMarketTimingAsync(string marketCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start market status monitoring
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop market status monitoring
    /// </summary>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event for market status changes
    /// </summary>
    event EventHandler<MarketStatusChangedEventArgs> OnMarketStatusChanged;
}

/// <summary>
/// Top movers DTO (gainers/losers)
/// </summary>
public class TopMoversDto
{
    public List<SymbolSummaryDto> Gainers { get; set; } = new();
    public List<SymbolSummaryDto> Losers { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? AssetClassCode { get; set; }
}

/// <summary>
/// Market overview DTO for dashboard
/// </summary>
public class MarketOverviewDto
{
    public int TotalSymbols { get; set; }
    public int TrackedSymbols { get; set; }
    public int ActiveMarkets { get; set; }
    public int OpenMarkets { get; set; }
    public List<AssetClassSummaryDto> AssetClassSummary { get; set; } = new();
    public List<MarketStatusDto> MarketStatuses { get; set; } = new();
    public TopMoversDto TopMovers { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Asset class summary for overview
/// </summary>
public class AssetClassSummaryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SymbolCount { get; set; }
    public int TrackedSymbolCount { get; set; }
    public decimal? TotalMarketCap { get; set; }
    public decimal? AvgPriceChange24h { get; set; }
}

/// <summary>
/// Market timing information
/// </summary>
public class MarketTimingDto
{
    public string MarketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? NextOpen { get; set; }
    public DateTime? NextClose { get; set; }
    public string? NextSessionType { get; set; }
    public TimeSpan? TimeUntilOpen { get; set; }
    public TimeSpan? TimeUntilClose { get; set; }
    public string Timezone { get; set; } = string.Empty;
}

/// <summary>
/// Market status changed event arguments
/// </summary>
public class MarketStatusChangedEventArgs : EventArgs
{
    public string MarketCode { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? StatusMessage { get; set; }
}