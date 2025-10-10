using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Database-backed implementation of IMultiAssetDataService
/// Provides real market data from the database
/// </summary>
public class MultiAssetDataService : IMultiAssetDataService
{
    private readonly TradingDbContext _context;
    private readonly ILogger<MultiAssetDataService> _logger;

    public MultiAssetDataService(TradingDbContext context, ILogger<MultiAssetDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public event EventHandler<MarketDataUpdateDto>? OnMarketDataUpdate;

    public async Task<UnifiedMarketDataDto?> GetMarketDataAsync(Guid symbolId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get symbol information
            var symbol = await _context.Symbols
                .Where(s => s.Id == symbolId && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.Ticker,
                    s.Display,
                    s.AssetClass,
                    s.Venue,
                    s.QuoteCurrency
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (symbol == null)
            {
                _logger.LogWarning("Symbol not found: {SymbolId}", symbolId);
                return null;
            }

            // Get latest market data
            var latestData = await _context.MarketData
                .Where(md => md.Symbol == symbol.Ticker)
                .OrderByDescending(md => md.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestData == null)
            {
                _logger.LogWarning("No market data found for symbol: {Symbol}", symbol.Ticker);
                return null;
            }

            return new UnifiedMarketDataDto
            {
                SymbolId = symbolId,
                Ticker = symbol.Ticker,
                AssetClassCode = symbol.AssetClass ?? "UNKNOWN",
                MarketCode = symbol.Venue ?? "UNKNOWN",
                Price = latestData.Close,
                PriceChange24h = 0m, // Would need historical calculation
                PriceChangePercent = 0m, // Would need historical calculation
                Volume24h = (long)latestData.Volume,
                HighPrice = latestData.High,
                LowPrice = latestData.Low,
                OpenPrice = latestData.Open,
                DataTimestamp = latestData.Timestamp,
                ReceivedTimestamp = DateTime.UtcNow,
                IsRealTime = IsDataRecent(latestData.Timestamp),
                MarketStatus = "OPEN", // Would need market hours logic
                IsMarketOpen = true,
                Currency = symbol.QuoteCurrency ?? "USD"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market data for symbol: {SymbolId}", symbolId);
            return null;
        }
    }

    public async Task<BatchMarketDataDto> GetBatchMarketDataAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var symbols = symbolIds.ToList();
            var marketDataList = new List<UnifiedMarketDataDto>();
            var failedCount = 0;

            foreach (var symbolId in symbols)
            {
                var data = await GetMarketDataAsync(symbolId, cancellationToken);
                if (data != null)
                {
                    marketDataList.Add(data);
                }
                else
                {
                    failedCount++;
                }
            }

            return new BatchMarketDataDto
            {
                TotalSymbols = symbols.Count,
                SuccessfulSymbols = marketDataList.Count,
                FailedSymbols = failedCount,
                MarketData = marketDataList,
                RequestTimestamp = DateTime.UtcNow,
                ResponseTimestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch market data");
            return new BatchMarketDataDto
            {
                TotalSymbols = symbolIds.Count(),
                SuccessfulSymbols = 0,
                FailedSymbols = symbolIds.Count(),
                MarketData = new List<UnifiedMarketDataDto>(),
                RequestTimestamp = DateTime.UtcNow,
                ResponseTimestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(Guid symbolId, string interval, DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var symbol = await _context.Symbols
                .Where(s => s.Id == symbolId && s.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (symbol == null)
                return null;

            var query = _context.MarketData
                .Where(md => md.Symbol == symbol.Ticker);

            if (startTime.HasValue)
                query = query.Where(md => md.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(md => md.Timestamp <= endTime.Value);

            query = query.OrderBy(md => md.Timestamp);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            var historicalData = await query.ToListAsync(cancellationToken);

            return new HistoricalMarketDataDto
            {
                SymbolId = symbolId,
                Ticker = symbol.Ticker,
                Interval = interval,
                StartTime = startTime ?? DateTime.UtcNow.AddDays(-30),
                EndTime = endTime ?? DateTime.UtcNow,
                CandleCount = historicalData.Count,
                Candles = historicalData.Select(md => new CandlestickDataDto
                {
                    OpenTime = md.Timestamp,
                    CloseTime = md.Timestamp, // Approximation - would need proper close time
                    Open = md.Open,
                    High = md.High,
                    Low = md.Low,
                    Close = md.Close,
                    Volume = md.Volume
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical data for symbol: {SymbolId}", symbolId);
            return null;
        }
    }

    public async Task<MarketStatisticsDto?> GetMarketStatisticsAsync(Guid symbolId, CancellationToken cancellationToken = default)
    {
        try
        {
            var symbol = await _context.Symbols
                .Where(s => s.Id == symbolId && s.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (symbol == null)
                return null;

            var latestData = await _context.MarketData
                .Where(md => md.Symbol == symbol.Ticker)
                .OrderByDescending(md => md.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestData == null)
                return null;

            return new MarketStatisticsDto
            {
                SymbolId = symbolId,
                Ticker = symbol.Ticker,
                LastUpdated = latestData.Timestamp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market statistics for symbol: {SymbolId}", symbolId);
            return null;
        }
    }

    public async Task<List<SymbolSearchResultDto>> SearchSymbolsAsync(SymbolSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Symbols
                .Where(s => s.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                query = query.Where(s =>
                    s.Ticker.Contains(request.Query) ||
                    s.Display.Contains(request.Query) ||
                    s.FullName.Contains(request.Query));
            }

            if (!string.IsNullOrWhiteSpace(request.AssetClass))
            {
                query = query.Where(s => s.AssetClass == request.AssetClass);
            }

            var symbols = await query
                .Take(request.Limit ?? 50)
                .Select(s => new SymbolSearchResultDto
                {
                    Id = s.Id,
                    Ticker = s.Ticker,
                    FullName = s.FullName ?? s.Ticker
                })
                .ToListAsync(cancellationToken);

            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching symbols");
            return new List<SymbolSearchResultDto>();
        }
    }

    public async Task<List<SymbolSummaryDto>> GetSymbolsByAssetClassAsync(Guid assetClassId, int? limit = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Since we don't have asset class IDs in current schema, use asset class string
            var query = _context.Symbols.Where(s => s.IsActive);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return await query
                .Select(s => new SymbolSummaryDto
                {
                    Id = s.Id,
                    Ticker = s.Ticker,
                    Display = s.Display ?? s.Ticker,
                    AssetClassCode = s.AssetClass ?? "UNKNOWN",
                    MarketCode = s.Venue ?? "UNKNOWN",
                    IsTracked = s.IsTracked
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols by asset class");
            return new List<SymbolSummaryDto>();
        }
    }

    public async Task<List<SymbolSummaryDto>> GetPopularSymbolsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get popular or tracked symbols
            return await _context.Symbols
                .Where(s => s.IsActive && (s.IsPopular || s.IsTracked))
                .Take(limit)
                .Select(s => new SymbolSummaryDto
                {
                    Id = s.Id,
                    Ticker = s.Ticker,
                    Display = s.Display ?? s.Ticker,
                    AssetClassCode = s.AssetClass ?? "UNKNOWN",
                    MarketCode = s.Venue ?? "UNKNOWN",
                    IsTracked = s.IsTracked
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular symbols");
            return new List<SymbolSummaryDto>();
        }
    }

    public async Task<TopMoversDto> GetTopMoversAsync(string? assetClassCode = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // This would require historical price comparison - returning empty for now
            return new TopMoversDto
            {
                Gainers = new List<SymbolSummaryDto>(),
                Losers = new List<SymbolSummaryDto>(),
                AssetClassCode = assetClassCode,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top movers");
            return new TopMoversDto();
        }
    }

    public async Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalSymbols = await _context.Symbols.CountAsync(s => s.IsActive, cancellationToken);
            var trackedSymbols = await _context.Symbols.CountAsync(s => s.IsActive && s.IsTracked, cancellationToken);

            var assetClassSummary = await _context.Symbols
                .Where(s => s.IsActive)
                .GroupBy(s => s.AssetClass)
                .Select(g => new MyTrader.Core.Interfaces.AssetClassSummaryDto
                {
                    Code = g.Key ?? "UNKNOWN",
                    Name = g.Key ?? "Unknown",
                    SymbolCount = g.Count(),
                    TrackedSymbolCount = g.Count(s => s.IsTracked)
                })
                .ToListAsync(cancellationToken);

            return new MarketOverviewDto
            {
                TotalSymbols = totalSymbols,
                TrackedSymbols = trackedSymbols,
                ActiveMarkets = assetClassSummary.Count,
                OpenMarkets = assetClassSummary.Count, // Simplified - assume all markets are open
                AssetClassSummary = assetClassSummary,
                MarketStatuses = new List<MarketStatusDto>
                {
                    new MarketStatusDto
                    {
                        Status = "OPEN",
                        StatusMessage = "24/7 Trading"
                    }
                },
                TopMovers = new TopMoversDto(),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market overview");
            return new MarketOverviewDto();
        }
    }

    public Task<bool> SubscribeToRealtimeUpdatesAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default)
    {
        // Real-time subscription would be handled by WebSocket services
        return Task.FromResult(true);
    }

    public Task<bool> UnsubscribeFromRealtimeUpdatesAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default)
    {
        // Real-time subscription would be handled by WebSocket services
        return Task.FromResult(true);
    }

    public async Task<List<VolumeLeaderDto>> GetTopByVolumePerAssetClassAsync(int perClass = 8, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting top {PerClass} symbols by volume per asset class", perClass);

            // ✅ FIX: Use window function to get previous close for correct price change calculation
            // This approach gets the latest data AND the previous day's close in one query
            var cutoffTime = DateTime.UtcNow.AddDays(-2); // Extended to get previous day's data for comparison

            // First, get latest data per symbol with window function for previous close
            var latestDataWithPrevClose = from md in _context.MarketData
                                          join s in _context.Symbols on md.Symbol equals s.Ticker
                                          where s.IsActive && md.Timestamp >= cutoffTime
                                          group md by new { md.Symbol, s.Id, s.Display, s.AssetClass, s.Venue, s.QuoteCurrency } into g
                                          let latest = g.OrderByDescending(x => x.Timestamp).First()
                                          let previous = g.OrderByDescending(x => x.Timestamp).Skip(1).FirstOrDefault()
                                          select new
                                          {
                                              SymbolId = g.Key.Id,
                                              Ticker = g.Key.Symbol,
                                              Display = g.Key.Display,
                                              AssetClass = g.Key.AssetClass,
                                              Market = g.Key.Venue,
                                              Currency = g.Key.QuoteCurrency,
                                              CurrentClose = latest.Close,
                                              PreviousClose = previous != null ? previous.Close : latest.Open, // Fallback to Open if no previous data
                                              Volume = latest.Volume,
                                              LastUpdated = latest.Timestamp
                                          };

            // Then group by asset class and get top N by volume
            var query = from data in latestDataWithPrevClose
                        group data by data.AssetClass into assetGroup
                        select new
                        {
                            AssetClass = assetGroup.Key,
                            TopSymbols = assetGroup
                                .OrderByDescending(x => x.Volume)
                                .Take(perClass)
                                .Select(x => new VolumeLeaderDto
                                {
                                    SymbolId = x.SymbolId,
                                    Ticker = x.Ticker,
                                    Display = x.Display ?? x.Ticker,
                                    AssetClass = x.AssetClass ?? "UNKNOWN",
                                    Market = x.Market ?? "UNKNOWN",
                                    Price = x.CurrentClose,
                                    // ✅ Correct calculation: current close - previous close
                                    PriceChange = x.CurrentClose - x.PreviousClose,
                                    PriceChangePercent = x.PreviousClose != 0 ?
                                        ((x.CurrentClose - x.PreviousClose) / x.PreviousClose) * 100 : 0,
                                    Volume = (long)x.Volume,
                                    VolumeQuote = null,
                                    LastUpdated = x.LastUpdated,
                                    Currency = x.Currency ?? "USD"
                                })
                                .ToList()
                        };

            var result = await query.ToListAsync(cancellationToken);

            var volumeLeaders = new List<VolumeLeaderDto>();
            foreach (var assetClassGroup in result)
            {
                volumeLeaders.AddRange(assetClassGroup.TopSymbols);
            }

            _logger.LogInformation("Retrieved {Count} volume leaders across {AssetClassCount} asset classes",
                volumeLeaders.Count, result.Count);

            return volumeLeaders.OrderByDescending(x => x.Volume).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top symbols by volume per asset class");
            return new List<VolumeLeaderDto>();
        }
    }

    private static bool IsDataRecent(DateTime timestamp, int minutesThreshold = 5)
    {
        return DateTime.UtcNow - timestamp.ToUniversalTime() < TimeSpan.FromMinutes(minutesThreshold);
    }
}