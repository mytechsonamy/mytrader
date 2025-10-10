using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;

namespace MyTrader.Api.Services;

/// <summary>
/// Mock implementation of IEnhancedSymbolService for development
/// Provides sample data to prevent HTTP 409 errors
/// </summary>
public class MockEnhancedSymbolService : IEnhancedSymbolService
{
    private readonly ILogger<MockEnhancedSymbolService> _logger;

    public MockEnhancedSymbolService(ILogger<MockEnhancedSymbolService> logger)
    {
        _logger = logger;
    }

    public Task<EnhancedSymbolDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetByIdAsync called for symbol: {Id}", id);

        var symbol = new EnhancedSymbolDto
        {
            Id = id,
            Ticker = "MOCK",
            Display = "Mock Symbol",
            FullName = "Mock Symbol",
            AssetClass = new AssetClassDto { Code = "CRYPTO", Name = "Cryptocurrency" },
            Market = new MarketSummaryDto { Code = "MOCK_MARKET", Name = "Mock Market" },
            BaseCurrency = "MOCK",
            QuoteCurrency = "USDT",
            CurrentPrice = 100.0m,
            PriceChange24h = 5.0m,
            IsActive = true,
            IsTracked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult<EnhancedSymbolDto?>(symbol);
    }

    public Task<EnhancedSymbolDto?> GetByTickerAsync(string ticker, string? marketCode = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetByTickerAsync called for ticker: {Ticker}, market: {Market}", ticker, marketCode ?? "any");

        var symbol = new EnhancedSymbolDto
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Display = $"Mock {ticker}",
            FullName = $"Mock {ticker}",
            AssetClass = new AssetClassDto { Code = "CRYPTO", Name = "Cryptocurrency" },
            Market = new MarketSummaryDto { Code = marketCode ?? "MOCK_MARKET", Name = marketCode ?? "Mock Market" },
            BaseCurrency = ticker,
            QuoteCurrency = "USDT",
            CurrentPrice = 100.0m,
            PriceChange24h = 5.0m,
            IsActive = true,
            IsTracked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult<EnhancedSymbolDto?>(symbol);
    }

    public Task<PaginatedResponse<SymbolSummaryDto>> GetSymbolsAsync(BaseListRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetSymbolsAsync called with page: {Page}, size: {PageSize}", request.Page, request.PageSize);

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
            },
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
                IsActive = true
            }
        };

        var response = PaginatedResponse<SymbolSummaryDto>.SuccessResult(
            symbols,
            request.Page,
            request.PageSize,
            symbols.Count,
            "Mock symbols retrieved successfully"
        );

        return Task.FromResult(response);
    }

    public Task<List<SymbolSummaryDto>> GetByAssetClassAsync(Guid assetClassId, int? limit = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetByAssetClassAsync called for asset class: {AssetClassId}", assetClassId);

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

    public Task<List<SymbolSummaryDto>> GetByMarketAsync(Guid marketId, int? limit = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetByMarketAsync called for market: {MarketId}", marketId);

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

    public Task<List<SymbolSearchResultDto>> SearchAsync(SymbolSearchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock SearchAsync called with query: {Query}", request.Query);

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

    public Task<List<SymbolSummaryDto>> GetTrackedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetTrackedAsync called");

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

        return Task.FromResult(symbols);
    }

    public Task<List<SymbolSummaryDto>> GetPopularAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock GetPopularAsync called with limit: {Limit}", limit);

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

    public Task<EnhancedSymbolDto> CreateAsync(CreateSymbolRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock CreateAsync called for ticker: {Ticker}", request.Ticker);

        var symbol = new EnhancedSymbolDto
        {
            Id = Guid.NewGuid(),
            Ticker = request.Ticker,
            Display = request.Display,
            FullName = request.FullName,
            AssetClass = new AssetClassDto { Code = "CRYPTO", Name = "Cryptocurrency" },
            Market = new MarketSummaryDto { Code = "MOCK_MARKET", Name = "Mock Market" },
            BaseCurrency = request.BaseCurrency,
            QuoteCurrency = request.QuoteCurrency,
            CurrentPrice = 100.0m,
            PriceChange24h = 0.0m,
            IsActive = true,
            IsTracked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult(symbol);
    }

    public Task<EnhancedSymbolDto?> UpdateAsync(Guid id, UpdateSymbolRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock UpdateAsync called for symbol: {Id}", id);

        var symbol = new EnhancedSymbolDto
        {
            Id = id,
            Ticker = "UPDATED",
            Display = "Updated Symbol",
            FullName = "Updated Symbol",
            AssetClass = new AssetClassDto { Code = "CRYPTO", Name = "Cryptocurrency" },
            Market = new MarketSummaryDto { Code = "MOCK_MARKET", Name = "Mock Market" },
            BaseCurrency = "UPDATED",
            QuoteCurrency = "USDT",
            CurrentPrice = 100.0m,
            PriceChange24h = 0.0m,
            IsActive = true,
            IsTracked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult<EnhancedSymbolDto?>(symbol);
    }

    public Task<bool> UpdateTrackingStatusAsync(Guid id, bool isTracked, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock UpdateTrackingStatusAsync called for symbol: {Id}, tracked: {IsTracked}", id, isTracked);
        return Task.FromResult(true);
    }

    public Task<int> BulkUpdateTrackingStatusAsync(BulkUpdateSymbolTrackingRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock BulkUpdateTrackingStatusAsync called for {Count} symbols", request.SymbolIds.Count);
        return Task.FromResult(request.SymbolIds.Count);
    }

    public Task<bool> UpdatePriceDataAsync(Guid symbolId, decimal? currentPrice, decimal? priceChange24h, decimal? volume24h, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock UpdatePriceDataAsync called for symbol: {SymbolId}", symbolId);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock DeleteAsync called for symbol: {Id}", id);
        return Task.FromResult(true);
    }

    public Task<bool> IsTickerUniqueAsync(string ticker, Guid? marketId = null, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock IsTickerUniqueAsync called for ticker: {Ticker}", ticker);
        return Task.FromResult(true);
    }
}