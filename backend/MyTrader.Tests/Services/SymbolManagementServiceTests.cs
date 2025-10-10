using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Core.Data;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using MyTrader.Infrastructure.Data;
using MyTrader.Infrastructure.Services;
using Xunit;

namespace MyTrader.Tests.Services;

/// <summary>
/// Comprehensive unit tests for SymbolManagementService.
/// Tests cover caching, database queries, error handling, and fallback mechanisms.
/// </summary>
public class SymbolManagementServiceTests : IDisposable
{
    private readonly TradingDbContext _dbContext;
    private readonly Mock<ISymbolCacheService> _mockCacheService;
    private readonly Mock<ILogger<SymbolManagementService>> _mockLogger;
    private readonly SymbolManagementService _service;
    private readonly List<Symbol> _testSymbols;

    public SymbolManagementServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TradingDbContext(options);

        // Setup mocks
        _mockCacheService = new Mock<ISymbolCacheService>();
        _mockLogger = new Mock<ILogger<SymbolManagementService>>();

        // Create service
        _service = new SymbolManagementService(_dbContext, _mockCacheService.Object, _mockLogger.Object);

        // Seed test data
        _testSymbols = SeedTestData();
    }

    private List<Symbol> SeedTestData()
    {
        var assetClass = new AssetClass
        {
            Id = Guid.NewGuid(),
            Code = "CRYPTO",
            Name = "Cryptocurrency",
            PrimaryCurrency = "USD",
            IsActive = true,
            DisplayOrder = 1
        };

        var market = new Market
        {
            Id = Guid.NewGuid(),
            Code = "BINANCE",
            Name = "Binance Exchange",
            CountryCode = "US",
            Timezone = "UTC",
            PrimaryCurrency = "USD",
            IsActive = true,
            DisplayOrder = 1
        };

        _dbContext.AssetClasses.Add(assetClass);
        _dbContext.Markets.Add(market);

        var symbols = new List<Symbol>
        {
            new Symbol
            {
                Id = Guid.NewGuid(),
                Ticker = "BTCUSDT",
                Display = "Bitcoin",
                AssetClass = "CRYPTO",
                Venue = "BINANCE",
                BaseCurrency = "BTC",
                QuoteCurrency = "USDT",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                DisplayOrder = 100,
                AssetClassId = assetClass.Id,
                MarketId = market.Id
            },
            new Symbol
            {
                Id = Guid.NewGuid(),
                Ticker = "ETHUSDT",
                Display = "Ethereum",
                AssetClass = "CRYPTO",
                Venue = "BINANCE",
                BaseCurrency = "ETH",
                QuoteCurrency = "USDT",
                IsActive = true,
                IsTracked = true,
                IsPopular = true,
                DisplayOrder = 95,
                AssetClassId = assetClass.Id,
                MarketId = market.Id
            },
            new Symbol
            {
                Id = Guid.NewGuid(),
                Ticker = "XRPUSDT",
                Display = "Ripple",
                AssetClass = "CRYPTO",
                Venue = "BINANCE",
                BaseCurrency = "XRP",
                QuoteCurrency = "USDT",
                IsActive = true,
                IsTracked = true,
                IsPopular = false,
                DisplayOrder = 90,
                AssetClassId = assetClass.Id,
                MarketId = market.Id
            },
            new Symbol
            {
                Id = Guid.NewGuid(),
                Ticker = "ADAUSDT",
                Display = "Cardano",
                AssetClass = "CRYPTO",
                Venue = "BINANCE",
                BaseCurrency = "ADA",
                QuoteCurrency = "USDT",
                IsActive = false, // Inactive symbol
                IsTracked = false,
                IsPopular = false,
                DisplayOrder = 0,
                AssetClassId = assetClass.Id,
                MarketId = market.Id
            }
        };

        _dbContext.Symbols.AddRange(symbols);
        _dbContext.SaveChanges();

        return symbols;
    }

    [Fact]
    public async Task GetActiveSymbolsForBroadcast_ReturnsOrderedSymbols()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Act
        var result = await _service.GetActiveSymbolsForBroadcastAsync("CRYPTO", "BINANCE");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Only active symbols
        Assert.Equal("BTCUSDT", result[0].Ticker); // Highest priority first (DisplayOrder DESC)
        Assert.Equal("ETHUSDT", result[1].Ticker);
        Assert.Equal("XRPUSDT", result[2].Ticker);

        // Verify caching was called
        _mockCacheService.Verify(x => x.SetCachedSymbols(It.IsAny<string>(), It.IsAny<List<Symbol>>(), 5), Times.Once);
    }

    [Fact]
    public async Task GetActiveSymbolsForBroadcast_UsesCachedData_WhenAvailable()
    {
        // Arrange
        var cachedSymbols = _testSymbols.Where(s => s.IsActive).Take(2).ToList();
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns(cachedSymbols);

        // Act
        var result = await _service.GetActiveSymbolsForBroadcastAsync("CRYPTO", "BINANCE");

        // Assert
        Assert.Equal(cachedSymbols.Count, result.Count);
        Assert.Same(cachedSymbols, result); // Should return exact cached instance

        // Verify database was not queried (no SetCachedSymbols called)
        _mockCacheService.Verify(x => x.SetCachedSymbols(It.IsAny<string>(), It.IsAny<List<Symbol>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDefaultSymbols_ReturnsOnlyPopularSymbols()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Act
        var result = await _service.GetDefaultSymbolsAsync("CRYPTO");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Only BTC and ETH are popular
        Assert.All(result, s => Assert.True(s.IsPopular));
        Assert.Contains(result, s => s.Ticker == "BTCUSDT");
        Assert.Contains(result, s => s.Ticker == "ETHUSDT");
    }

    [Fact]
    public async Task GetDefaultSymbols_CachesResults()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Act
        await _service.GetDefaultSymbolsAsync("CRYPTO");

        // Assert
        _mockCacheService.Verify(x => x.SetCachedSymbols(It.IsAny<string>(), It.IsAny<List<Symbol>>(), 10), Times.Once);
    }

    [Fact]
    public async Task GetUserSymbols_ReturnsDefaultsWhenNoPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Act
        var result = await _service.GetUserSymbolsAsync(userId, "CRYPTO");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Should return defaults (popular symbols)
    }

    [Fact]
    public async Task GetUserSymbols_ReturnsUserPreferences_WhenExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var symbol = _testSymbols.First(s => s.Ticker == "XRPUSDT");

        var preference = new UserDashboardPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SymbolId = symbol.Id,
            IsVisible = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.UserDashboardPreferences.Add(preference);
        await _dbContext.SaveChangesAsync();

        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Act
        var result = await _service.GetUserSymbolsAsync(userId.ToString(), "CRYPTO");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("XRPUSDT", result[0].Ticker);
    }

    [Fact]
    public async Task UpdateSymbolPreferences_RemovesOldAndAddsNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldSymbol = _testSymbols.First(s => s.Ticker == "BTCUSDT");
        var newSymbol = _testSymbols.First(s => s.Ticker == "ETHUSDT");

        // Add old preference
        var oldPreference = new UserDashboardPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SymbolId = oldSymbol.Id,
            IsVisible = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.UserDashboardPreferences.Add(oldPreference);
        await _dbContext.SaveChangesAsync();

        var newSymbolIds = new List<string> { newSymbol.Id.ToString() };

        // Act
        await _service.UpdateSymbolPreferencesAsync(userId.ToString(), newSymbolIds);

        // Assert
        var preferences = await _dbContext.UserDashboardPreferences.Where(p => p.UserId == userId).ToListAsync();
        Assert.Single(preferences);
        Assert.Equal(newSymbol.Id, preferences[0].SymbolId);

        // Verify cache was cleared
        _mockCacheService.Verify(x => x.ClearCache(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReloadSymbols_ClearsCacheAndPrewarmsCommonQueries()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Act
        await _service.ReloadSymbolsAsync();

        // Assert
        _mockCacheService.Verify(x => x.ClearAllCaches(), Times.Once);
        _mockCacheService.Verify(x => x.SetCachedSymbols(It.IsAny<string>(), It.IsAny<List<Symbol>>(), It.IsAny<int>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task GetSymbolsByAssetClass_ExcludesInactiveByDefault()
    {
        // Act
        var result = await _service.GetSymbolsByAssetClassAsync("CRYPTO", includeInactive: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Only active symbols
        Assert.All(result, s => Assert.True(s.IsActive));
    }

    [Fact]
    public async Task GetSymbolsByAssetClass_IncludesInactiveWhenRequested()
    {
        // Act
        var result = await _service.GetSymbolsByAssetClassAsync("CRYPTO", includeInactive: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count); // All symbols including inactive
    }

    [Fact]
    public async Task GetSymbolByTicker_ReturnsCorrectSymbol()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedSymbol(It.IsAny<string>())).Returns((Symbol?)null);

        // Act
        var result = await _service.GetSymbolByTickerAsync("BTCUSDT", "BINANCE");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BTCUSDT", result.Ticker);
        Assert.Equal("BINANCE", result.Venue);

        // Verify caching
        _mockCacheService.Verify(x => x.SetCachedSymbol(It.IsAny<string>(), It.IsAny<Symbol>(), 10), Times.Once);
    }

    [Fact]
    public async Task GetSymbolByTicker_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedSymbol(It.IsAny<string>())).Returns((Symbol?)null);

        // Act
        var result = await _service.GetSymbolByTickerAsync("NONEXISTENT", "BINANCE");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSymbolByTicker_UsesCachedData()
    {
        // Arrange
        var cachedSymbol = _testSymbols.First();
        _mockCacheService.Setup(x => x.GetCachedSymbol(It.IsAny<string>())).Returns(cachedSymbol);

        // Act
        var result = await _service.GetSymbolByTickerAsync("BTCUSDT");

        // Assert
        Assert.Same(cachedSymbol, result);

        // Verify database was not queried
        _mockCacheService.Verify(x => x.SetCachedSymbol(It.IsAny<string>(), It.IsAny<Symbol>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLastBroadcastTime_UpdatesTimestamp()
    {
        // Arrange
        var symbol = _testSymbols.First();
        var broadcastTime = DateTime.UtcNow;

        // Act
        await _service.UpdateLastBroadcastTimeAsync(symbol.Id, broadcastTime);

        // Assert
        var updated = await _dbContext.Symbols.FindAsync(symbol.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.PriceUpdatedAt);
        Assert.True((updated.PriceUpdatedAt.Value - broadcastTime).TotalSeconds < 1);
    }

    [Fact]
    public async Task GetSymbolsDueBroadcast_ReturnsSymbolsPastInterval()
    {
        // Arrange
        var oldTime = DateTime.UtcNow.AddMinutes(-10);
        var symbol = _testSymbols.First(s => s.IsActive);
        symbol.PriceUpdatedAt = oldTime;
        _dbContext.Symbols.Update(symbol);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSymbolsDueBroadcastAsync("CRYPTO", "BINANCE", minIntervalSeconds: 60);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, s => s.Id == symbol.Id);
    }

    [Fact]
    public async Task GetSymbolsDueBroadcast_ExcludesRecentlyBroadcast()
    {
        // Arrange
        var recentTime = DateTime.UtcNow;
        var symbol = _testSymbols.First(s => s.IsActive);
        symbol.PriceUpdatedAt = recentTime;
        _dbContext.Symbols.Update(symbol);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetSymbolsDueBroadcastAsync("CRYPTO", "BINANCE", minIntervalSeconds: 3600);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result, s => s.Id == symbol.Id);
    }

    [Fact]
    public async Task GetActiveSymbolsForBroadcast_HandlesDatabaseError_ReturnsFallback()
    {
        // Arrange - Dispose context to simulate database error
        _dbContext.Dispose();
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Create new service with disposed context
        var faultyService = new SymbolManagementService(_dbContext, _mockCacheService.Object, _mockLogger.Object);

        // Act
        var result = await faultyService.GetActiveSymbolsForBroadcastAsync("CRYPTO", "BINANCE");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result); // Should return fallback symbols
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetUserSymbols_HandlesInvalidUserId_ReturnsDefaults()
    {
        // Arrange
        _mockCacheService.Setup(x => x.GetCachedSymbols(It.IsAny<string>())).Returns((List<Symbol>?)null);

        // Act
        var result = await _service.GetUserSymbolsAsync("invalid-guid", "CRYPTO");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result); // Should return default symbols
    }

    [Fact]
    public async Task GetSymbolsByAssetClass_HandlesEmptyAssetClass_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetSymbolsByAssetClassAsync("", includeInactive: false);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
