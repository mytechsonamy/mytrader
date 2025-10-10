using MyTrader.Core.DTOs;
using MyTrader.Core.Enums;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Interface for market data providers (Yahoo Finance, Binance, etc.)
/// Provides abstraction for pluggable data sources
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>
    /// Unique provider identifier
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Supported market type (CRYPTO, STOCK_BIST, STOCK_NASDAQ, STOCK_NYSE)
    /// </summary>
    string SupportedMarket { get; }

    /// <summary>
    /// Update interval for polling-based providers
    /// </summary>
    TimeSpan UpdateInterval { get; }

    /// <summary>
    /// Get market prices for specified symbols
    /// </summary>
    /// <param name="symbols">List of symbol tickers to fetch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of market prices</returns>
    Task<List<UnifiedMarketDataDto>> GetPricesAsync(List<string> symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is available and operational
    /// </summary>
    /// <returns>True if provider is available, false otherwise</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get market status (open/closed/pre-market/after-hours)
    /// </summary>
    /// <param name="market">Market code (e.g., BIST, NASDAQ, NYSE)</param>
    /// <returns>Market status information</returns>
    Task<MarketStatusDto> GetMarketStatusAsync(string market);
}
