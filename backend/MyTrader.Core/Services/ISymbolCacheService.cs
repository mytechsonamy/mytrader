using MyTrader.Core.Models;

namespace MyTrader.Core.Services;

/// <summary>
/// Thread-safe caching service for symbols.
/// Uses IMemoryCache for high-performance in-memory caching.
/// </summary>
public interface ISymbolCacheService
{
    /// <summary>
    /// Get cached symbols by key.
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>Cached symbols or null if not found/expired</returns>
    List<Symbol>? GetCachedSymbols(string cacheKey);

    /// <summary>
    /// Get cached single symbol by key.
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>Cached symbol or null if not found/expired</returns>
    Symbol? GetCachedSymbol(string cacheKey);

    /// <summary>
    /// Set symbols in cache with expiration.
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="symbols">Symbols to cache</param>
    /// <param name="expirationMinutes">Cache expiration in minutes (default: 5)</param>
    void SetCachedSymbols(string cacheKey, List<Symbol> symbols, int expirationMinutes = 5);

    /// <summary>
    /// Set single symbol in cache with expiration.
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="symbol">Symbol to cache</param>
    /// <param name="expirationMinutes">Cache expiration in minutes (default: 5)</param>
    void SetCachedSymbol(string cacheKey, Symbol symbol, int expirationMinutes = 5);

    /// <summary>
    /// Clear all symbol caches.
    /// Use this when symbols are updated or for hot-reload.
    /// </summary>
    void ClearAllCaches();

    /// <summary>
    /// Clear specific cache entry.
    /// </summary>
    /// <param name="cacheKey">Cache key to remove</param>
    void ClearCache(string cacheKey);

    /// <summary>
    /// Generate cache key for broadcast symbols.
    /// </summary>
    /// <param name="assetClass">Asset class code</param>
    /// <param name="market">Market code</param>
    /// <returns>Cache key</returns>
    string GetBroadcastCacheKey(string assetClass, string market);

    /// <summary>
    /// Generate cache key for default symbols.
    /// </summary>
    /// <param name="assetClass">Asset class code (optional)</param>
    /// <returns>Cache key</returns>
    string GetDefaultsCacheKey(string? assetClass = null);

    /// <summary>
    /// Generate cache key for user symbols.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="assetClass">Asset class code (optional)</param>
    /// <returns>Cache key</returns>
    string GetUserSymbolsCacheKey(string userId, string? assetClass = null);

    /// <summary>
    /// Generate cache key for symbol by ticker.
    /// </summary>
    /// <param name="ticker">Symbol ticker</param>
    /// <param name="market">Market code (optional)</param>
    /// <returns>Cache key</returns>
    string GetSymbolByTickerCacheKey(string ticker, string? market = null);
}
