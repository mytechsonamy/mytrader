using MyTrader.Core.DTOs;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Interface for Alpaca market data service
/// </summary>
public interface IAlpacaMarketDataService
{
    /// <summary>
    /// Get live cryptocurrency market data from Alpaca
    /// </summary>
    /// <param name="symbols">Optional symbols to filter (if empty, returns default crypto symbols)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of crypto market data</returns>
    Task<List<AlpacaCryptoDataDto>> GetCryptoMarketDataAsync(
        List<string>? symbols = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get live NASDAQ stock market data from Alpaca
    /// </summary>
    /// <param name="symbols">Optional symbols to filter (if empty, returns default stock symbols)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stock market data</returns>
    Task<List<AlpacaStockDataDto>> GetNasdaqMarketDataAsync(
        List<string>? symbols = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unified market data in frontend format
    /// </summary>
    /// <param name="symbols">Optional symbols to filter</param>
    /// <param name="assetClass">Optional asset class filter (CRYPTO, STOCK)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unified market data for frontend</returns>
    Task<List<MarketDataDto>> GetUnifiedMarketDataAsync(
        List<string>? symbols = null,
        string? assetClass = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical price data for a symbol
    /// </summary>
    /// <param name="symbol">Symbol to get data for</param>
    /// <param name="interval">Time interval (1m, 5m, 1h, 1d)</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical market data</returns>
    Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(
        string symbol,
        string interval,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get market overview data combining crypto and stocks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Market overview data</returns>
    Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get health status of Alpaca API connection
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status</returns>
    Task<AlpacaHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connectivity to Alpaca API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available symbols from Alpaca
    /// </summary>
    /// <param name="assetClass">Optional asset class filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available symbols</returns>
    Task<List<string>> GetAvailableSymbolsAsync(
        string? assetClass = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current rate limit status
    /// </summary>
    /// <returns>Rate limit status</returns>
    AlpacaRateLimitStatus GetRateLimitStatus();

    /// <summary>
    /// Get circuit breaker status
    /// </summary>
    /// <returns>Circuit breaker status</returns>
    AlpacaCircuitBreakerStatus GetCircuitBreakerStatus();
}

/// <summary>
/// Interface for Alpaca data provider implementation
/// </summary>
public interface IAlpacaDataProvider : IDataProvider
{
    /// <summary>
    /// Get real-time crypto quote
    /// </summary>
    Task<AlpacaCryptoDataDto?> GetCryptoQuoteAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time stock quote
    /// </summary>
    Task<AlpacaStockDataDto?> GetStockQuoteAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get latest trades for a symbol
    /// </summary>
    Task<List<TradeDto>> GetLatestTradesAsync(string symbol, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get bars (OHLCV) data
    /// </summary>
    Task<List<CandlestickDataDto>> GetBarsAsync(
        string symbol,
        string timeframe,
        DateTime? start = null,
        DateTime? end = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
}