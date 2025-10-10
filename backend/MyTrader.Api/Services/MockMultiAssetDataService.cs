using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;

namespace MyTrader.Api.Services;

/// <summary>
/// Mock implementation of IMultiAssetDataService for development
/// Provides sample data to prevent HTTP 409 errors
/// </summary>
public class MockMultiAssetDataService : IMultiAssetDataService
{
    private readonly ILogger<MockMultiAssetDataService> _logger;
    private readonly Random _random = new();

    public MockMultiAssetDataService(ILogger<MockMultiAssetDataService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<MarketDataUpdateDto>? OnMarketDataUpdate;

    public Task<UnifiedMarketDataDto?> GetMarketDataAsync(Guid symbolId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetMarketDataAsync called for symbol: {SymbolId}", symbolId);

        var data = new UnifiedMarketDataDto
        {
            SymbolId = symbolId,
            Ticker = "MOCK",
            AssetClassCode = "CRYPTO",
            MarketCode = "MOCK_MARKET",
            Price = 100.0m + ((decimal)_random.NextSingle() * 100m),
            PriceChange24h = ((decimal)_random.NextSingle() - 0.5m) * 10m,
            PriceChangePercent = ((decimal)_random.NextSingle() - 0.5m) * 5m,
            Volume24h = _random.Next(1000000, 10000000),
            HighPrice = 110.0m,
            LowPrice = 90.0m,
            DataTimestamp = DateTime.UtcNow,
            ReceivedTimestamp = DateTime.UtcNow,
            IsRealTime = true,
            MarketStatus = "OPEN",
            IsMarketOpen = true,
            Currency = "USD"
        };

        return Task.FromResult<UnifiedMarketDataDto?>(data);
    }

    public Task<BatchMarketDataDto> GetBatchMarketDataAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetBatchMarketDataAsync called for {Count} symbols", symbolIds.Count());

        var symbols = symbolIds.ToList();
        var batch = new BatchMarketDataDto
        {
            TotalSymbols = symbols.Count,
            SuccessfulSymbols = symbols.Count,
            FailedSymbols = 0,
            MarketData = symbols.Select(id => new UnifiedMarketDataDto
            {
                SymbolId = id,
                Ticker = "MOCK",
                AssetClassCode = "CRYPTO",
                MarketCode = "MOCK_MARKET",
                Price = 100.0m + ((decimal)_random.NextSingle() * 100m),
                PriceChange24h = ((decimal)_random.NextSingle() - 0.5m) * 10m,
                PriceChangePercent = ((decimal)_random.NextSingle() - 0.5m) * 5m,
                Volume24h = _random.Next(1000000, 10000000),
                HighPrice = 110.0m,
                LowPrice = 90.0m,
                DataTimestamp = DateTime.UtcNow,
                ReceivedTimestamp = DateTime.UtcNow,
                IsRealTime = true,
                MarketStatus = "OPEN",
                IsMarketOpen = true,
                Currency = "USD"
            }).ToList(),
            RequestTimestamp = DateTime.UtcNow,
            ResponseTimestamp = DateTime.UtcNow
        };

        return Task.FromResult(batch);
    }

    public Task<HistoricalMarketDataDto?> GetHistoricalDataAsync(Guid symbolId, string interval, DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetHistoricalDataAsync called for symbol: {SymbolId}, interval: {Interval}", symbolId, interval);

        var data = new HistoricalMarketDataDto
        {
            SymbolId = symbolId,
            Ticker = "MOCK",
            Interval = interval,
            StartTime = startTime ?? DateTime.UtcNow.AddDays(-30),
            EndTime = endTime ?? DateTime.UtcNow,
            CandleCount = limit ?? 100,
            Candles = new List<CandlestickDataDto>()
        };

        return Task.FromResult<HistoricalMarketDataDto?>(data);
    }

    public Task<MarketStatisticsDto?> GetMarketStatisticsAsync(Guid symbolId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetMarketStatisticsAsync called for symbol: {SymbolId}", symbolId);

        var stats = new MarketStatisticsDto
        {
            SymbolId = symbolId,
            Ticker = "MOCK",
            CirculatingSupply = _random.Next(1000000, 100000000),
            TotalSupply = _random.Next(1000000, 100000000),
            LastUpdated = DateTime.UtcNow
        };

        return Task.FromResult<MarketStatisticsDto?>(stats);
    }

    public Task<List<SymbolSearchResultDto>> SearchSymbolsAsync(SymbolSearchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock SearchSymbolsAsync called with query: {Query}", request.Query);

        var results = new List<SymbolSearchResultDto>
        {
            new SymbolSearchResultDto
            {
                Id = Guid.NewGuid(),
                Ticker = "BTC",
                DisplayName = "Bitcoin",
                AssetClassCode = "CRYPTO",
                AssetClassName = "Cryptocurrency",
                MarketCode = "CRYPTO_MARKET",
                MarketName = "Crypto Market",
                IsActive = true,
                IsTracked = true,
                MatchScore = 100
            }
        };

        return Task.FromResult(results);
    }

    public Task<List<SymbolSummaryDto>> GetSymbolsByAssetClassAsync(Guid assetClassId, int? limit = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetSymbolsByAssetClassAsync called for asset class: {AssetClassId}", assetClassId);

        var symbols = new List<SymbolSummaryDto>
        {
            new SymbolSummaryDto
            {
                Id = Guid.NewGuid(),
                Ticker = "BTC",
                Display = "Bitcoin",
                FullName = "Bitcoin",
                AssetClassCode = "CRYPTO",
                MarketCode = "CRYPTO_MARKET",
                CurrentPrice = 43250.0m,
                PriceChange24h = 1250.0m,
                IsTracked = true,
                IsActive = true
            }
        };

        return Task.FromResult(symbols.Take(limit ?? 50).ToList());
    }

    public Task<List<SymbolSummaryDto>> GetPopularSymbolsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetPopularSymbolsAsync called with limit: {Limit}", limit);

        var symbols = new List<SymbolSummaryDto>
        {
            new SymbolSummaryDto
            {
                Id = Guid.NewGuid(),
                Ticker = "BTC",
                Display = "Bitcoin",
                FullName = "Bitcoin",
                AssetClassCode = "CRYPTO",
                MarketCode = "CRYPTO_MARKET",
                CurrentPrice = 43250.0m,
                PriceChange24h = 1250.0m,
                IsTracked = true,
                IsActive = true,
                IsPopular = true
            }
        };

        return Task.FromResult(symbols.Take(limit).ToList());
    }

    public Task<TopMoversDto> GetTopMoversAsync(string? assetClassCode = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetTopMoversAsync called for asset class: {AssetClass}, limit: {Limit}", assetClassCode ?? "all", limit);

        var topMovers = new TopMoversDto
        {
            AssetClassCode = assetClassCode,
            Gainers = new List<SymbolSummaryDto>
            {
                new SymbolSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Ticker = "ETH",
                    Display = "Ethereum",
                    FullName = "Ethereum",
                    AssetClassCode = "CRYPTO",
                    MarketCode = "CRYPTO_MARKET",
                    CurrentPrice = 2680.0m,
                    PriceChange24h = 180.0m,
                    IsTracked = true,
                    IsActive = true,
                    IsPopular = true
                }
            },
            Losers = new List<SymbolSummaryDto>
            {
                new SymbolSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Ticker = "ADA",
                    Display = "Cardano",
                    FullName = "Cardano",
                    AssetClassCode = "CRYPTO",
                    MarketCode = "CRYPTO_MARKET",
                    CurrentPrice = 0.48m,
                    PriceChange24h = -0.05m,
                    IsTracked = false,
                    IsActive = true
                }
            },
            Timestamp = DateTime.UtcNow
        };

        return Task.FromResult(topMovers);
    }

    public Task<MarketOverviewDto> GetMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetMarketOverviewAsync called");

        var overview = new MarketOverviewDto
        {
            TotalSymbols = 150,
            TrackedSymbols = 25,
            ActiveMarkets = 3,
            OpenMarkets = 2,
            AssetClassSummary = new List<MyTrader.Core.Interfaces.AssetClassSummaryDto>
            {
                new MyTrader.Core.Interfaces.AssetClassSummaryDto
                {
                    Code = "CRYPTO",
                    Name = "Cryptocurrency",
                    SymbolCount = 100,
                    TrackedSymbolCount = 60,
                    TotalMarketCap = 1500000000000m,
                    AvgPriceChange24h = 2.5m
                }
            },
            MarketStatuses = new List<MarketStatusDto>
            {
                new MarketStatusDto
                {
                    Status = "OPEN",
                    StatusMessage = "24/7 Trading"
                }
            },
            Timestamp = DateTime.UtcNow
        };

        return Task.FromResult(overview);
    }

    public Task<bool> SubscribeToRealtimeUpdatesAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock SubscribeToRealtimeUpdatesAsync called for {Count} symbols", symbolIds.Count());
        return Task.FromResult(true);
    }

    public Task<bool> UnsubscribeFromRealtimeUpdatesAsync(IEnumerable<Guid> symbolIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock UnsubscribeFromRealtimeUpdatesAsync called for {Count} symbols", symbolIds.Count());
        return Task.FromResult(true);
    }

    public Task<List<VolumeLeaderDto>> GetTopByVolumePerAssetClassAsync(int perClass = 8, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetTopByVolumePerAssetClassAsync called with perClass: {PerClass}", perClass);

        var volumeLeaders = new List<VolumeLeaderDto>
        {
            new VolumeLeaderDto
            {
                SymbolId = Guid.NewGuid(),
                Ticker = "BTC",
                Display = "Bitcoin",
                AssetClass = "CRYPTO",
                Market = "BINANCE",
                Price = 43250.0m,
                PriceChange = 1250.0m,
                PriceChangePercent = 2.98m,
                Volume = 28500000000L,
                VolumeQuote = 1230000000000m,
                LastUpdated = DateTime.UtcNow,
                Currency = "USD"
            },
            new VolumeLeaderDto
            {
                SymbolId = Guid.NewGuid(),
                Ticker = "ETH",
                Display = "Ethereum",
                AssetClass = "CRYPTO",
                Market = "BINANCE",
                Price = 2680.0m,
                PriceChange = 180.0m,
                PriceChangePercent = 7.20m,
                Volume = 15200000000L,
                VolumeQuote = 40700000000m,
                LastUpdated = DateTime.UtcNow,
                Currency = "USD"
            },
            new VolumeLeaderDto
            {
                SymbolId = Guid.NewGuid(),
                Ticker = "AAPL",
                Display = "Apple Inc.",
                AssetClass = "STOCK",
                Market = "NASDAQ",
                Price = 182.50m,
                PriceChange = -2.30m,
                PriceChangePercent = -1.24m,
                Volume = 52000000L,
                VolumeQuote = 9490000000m,
                LastUpdated = DateTime.UtcNow,
                Currency = "USD"
            },
            new VolumeLeaderDto
            {
                SymbolId = Guid.NewGuid(),
                Ticker = "MSFT",
                Display = "Microsoft Corporation",
                AssetClass = "STOCK",
                Market = "NASDAQ",
                Price = 415.30m,
                PriceChange = 8.75m,
                PriceChangePercent = 2.15m,
                Volume = 28000000L,
                VolumeQuote = 11628400000m,
                LastUpdated = DateTime.UtcNow,
                Currency = "USD"
            }
        };

        return Task.FromResult(volumeLeaders.Take(perClass * 3).ToList()); // Return up to 3 asset classes worth
    }
}