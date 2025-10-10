using MyTrader.Core.DTOs;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// High-performance BIST (Borsa Istanbul) market data service interface
/// Optimized for sub-100ms response times and efficient caching
/// </summary>
public interface IBistMarketDataService
{
    /// <summary>
    /// Get all BIST stocks with current market data
    /// Target: < 50ms response time
    /// </summary>
    /// <param name="symbols">Optional symbol filter</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of BIST market data</returns>
    Task<List<MarketDataDto>> GetBistMarketDataAsync(
        List<string>? symbols = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get individual BIST stock data
    /// Target: < 10ms response time
    /// </summary>
    /// <param name="symbol">Stock symbol (e.g., "THYAO")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stock market data</returns>
    Task<MarketDataDto?> GetBistStockDataAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get BIST market overview for dashboard
    /// Target: < 100ms response time
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Market overview data</returns>
    Task<BistMarketOverviewDto> GetBistMarketOverviewAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get BIST top movers (gainers, losers, most active)
    /// Target: < 75ms response time
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Top movers data</returns>
    Task<BistTopMoversDto> GetBistTopMoversAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get BIST sector performance
    /// Target: < 100ms response time
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sector performance data</returns>
    Task<List<BistSectorPerformanceDto>> GetBistSectorPerformanceAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search BIST stocks by symbol or name
    /// Target: < 50ms response time
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="limit">Maximum results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    Task<List<BistStockSearchResultDto>> SearchBistStocksAsync(
        string searchTerm,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get BIST historical data for a symbol
    /// </summary>
    /// <param name="symbol">Stock symbol</param>
    /// <param name="period">Period (1d, 5d, 1m, 3m, 6m, 1y)</param>
    /// <param name="interval">Interval (1d, 1w)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical data</returns>
    Task<BistHistoricalDataDto?> GetBistHistoricalDataAsync(
        string symbol,
        string period = "1m",
        string interval = "1d",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if BIST market is currently open
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Market status</returns>
    Task<BistMarketStatusDto> GetBistMarketStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh BIST data cache
    /// Should be called every 30-60 seconds during market hours
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refresh result</returns>
    Task<BistCacheRefreshResultDto> RefreshBistDataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache health and performance metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache health metrics</returns>
    Task<BistCacheHealthDto> GetCacheHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when BIST market data is updated
    /// </summary>
    event EventHandler<BistMarketDataUpdateEventArgs> OnBistDataUpdated;
}