using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Infrastructure.Data;
using System.Diagnostics;
using System.Text.Json;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// High-performance BIST market data service with intelligent caching
/// Optimized for sub-100ms response times and efficient memory usage
/// </summary>
public class BistMarketDataService : IBistMarketDataService
{
    private readonly TradingDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BistMarketDataService> _logger;
    private readonly BistConfiguration _config;

    // Cache keys
    private const string CACHE_KEY_BIST_OVERVIEW = "bist:overview";
    private const string CACHE_KEY_BIST_TOP_MOVERS = "bist:top_movers";
    private const string CACHE_KEY_BIST_SECTORS = "bist:sectors";
    private const string CACHE_KEY_BIST_ALL_STOCKS = "bist:all_stocks";
    private const string CACHE_KEY_BIST_STOCK_PREFIX = "bist:stock:";
    private const string CACHE_KEY_BIST_MARKET_STATUS = "bist:market_status";

    // Cache expiration times
    private static readonly TimeSpan CACHE_DURATION_OVERVIEW = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CACHE_DURATION_TOP_MOVERS = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CACHE_DURATION_SECTORS = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CACHE_DURATION_STOCK_DATA = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan CACHE_DURATION_MARKET_STATUS = TimeSpan.FromMinutes(1);

    // Performance monitoring
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private readonly object _metricsLock = new();

    public event EventHandler<BistMarketDataUpdateEventArgs>? OnBistDataUpdated;

    public BistMarketDataService(
        TradingDbContext context,
        IMemoryCache cache,
        ILogger<BistMarketDataService> logger,
        IOptions<BistConfiguration> config)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<List<MarketDataDto>> GetBistMarketDataAsync(
        List<string>? symbols = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var cacheKey = symbols != null
                ? $"{CACHE_KEY_BIST_ALL_STOCKS}:{string.Join(",", symbols)}:{limit}"
                : $"{CACHE_KEY_BIST_ALL_STOCKS}:all:{limit}";

            if (_cache.TryGetValue(cacheKey, out List<MarketDataDto>? cachedData))
            {
                IncrementCacheHits();
                _logger.LogDebug("BIST market data cache hit for key: {CacheKey}", cacheKey);
                return cachedData!;
            }

            IncrementCacheMisses();
            _logger.LogDebug("BIST market data cache miss, querying database");

            // Use optimized database function
            var result = await GetBistStocksFromDatabase(symbols, limit, cancellationToken);

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CACHE_DURATION_STOCK_DATA,
                Priority = CacheItemPriority.High,
                Size = result.Count * 512 // Estimate memory usage
            };
            _cache.Set(cacheKey, result, cacheOptions);

            _logger.LogInformation("Retrieved {Count} BIST stocks in {ElapsedMs}ms",
                result.Count, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BIST market data");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning("BIST market data query took {ElapsedMs}ms (target: <50ms)",
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public async Task<MarketDataDto?> GetBistStockDataAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var cacheKey = $"{CACHE_KEY_BIST_STOCK_PREFIX}{symbol.ToUpper()}";

            if (_cache.TryGetValue(cacheKey, out MarketDataDto? cachedStock))
            {
                IncrementCacheHits();
                _logger.LogDebug("BIST stock cache hit for symbol: {Symbol}", symbol);
                return cachedStock;
            }

            IncrementCacheMisses();

            // Use optimized database function for single stock
            var result = await GetSingleBistStockFromDatabase(symbol, cancellationToken);

            if (result != null)
            {
                // Cache individual stock data
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CACHE_DURATION_STOCK_DATA,
                    Priority = CacheItemPriority.High,
                    Size = 512
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            _logger.LogDebug("Retrieved BIST stock {Symbol} in {ElapsedMs}ms",
                symbol, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BIST stock data for {Symbol}", symbol);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 20)
            {
                _logger.LogWarning("BIST stock query for {Symbol} took {ElapsedMs}ms (target: <10ms)",
                    symbol, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public async Task<BistMarketOverviewDto> GetBistMarketOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (_cache.TryGetValue(CACHE_KEY_BIST_OVERVIEW, out BistMarketOverviewDto? cachedOverview))
            {
                IncrementCacheHits();
                _logger.LogDebug("BIST overview cache hit");
                return cachedOverview!;
            }

            IncrementCacheMisses();

            // Use optimized database function
            var overview = await GetBistOverviewFromDatabase(cancellationToken);

            // Cache the overview
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CACHE_DURATION_OVERVIEW,
                Priority = CacheItemPriority.High,
                Size = 1024
            };
            _cache.Set(CACHE_KEY_BIST_OVERVIEW, overview, cacheOptions);

            _logger.LogInformation("Retrieved BIST overview in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return overview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BIST market overview");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 150)
            {
                _logger.LogWarning("BIST overview query took {ElapsedMs}ms (target: <100ms)",
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public async Task<BistTopMoversDto> GetBistTopMoversAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (_cache.TryGetValue(CACHE_KEY_BIST_TOP_MOVERS, out BistTopMoversDto? cachedMovers))
            {
                IncrementCacheHits();
                _logger.LogDebug("BIST top movers cache hit");
                return cachedMovers!;
            }

            IncrementCacheMisses();

            var topMovers = await GetBistTopMoversFromDatabase(cancellationToken);

            // Cache the top movers
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CACHE_DURATION_TOP_MOVERS,
                Priority = CacheItemPriority.High,
                Size = 4096 // Larger data structure
            };
            _cache.Set(CACHE_KEY_BIST_TOP_MOVERS, topMovers, cacheOptions);

            _logger.LogInformation("Retrieved BIST top movers in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return topMovers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BIST top movers");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning("BIST top movers query took {ElapsedMs}ms (target: <75ms)",
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public async Task<List<BistSectorPerformanceDto>> GetBistSectorPerformanceAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (_cache.TryGetValue(CACHE_KEY_BIST_SECTORS, out List<BistSectorPerformanceDto>? cachedSectors))
            {
                IncrementCacheHits();
                return cachedSectors!;
            }

            IncrementCacheMisses();

            var sectors = await GetBistSectorsFromDatabase(cancellationToken);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CACHE_DURATION_SECTORS,
                Priority = CacheItemPriority.Normal,
                Size = sectors.Count * 256
            };
            _cache.Set(CACHE_KEY_BIST_SECTORS, sectors, cacheOptions);

            return sectors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BIST sector performance");
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<List<BistStockSearchResultDto>> SearchBistStocksAsync(
        string searchTerm,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Search is not cached due to variable nature
            var searchResults = await SearchBistStocksInDatabase(searchTerm, limit, cancellationToken);

            _logger.LogDebug("BIST search for '{SearchTerm}' returned {Count} results in {ElapsedMs}ms",
                searchTerm, searchResults.Count, stopwatch.ElapsedMilliseconds);

            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching BIST stocks for term: {SearchTerm}", searchTerm);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<BistHistoricalDataDto?> GetBistHistoricalDataAsync(
        string symbol,
        string period = "1m",
        string interval = "1d",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Historical data queries are complex and not cached for now
            var historicalData = await GetBistHistoricalDataFromDatabase(symbol, period, interval, cancellationToken);

            _logger.LogDebug("Retrieved BIST historical data for {Symbol} ({Period}/{Interval}) in {ElapsedMs}ms",
                symbol, period, interval, stopwatch.ElapsedMilliseconds);

            return historicalData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BIST historical data for {Symbol}", symbol);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<BistMarketStatusDto> GetBistMarketStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cache.TryGetValue(CACHE_KEY_BIST_MARKET_STATUS, out BistMarketStatusDto? cachedStatus))
            {
                IncrementCacheHits();
                return cachedStatus!;
            }

            IncrementCacheMisses();

            var marketStatus = await GetBistMarketStatusFromDatabase(cancellationToken);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CACHE_DURATION_MARKET_STATUS,
                Priority = CacheItemPriority.High,
                Size = 256
            };
            _cache.Set(CACHE_KEY_BIST_MARKET_STATUS, marketStatus, cacheOptions);

            return marketStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BIST market status");
            throw;
        }
    }

    public async Task<BistCacheRefreshResultDto> RefreshBistDataAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var refreshResult = new BistCacheRefreshResultDto
        {
            RefreshTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting BIST data cache refresh");

            // Clear all BIST-related cache entries
            ClearBistCache();

            // Pre-warm critical cache entries
            var tasks = new List<Task>
            {
                GetBistMarketOverviewAsync(cancellationToken),
                GetBistTopMoversAsync(cancellationToken),
                GetBistSectorPerformanceAsync(cancellationToken),
                GetBistMarketDataAsync(null, 50, cancellationToken)
            };

            await Task.WhenAll(tasks);

            refreshResult.Success = true;
            refreshResult.Message = "BIST cache refreshed successfully";
            refreshResult.SymbolsUpdated = await GetBistSymbolCount(cancellationToken);
            refreshResult.RefreshDuration = stopwatch.Elapsed;

            // Fire update event
            OnBistDataUpdated?.Invoke(this, new BistMarketDataUpdateEventArgs
            {
                UpdateTime = refreshResult.RefreshTime,
                UpdateType = "CACHE_REFRESH",
                AffectedSymbolsCount = refreshResult.SymbolsUpdated
            });

            _logger.LogInformation("BIST cache refresh completed in {ElapsedMs}ms, {SymbolCount} symbols updated",
                stopwatch.ElapsedMilliseconds, refreshResult.SymbolsUpdated);
        }
        catch (Exception ex)
        {
            refreshResult.Success = false;
            refreshResult.Message = $"Cache refresh failed: {ex.Message}";
            refreshResult.Errors.Add(ex.Message);

            _logger.LogError(ex, "BIST cache refresh failed");
        }
        finally
        {
            stopwatch.Stop();
            refreshResult.RefreshDuration = stopwatch.Elapsed;
        }

        return refreshResult;
    }

    public async Task<BistCacheHealthDto> GetCacheHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var health = new BistCacheHealthDto();

        try
        {
            // Check cache statistics
            lock (_metricsLock)
            {
                var totalRequests = _cacheHits + _cacheMisses;
                health.CacheHitRatio = totalRequests > 0 ? (double)_cacheHits / totalRequests : 0;
                health.TotalCacheHits = _cacheHits;
                health.TotalCacheMisses = _cacheMisses;
            }

            // Check data freshness
            if (_cache.TryGetValue(CACHE_KEY_BIST_OVERVIEW, out BistMarketOverviewDto? overview))
            {
                health.LastRefresh = overview!.LastUpdated;
                health.DataAge = DateTime.UtcNow - overview.LastUpdated;
            }
            else
            {
                health.DataAge = TimeSpan.MaxValue;
                health.Issues.Add("No cached overview data found");
            }

            // Estimate cached symbols count
            health.CachedSymbolsCount = await GetBistSymbolCount(cancellationToken);

            // Health assessment
            health.IsHealthy = health.CacheHitRatio > 0.7 && health.DataAge < TimeSpan.FromMinutes(5);

            if (health.CacheHitRatio < 0.5)
            {
                health.Issues.Add("Low cache hit ratio");
            }

            if (health.DataAge > TimeSpan.FromMinutes(2))
            {
                health.Issues.Add("Stale cache data");
            }

            // Performance metrics
            health.PerformanceMetrics["cache_hit_ratio"] = health.CacheHitRatio;
            health.PerformanceMetrics["data_age_minutes"] = health.DataAge.TotalMinutes;
            health.PerformanceMetrics["cached_symbols"] = health.CachedSymbolsCount;
        }
        catch (Exception ex)
        {
            health.IsHealthy = false;
            health.Issues.Add($"Health check failed: {ex.Message}");
            _logger.LogError(ex, "Error during BIST cache health check");
        }

        return health;
    }

    #region Private Database Query Methods

    private async Task<List<MarketDataDto>> GetBistStocksFromDatabase(
        List<string>? symbols,
        int limit,
        CancellationToken cancellationToken)
    {
        var symbolsParam = symbols?.ToArray();

        var result = await _context.Database
            .SqlQueryRaw<BistStockQueryResult>(@"
                SELECT * FROM get_bist_stocks_data(@p0, @p1)",
                symbolsParam, limit)
            .ToListAsync(cancellationToken);

        return result.Select(r => new MarketDataDto
        {
            Symbol = r.Symbol,
            Name = r.Name,
            Price = r.Price,
            Change = r.Change,
            ChangePercent = r.Change_Percent,
            Volume = (long)r.Volume,
            High24h = r.High24h,
            Low24h = r.Low24h,
            LastUpdated = r.Last_Updated,
            AssetClass = r.Asset_Class,
            Currency = r.Currency
        }).ToList();
    }

    private async Task<MarketDataDto?> GetSingleBistStockFromDatabase(
        string symbol,
        CancellationToken cancellationToken)
    {
        var result = await _context.Database
            .SqlQueryRaw<BistStockDetailQueryResult>(@"
                SELECT * FROM get_bist_stock_data(@p0)",
                symbol)
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null) return null;

        return new MarketDataDto
        {
            Symbol = result.Symbol,
            Name = result.Name,
            Price = result.Price,
            Change = result.Change,
            ChangePercent = result.Change_Percent,
            Volume = (long)result.Volume,
            High24h = result.High24h,
            Low24h = result.Low24h,
            LastUpdated = result.Last_Updated,
            Currency = result.Currency,
            AssetClass = "BIST"
        };
    }

    private async Task<BistMarketOverviewDto> GetBistOverviewFromDatabase(
        CancellationToken cancellationToken)
    {
        var result = await _context.Database
            .SqlQueryRaw<BistOverviewQueryResult>(@"
                SELECT * FROM get_bist_market_overview()")
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return new BistMarketOverviewDto
            {
                LastUpdated = DateTime.UtcNow,
                MarketStatus = "UNKNOWN"
            };
        }

        return new BistMarketOverviewDto
        {
            TotalStocks = result.Total_Stocks,
            TotalVolume = result.Total_Volume,
            TotalMarketCap = result.Total_Market_Cap,
            AvgChangePercent = result.Avg_Change_Percent,
            GainersCount = result.Gainers_Count,
            LosersCount = result.Losers_Count,
            UnchangedCount = result.Unchanged_Count,
            LastUpdated = result.Last_Updated,
            MarketStatus = await DetermineMarketStatus(cancellationToken)
        };
    }

    private async Task<BistTopMoversDto> GetBistTopMoversFromDatabase(
        CancellationToken cancellationToken)
    {
        var result = await _context.Database
            .SqlQueryRaw<BistTopMoversQueryResult>(@"
                SELECT * FROM get_bist_top_movers()")
            .FirstOrDefaultAsync(cancellationToken);

        if (result?.Gainers == null)
        {
            return new BistTopMoversDto { LastUpdated = DateTime.UtcNow };
        }

        var topMovers = new BistTopMoversDto
        {
            LastUpdated = DateTime.UtcNow
        };

        // Parse JSON results
        if (result.Gainers != null)
        {
            var gainersJson = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result.Gainers);
            topMovers.Gainers = ParseMoversJson(gainersJson);
        }

        if (result.Losers != null)
        {
            var losersJson = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result.Losers);
            topMovers.Losers = ParseMoversJson(losersJson);
        }

        if (result.Most_Active != null)
        {
            var activeJson = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result.Most_Active);
            topMovers.MostActive = ParseMoversJson(activeJson);
        }

        return topMovers;
    }

    private async Task<List<BistSectorPerformanceDto>> GetBistSectorsFromDatabase(
        CancellationToken cancellationToken)
    {
        var result = await _context.Database
            .SqlQueryRaw<BistSectorQueryResult>(@"
                SELECT * FROM get_bist_sector_performance()")
            .ToListAsync(cancellationToken);

        return result.Select(r => new BistSectorPerformanceDto
        {
            Sector = r.Sector,
            StockCount = r.Stock_Count,
            AvgChangePercent = r.Avg_Change_Percent,
            TotalVolume = r.Total_Volume,
            TotalMarketCap = r.Total_Market_Cap,
            Gainers = r.Gainers,
            Losers = r.Losers,
            PerformanceRank = r.Performance_Rank,
            LastUpdated = DateTime.UtcNow
        }).ToList();
    }

    private async Task<List<BistStockSearchResultDto>> SearchBistStocksInDatabase(
        string searchTerm,
        int limit,
        CancellationToken cancellationToken)
    {
        var result = await _context.Database
            .SqlQueryRaw<BistSearchQueryResult>(@"
                SELECT * FROM search_symbols(@p0, 'STOCK_BIST', @p1)",
                searchTerm, limit)
            .ToListAsync(cancellationToken);

        return result.Select(r => new BistStockSearchResultDto
        {
            Symbol = r.Ticker,
            Name = r.Display_Name,
            FullName = r.Display_Name,
            CurrentPrice = r.Current_Price,
            Volume24h = r.Volume_24h,
            SearchRank = r.Search_Rank
        }).ToList();
    }

    private async Task<BistHistoricalDataDto?> GetBistHistoricalDataFromDatabase(
        string symbol,
        string period,
        string interval,
        CancellationToken cancellationToken)
    {
        // Complex historical data query - placeholder implementation
        // This would need to be implemented based on your historical data requirements
        return null;
    }

    private async Task<BistMarketStatusDto> GetBistMarketStatusFromDatabase(
        CancellationToken cancellationToken)
    {
        var market = await _context.Markets
            .Where(m => m.Code == "BIST")
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var istanbulTime = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));

        var isOpen = istanbulTime.DayOfWeek >= DayOfWeek.Monday &&
                     istanbulTime.DayOfWeek <= DayOfWeek.Friday &&
                     istanbulTime.TimeOfDay >= TimeSpan.FromHours(9.5) &&
                     istanbulTime.TimeOfDay <= TimeSpan.FromHours(18);

        return new BistMarketStatusDto
        {
            IsOpen = isOpen,
            Status = isOpen ? "OPEN" : "CLOSED",
            Timezone = "Europe/Istanbul",
            LastUpdated = DateTime.UtcNow
        };
    }

    private async Task<string> DetermineMarketStatus(CancellationToken cancellationToken)
    {
        var status = await GetBistMarketStatusAsync(cancellationToken);
        return status.Status;
    }

    private async Task<int> GetBistSymbolCount(CancellationToken cancellationToken)
    {
        return await _context.Symbols
            .Where(s => s.IsActive && (s.AssetClass == "STOCK_BIST" || s.AssetClass == "BIST"))
            .CountAsync(cancellationToken);
    }

    #endregion

    #region Cache Management

    private void ClearBistCache()
    {
        var cacheKeys = new[]
        {
            CACHE_KEY_BIST_OVERVIEW,
            CACHE_KEY_BIST_TOP_MOVERS,
            CACHE_KEY_BIST_SECTORS,
            CACHE_KEY_BIST_ALL_STOCKS,
            CACHE_KEY_BIST_MARKET_STATUS
        };

        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }

        // Clear individual stock caches - this is expensive but necessary
        // In a production system, you might maintain a list of cached stock keys
        _logger.LogDebug("Cleared BIST cache entries");
    }

    private void IncrementCacheHits()
    {
        lock (_metricsLock)
        {
            _cacheHits++;
        }
    }

    private void IncrementCacheMisses()
    {
        lock (_metricsLock)
        {
            _cacheMisses++;
        }
    }

    private List<BistMoverDto> ParseMoversJson(List<Dictionary<string, object>>? jsonData)
    {
        if (jsonData == null) return new List<BistMoverDto>();

        return jsonData.Select(item => new BistMoverDto
        {
            Symbol = item.GetValueOrDefault("symbol")?.ToString() ?? "",
            Name = item.GetValueOrDefault("name")?.ToString() ?? "",
            Sector = item.GetValueOrDefault("sector")?.ToString(),
            Price = Convert.ToDecimal(item.GetValueOrDefault("price") ?? 0),
            Change = Convert.ToDecimal(item.GetValueOrDefault("change") ?? 0),
            ChangePercent = Convert.ToDecimal(item.GetValueOrDefault("changePercent") ?? 0),
            Volume = Convert.ToDecimal(item.GetValueOrDefault("volume") ?? 0),
            Currency = "TRY",
            LastUpdated = DateTime.UtcNow
        }).ToList();
    }

    #endregion

    #region Query Result Classes

    private class BistStockQueryResult
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Change { get; set; }
        public decimal Change_Percent { get; set; }
        public decimal Volume { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public DateTime Last_Updated { get; set; }
        public string Asset_Class { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }

    private class BistStockDetailQueryResult : BistStockQueryResult
    {
        public string Full_Name { get; set; } = string.Empty;
        public string? Sector { get; set; }
        public string? Industry { get; set; }
        public decimal Previous_Close { get; set; }
        public decimal Market_Cap { get; set; }
        public decimal Trading_Value { get; set; }
        public long Transaction_Count { get; set; }
    }

    private class BistOverviewQueryResult
    {
        public int Total_Stocks { get; set; }
        public decimal Total_Volume { get; set; }
        public decimal Total_Market_Cap { get; set; }
        public decimal Avg_Change_Percent { get; set; }
        public int Gainers_Count { get; set; }
        public int Losers_Count { get; set; }
        public int Unchanged_Count { get; set; }
        public DateTime Last_Updated { get; set; }
    }

    private class BistTopMoversQueryResult
    {
        public string? Gainers { get; set; }
        public string? Losers { get; set; }
        public string? Most_Active { get; set; }
    }

    private class BistSectorQueryResult
    {
        public string Sector { get; set; } = string.Empty;
        public int Stock_Count { get; set; }
        public decimal Avg_Change_Percent { get; set; }
        public decimal Total_Volume { get; set; }
        public decimal Total_Market_Cap { get; set; }
        public int Gainers { get; set; }
        public int Losers { get; set; }
        public int Performance_Rank { get; set; }
    }

    private class BistSearchQueryResult
    {
        public string Ticker { get; set; } = string.Empty;
        public string Display_Name { get; set; } = string.Empty;
        public decimal? Current_Price { get; set; }
        public decimal? Volume_24h { get; set; }
        public float Search_Rank { get; set; }
    }

    #endregion
}

/// <summary>
/// BIST service configuration
/// </summary>
public class BistConfiguration
{
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirySeconds { get; set; } = 30;
    public int MaxConcurrentQueries { get; set; } = 10;
    public bool EnablePerformanceLogging { get; set; } = true;
    public List<string> DefaultSymbols { get; set; } = new();
}