using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using MyTrader.Core.Services;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Implementation of symbol management service with database access and caching.
/// Provides optimized queries for symbol retrieval and broadcast control.
/// </summary>
public class SymbolManagementService : ISymbolManagementService
{
    private readonly ITradingDbContext _dbContext;
    private readonly ISymbolCacheService _cacheService;
    private readonly ILogger<SymbolManagementService> _logger;

    // Fallback symbols in case database query fails or returns empty results
    private static readonly string[] FALLBACK_SYMBOLS = { "BTCUSDT", "ETHUSDT" };

    public SymbolManagementService(
        ITradingDbContext dbContext,
        ISymbolCacheService cacheService,
        ILogger<SymbolManagementService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<Symbol>> GetActiveSymbolsForBroadcastAsync(string assetClass, string market)
    {
        try
        {
            // Check cache first
            var cacheKey = _cacheService.GetBroadcastCacheKey(assetClass, market);
            var cached = _cacheService.GetCachedSymbols(cacheKey);
            if (cached != null && cached.Any())
            {
                return cached;
            }

            _logger.LogDebug("Querying database for broadcast symbols: AssetClass={AssetClass}, Market={Market}",
                assetClass, market);

            // Query database with optimized filtering and joins
            var query = _dbContext.Symbols
                .Include(s => s.AssetClassEntity)
                .Include(s => s.Market)
                .Where(s => s.IsActive && s.IsTracked);

            // Filter by asset class (legacy string field OR new relationship)
            if (!string.IsNullOrWhiteSpace(assetClass))
            {
                query = query.Where(s =>
                    s.AssetClass == assetClass ||
                    (s.AssetClassEntity != null && s.AssetClassEntity.Code == assetClass));
            }

            // Filter by market (legacy venue field OR new relationship)
            if (!string.IsNullOrWhiteSpace(market))
            {
                query = query.Where(s =>
                    s.Venue == market ||
                    (s.Market != null && s.Market.Code == market));
            }

            // Order by broadcast priority (highest first)
            var symbols = await query
                .OrderByDescending(s => s.DisplayOrder) // Use DisplayOrder as proxy for broadcast_priority
                .ThenBy(s => s.Ticker)
                .ToListAsync();

            if (symbols.Any())
            {
                // Cache the results
                _cacheService.SetCachedSymbols(cacheKey, symbols, expirationMinutes: 5);

                _logger.LogInformation("Loaded {Count} broadcast symbols for {AssetClass}/{Market}",
                    symbols.Count, assetClass, market);

                return symbols;
            }

            // No symbols found - log warning and return fallback
            _logger.LogWarning("No active symbols found for broadcast: AssetClass={AssetClass}, Market={Market}. Using fallback.",
                assetClass, market);

            return await GetFallbackSymbolsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading broadcast symbols for {AssetClass}/{Market}. Using fallback.",
                assetClass, market);

            return await GetFallbackSymbolsAsync();
        }
    }

    public async Task<List<Symbol>> GetDefaultSymbolsAsync(string? assetClass = null)
    {
        try
        {
            // Check cache first
            var cacheKey = _cacheService.GetDefaultsCacheKey(assetClass);
            var cached = _cacheService.GetCachedSymbols(cacheKey);
            if (cached != null && cached.Any())
            {
                return cached;
            }

            _logger.LogDebug("Querying database for default symbols: AssetClass={AssetClass}", assetClass ?? "ALL");

            // Query for default symbols (is_default_symbol=true)
            var query = _dbContext.Symbols
                .Include(s => s.AssetClassEntity)
                .Include(s => s.Market)
                .Where(s => s.IsActive && s.IsPopular); // Use IsPopular as proxy for is_default_symbol

            if (!string.IsNullOrWhiteSpace(assetClass))
            {
                query = query.Where(s =>
                    s.AssetClass == assetClass ||
                    (s.AssetClassEntity != null && s.AssetClassEntity.Code == assetClass));
            }

            var symbols = await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Ticker)
                .ToListAsync();

            if (symbols.Any())
            {
                _cacheService.SetCachedSymbols(cacheKey, symbols, expirationMinutes: 10);

                _logger.LogInformation("Loaded {Count} default symbols for {AssetClass}",
                    symbols.Count, assetClass ?? "ALL");

                return symbols;
            }

            _logger.LogWarning("No default symbols found for AssetClass={AssetClass}. Using fallback.", assetClass ?? "ALL");
            return await GetFallbackSymbolsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading default symbols for {AssetClass}. Using fallback.", assetClass ?? "ALL");
            return await GetFallbackSymbolsAsync();
        }
    }

    public async Task<List<Symbol>> GetUserSymbolsAsync(string userId, string? assetClass = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Invalid userId provided for GetUserSymbolsAsync");
                return await GetDefaultSymbolsAsync(assetClass);
            }

            // Check cache first
            var cacheKey = _cacheService.GetUserSymbolsCacheKey(userId, assetClass);
            var cached = _cacheService.GetCachedSymbols(cacheKey);
            if (cached != null && cached.Any())
            {
                return cached;
            }

            _logger.LogDebug("Querying database for user symbols: UserId={UserId}, AssetClass={AssetClass}",
                userId, assetClass ?? "ALL");

            // Parse userId to GUID
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid GUID format for userId: {UserId}", userId);
                return await GetDefaultSymbolsAsync(assetClass);
            }

            // Query user preferences joined with symbols
            var query = from pref in _dbContext.UserDashboardPreferences
                        join symbol in _dbContext.Symbols on pref.SymbolId equals symbol.Id
                        where pref.UserId == userGuid && pref.IsVisible && symbol.IsActive
                        select symbol;

            if (!string.IsNullOrWhiteSpace(assetClass))
            {
                query = query.Where(s =>
                    s.AssetClass == assetClass ||
                    (s.AssetClassEntity != null && s.AssetClassEntity.Code == assetClass));
            }

            var symbols = await query
                .Include(s => s.AssetClassEntity)
                .Include(s => s.Market)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Ticker)
                .ToListAsync();

            if (symbols.Any())
            {
                _cacheService.SetCachedSymbols(cacheKey, symbols, expirationMinutes: 5);

                _logger.LogInformation("Loaded {Count} user symbols for UserId={UserId}, AssetClass={AssetClass}",
                    symbols.Count, userId, assetClass ?? "ALL");

                return symbols;
            }

            // No user preferences found - return defaults
            _logger.LogInformation("No user preferences found for UserId={UserId}. Returning defaults.", userId);
            return await GetDefaultSymbolsAsync(assetClass);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user symbols for UserId={UserId}. Using defaults.", userId);
            return await GetDefaultSymbolsAsync(assetClass);
        }
    }

    public async Task UpdateSymbolPreferencesAsync(string userId, List<string> symbolIds)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Invalid userId provided for UpdateSymbolPreferencesAsync");
                return;
            }

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid GUID format for userId: {UserId}", userId);
                return;
            }

            _logger.LogInformation("Updating symbol preferences for UserId={UserId}, Count={Count}",
                userId, symbolIds?.Count ?? 0);

            // Remove existing preferences
            var existingPrefs = await _dbContext.UserDashboardPreferences
                .Where(p => p.UserId == userGuid)
                .ToListAsync();

            if (existingPrefs.Any())
            {
                _dbContext.UserDashboardPreferences.RemoveRange(existingPrefs);
            }

            // Add new preferences
            if (symbolIds != null && symbolIds.Any())
            {
                var displayOrder = 1;
                foreach (var symbolIdStr in symbolIds)
                {
                    if (Guid.TryParse(symbolIdStr, out var symbolId))
                    {
                        var preference = new UserDashboardPreferences
                        {
                            Id = Guid.NewGuid(),
                            UserId = userGuid,
                            SymbolId = symbolId,
                            IsVisible = true,
                            DisplayOrder = displayOrder++,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _dbContext.UserDashboardPreferences.Add(preference);
                    }
                }
            }

            await _dbContext.SaveChangesAsync();

            // Clear user's cache
            var cacheKey = _cacheService.GetUserSymbolsCacheKey(userId, null);
            _cacheService.ClearCache(cacheKey);

            _logger.LogInformation("Successfully updated symbol preferences for UserId={UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating symbol preferences for UserId={UserId}", userId);
            throw;
        }
    }

    public async Task ReloadSymbolsAsync()
    {
        try
        {
            _logger.LogInformation("Reloading symbols - clearing all caches");

            // Clear all caches to force fresh database queries
            _cacheService.ClearAllCaches();

            // Pre-warm cache with most common queries
            await GetDefaultSymbolsAsync("CRYPTO");
            await GetActiveSymbolsForBroadcastAsync("CRYPTO", "BINANCE");

            _logger.LogInformation("Symbol reload completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during symbol reload");
            throw;
        }
    }

    public async Task<List<Symbol>> GetSymbolsByAssetClassAsync(string assetClass, bool includeInactive = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(assetClass))
            {
                _logger.LogWarning("Empty asset class provided to GetSymbolsByAssetClassAsync");
                return new List<Symbol>();
            }

            _logger.LogDebug("Querying symbols by asset class: {AssetClass}, IncludeInactive={IncludeInactive}",
                assetClass, includeInactive);

            var query = _dbContext.Symbols
                .Include(s => s.AssetClassEntity)
                .Include(s => s.Market)
                .Where(s =>
                    s.AssetClass == assetClass ||
                    (s.AssetClassEntity != null && s.AssetClassEntity.Code == assetClass));

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            var symbols = await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Ticker)
                .ToListAsync();

            _logger.LogInformation("Found {Count} symbols for asset class {AssetClass}",
                symbols.Count, assetClass);

            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading symbols by asset class: {AssetClass}", assetClass);
            return new List<Symbol>();
        }
    }

    public async Task<Symbol?> GetSymbolByTickerAsync(string ticker, string? market = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return null;
            }

            // Check cache first
            var cacheKey = _cacheService.GetSymbolByTickerCacheKey(ticker, market);
            var cached = _cacheService.GetCachedSymbol(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            _logger.LogDebug("Querying symbol by ticker: {Ticker}, Market={Market}", ticker, market ?? "ANY");

            var query = _dbContext.Symbols
                .Include(s => s.AssetClassEntity)
                .Include(s => s.Market)
                .Where(s => s.Ticker.ToUpper() == ticker.ToUpper());

            if (!string.IsNullOrWhiteSpace(market))
            {
                query = query.Where(s =>
                    s.Venue == market ||
                    (s.Market != null && s.Market.Code == market));
            }

            var symbol = await query.FirstOrDefaultAsync();

            if (symbol != null)
            {
                _cacheService.SetCachedSymbol(cacheKey, symbol, expirationMinutes: 10);
            }

            return symbol;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading symbol by ticker: {Ticker}", ticker);
            return null;
        }
    }

    public async Task UpdateLastBroadcastTimeAsync(Guid symbolId, DateTime broadcastTime)
    {
        try
        {
            // This is a lightweight update operation - no caching needed
            var symbol = await _dbContext.Symbols.FindAsync(symbolId);
            if (symbol != null)
            {
                symbol.PriceUpdatedAt = broadcastTime; // Use PriceUpdatedAt as proxy for last_broadcast_at
                symbol.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("Updated last broadcast time for symbol {SymbolId} at {BroadcastTime}",
                    symbolId, broadcastTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating last broadcast time for symbol {SymbolId}", symbolId);
            // Don't throw - this is a non-critical operation
        }
    }

    public async Task<List<Symbol>> GetSymbolsDueBroadcastAsync(string assetClass, string market, int minIntervalSeconds = 1)
    {
        try
        {
            _logger.LogDebug("Querying symbols due for broadcast: AssetClass={AssetClass}, Market={Market}, MinInterval={Interval}s",
                assetClass, market, minIntervalSeconds);

            var cutoffTime = DateTime.UtcNow.AddSeconds(-minIntervalSeconds);

            var query = _dbContext.Symbols
                .Include(s => s.AssetClassEntity)
                .Include(s => s.Market)
                .Where(s => s.IsActive && s.IsTracked);

            // Filter by asset class
            if (!string.IsNullOrWhiteSpace(assetClass))
            {
                query = query.Where(s =>
                    s.AssetClass == assetClass ||
                    (s.AssetClassEntity != null && s.AssetClassEntity.Code == assetClass));
            }

            // Filter by market
            if (!string.IsNullOrWhiteSpace(market))
            {
                query = query.Where(s =>
                    s.Venue == market ||
                    (s.Market != null && s.Market.Code == market));
            }

            // Filter by last broadcast time (use PriceUpdatedAt as proxy)
            query = query.Where(s =>
                s.PriceUpdatedAt == null ||
                s.PriceUpdatedAt < cutoffTime);

            var symbols = await query
                .OrderByDescending(s => s.DisplayOrder)
                .ThenBy(s => s.Ticker)
                .ToListAsync();

            _logger.LogDebug("Found {Count} symbols due for broadcast", symbols.Count);

            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying symbols due for broadcast");
            return new List<Symbol>();
        }
    }

    /// <summary>
    /// Get fallback symbols when database query fails or returns no results.
    /// Uses minimal crypto symbols (BTC, ETH) to ensure service continues functioning.
    /// </summary>
    private async Task<List<Symbol>> GetFallbackSymbolsAsync()
    {
        try
        {
            var fallbackList = new List<Symbol>();

            foreach (var ticker in FALLBACK_SYMBOLS)
            {
                var symbol = await _dbContext.Symbols
                    .FirstOrDefaultAsync(s => s.Ticker == ticker);

                if (symbol != null)
                {
                    fallbackList.Add(symbol);
                }
                else
                {
                    // Create minimal symbol object if not found in database
                    fallbackList.Add(new Symbol
                    {
                        Id = Guid.NewGuid(),
                        Ticker = ticker,
                        Display = ticker.Replace("USDT", ""),
                        AssetClass = "CRYPTO",
                        Venue = "BINANCE",
                        IsActive = true,
                        IsTracked = true,
                        BaseCurrency = ticker.Replace("USDT", ""),
                        QuoteCurrency = "USDT"
                    });
                }
            }

            _logger.LogWarning("Using {Count} fallback symbols: {Symbols}",
                fallbackList.Count, string.Join(", ", fallbackList.Select(s => s.Ticker)));

            return fallbackList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fallback symbols - returning empty list");
            return new List<Symbol>();
        }
    }
}
