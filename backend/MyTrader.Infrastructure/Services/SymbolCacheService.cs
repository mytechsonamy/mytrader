using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.Services;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Thread-safe implementation of symbol caching using IMemoryCache.
/// Provides high-performance caching for frequently accessed symbol queries.
/// </summary>
public class SymbolCacheService : ISymbolCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SymbolCacheService> _logger;
    private readonly HashSet<string> _cacheKeys;
    private readonly object _lockObject = new object();

    private const string CACHE_KEY_PREFIX = "symbols:";
    private const int DEFAULT_EXPIRATION_MINUTES = 5;

    public SymbolCacheService(
        IMemoryCache cache,
        ILogger<SymbolCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheKeys = new HashSet<string>();
    }

    public List<Symbol>? GetCachedSymbols(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return null;

        var fullKey = CACHE_KEY_PREFIX + cacheKey;
        if (_cache.TryGetValue(fullKey, out List<Symbol>? symbols))
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}, Count: {Count}", cacheKey, symbols?.Count ?? 0);
            return symbols;
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);
        return null;
    }

    public Symbol? GetCachedSymbol(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return null;

        var fullKey = CACHE_KEY_PREFIX + cacheKey;
        if (_cache.TryGetValue(fullKey, out Symbol? symbol))
        {
            _logger.LogDebug("Cache HIT for single symbol key: {CacheKey}", cacheKey);
            return symbol;
        }

        _logger.LogDebug("Cache MISS for single symbol key: {CacheKey}", cacheKey);
        return null;
    }

    public void SetCachedSymbols(string cacheKey, List<Symbol> symbols, int expirationMinutes = DEFAULT_EXPIRATION_MINUTES)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return;

        if (symbols == null)
        {
            _logger.LogWarning("Attempted to cache null symbols for key: {CacheKey}", cacheKey);
            return;
        }

        var fullKey = CACHE_KEY_PREFIX + cacheKey;
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
            Priority = CacheItemPriority.High
        };

        _cache.Set(fullKey, symbols, cacheOptions);

        lock (_lockObject)
        {
            _cacheKeys.Add(fullKey);
        }

        _logger.LogDebug("Cached {Count} symbols with key: {CacheKey}, Expiration: {Minutes}min",
            symbols.Count, cacheKey, expirationMinutes);
    }

    public void SetCachedSymbol(string cacheKey, Symbol symbol, int expirationMinutes = DEFAULT_EXPIRATION_MINUTES)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return;

        if (symbol == null)
        {
            _logger.LogWarning("Attempted to cache null symbol for key: {CacheKey}", cacheKey);
            return;
        }

        var fullKey = CACHE_KEY_PREFIX + cacheKey;
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(fullKey, symbol, cacheOptions);

        lock (_lockObject)
        {
            _cacheKeys.Add(fullKey);
        }

        _logger.LogDebug("Cached single symbol with key: {CacheKey}, Ticker: {Ticker}, Expiration: {Minutes}min",
            cacheKey, symbol.Ticker, expirationMinutes);
    }

    public void ClearAllCaches()
    {
        lock (_lockObject)
        {
            foreach (var key in _cacheKeys.ToList())
            {
                _cache.Remove(key);
            }

            var count = _cacheKeys.Count;
            _cacheKeys.Clear();

            _logger.LogInformation("Cleared all symbol caches ({Count} entries)", count);
        }
    }

    public void ClearCache(string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return;

        var fullKey = CACHE_KEY_PREFIX + cacheKey;

        lock (_lockObject)
        {
            _cache.Remove(fullKey);
            _cacheKeys.Remove(fullKey);
        }

        _logger.LogDebug("Cleared cache for key: {CacheKey}", cacheKey);
    }

    public string GetBroadcastCacheKey(string assetClass, string market)
    {
        return $"broadcast:{assetClass?.ToUpperInvariant() ?? "ALL"}:{market?.ToUpperInvariant() ?? "ALL"}";
    }

    public string GetDefaultsCacheKey(string? assetClass = null)
    {
        return $"defaults:{assetClass?.ToUpperInvariant() ?? "ALL"}";
    }

    public string GetUserSymbolsCacheKey(string userId, string? assetClass = null)
    {
        return $"user:{userId}:{assetClass?.ToUpperInvariant() ?? "ALL"}";
    }

    public string GetSymbolByTickerCacheKey(string ticker, string? market = null)
    {
        return $"ticker:{ticker?.ToUpperInvariant()}:{market?.ToUpperInvariant() ?? "ANY"}";
    }
}
