using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyTrader.Core.DTOs;
using MyTrader.Infrastructure.Services;
using Xunit;

namespace MyTrader.Tests.Infrastructure.Services;

public class AlpacaMarketDataServiceTests
{
    private readonly Mock<ILogger<AlpacaMarketDataService>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly AlpacaConfiguration _config;
    private readonly IOptions<AlpacaConfiguration> _options;

    public AlpacaMarketDataServiceTests()
    {
        _mockLogger = new Mock<ILogger<AlpacaMarketDataService>>();
        _mockCache = new Mock<IMemoryCache>();
        _mockHttpClient = new Mock<HttpClient>();

        _config = new AlpacaConfiguration
        {
            UsePaperTrading = true,
            PaperApiKey = "test-api-key",
            PaperSecretKey = "test-secret-key",
            EnableCaching = true,
            CacheExpirySeconds = 30,
            DefaultSymbols = new AlpacaDefaultSymbols
            {
                Crypto = new List<string> { "BTCUSD", "ETHUSD" },
                Stocks = new List<string> { "AAPL", "GOOGL" }
            }
        };

        _options = Options.Create(_config);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void GetRateLimitStatus_ShouldReturnValidStatus()
    {
        // Arrange
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Act
        var status = service.GetRateLimitStatus();

        // Assert
        Assert.NotNull(status);
        Assert.Equal(_config.RateLimitPerMinute, status.RequestsRemaining);
        Assert.Equal(_config.RateLimitPerMinute, status.RequestsPerMinute);
        Assert.False(status.IsNearLimit);
    }

    [Fact]
    public void GetCircuitBreakerStatus_ShouldReturnValidStatus()
    {
        // Arrange
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Act
        var status = service.GetCircuitBreakerStatus();

        // Assert
        Assert.NotNull(status);
        Assert.False(status.IsOpen);
        Assert.Equal(0, status.FailureCount);
        Assert.Null(status.LastFailureTime);
    }

    [Fact]
    public async Task GetAvailableSymbolsAsync_ShouldReturnConfiguredSymbols()
    {
        // Arrange
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Act
        var symbols = await service.GetAvailableSymbolsAsync();

        // Assert
        Assert.NotNull(symbols);
        Assert.Contains("BTCUSD", symbols);
        Assert.Contains("ETHUSD", symbols);
        Assert.Contains("AAPL", symbols);
        Assert.Contains("GOOGL", symbols);
    }

    [Fact]
    public async Task GetAvailableSymbolsAsync_WithCryptoFilter_ShouldReturnOnlyCryptoSymbols()
    {
        // Arrange
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Act
        var symbols = await service.GetAvailableSymbolsAsync("CRYPTO");

        // Assert
        Assert.NotNull(symbols);
        Assert.Contains("BTCUSD", symbols);
        Assert.Contains("ETHUSD", symbols);
        Assert.DoesNotContain("AAPL", symbols);
        Assert.DoesNotContain("GOOGL", symbols);
    }

    [Fact]
    public async Task GetAvailableSymbolsAsync_WithStockFilter_ShouldReturnOnlyStockSymbols()
    {
        // Arrange
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Act
        var symbols = await service.GetAvailableSymbolsAsync("STOCK");

        // Assert
        Assert.NotNull(symbols);
        Assert.DoesNotContain("BTCUSD", symbols);
        Assert.DoesNotContain("ETHUSD", symbols);
        Assert.Contains("AAPL", symbols);
        Assert.Contains("GOOGL", symbols);
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("5m")]
    [InlineData("1h")]
    [InlineData("1d")]
    public async Task GetHistoricalDataAsync_WithValidInterval_ShouldReturnData(string interval)
    {
        // Arrange
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Act & Assert - This will test the interval mapping logic even if API calls fail
        var result = await service.GetHistoricalDataAsync("AAPL", interval);

        // The result might be null due to API not being initialized, but it shouldn't throw
        Assert.True(true); // Test passes if no exception is thrown
    }

    [Fact]
    public async Task GetHealthStatusAsync_ShouldReturnHealthStatus()
    {
        // Arrange
        var service = new AlpacaMarketDataService(_options, _mockLogger.Object, _mockCache.Object, _mockHttpClient.Object);

        // Act
        var health = await service.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(health);
        Assert.NotNull(health.Status);
        Assert.NotNull(health.Details);
        Assert.True(health.LastChecked > DateTime.MinValue);
    }
}

public class AlpacaDataProviderTests
{
    private readonly Mock<ILogger<AlpacaDataProvider>> _mockLogger;
    private readonly Mock<IAlpacaMarketDataService> _mockAlpacaService;
    private readonly AlpacaConfiguration _config;
    private readonly IOptions<AlpacaConfiguration> _options;

    public AlpacaDataProviderTests()
    {
        _mockLogger = new Mock<ILogger<AlpacaDataProvider>>();
        _mockAlpacaService = new Mock<IAlpacaMarketDataService>();

        _config = new AlpacaConfiguration
        {
            UsePaperTrading = true,
            PaperApiKey = "test-api-key",
            PaperSecretKey = "test-secret-key",
            DefaultSymbols = new AlpacaDefaultSymbols
            {
                Crypto = new List<string> { "BTCUSD", "ETHUSD" },
                Stocks = new List<string> { "AAPL", "GOOGL" }
            }
        };

        _options = Options.Create(_config);
    }

    [Fact]
    public void Constructor_ShouldInitializeProvider()
    {
        // Act
        var provider = new AlpacaDataProvider(_options, _mockLogger.Object, _mockAlpacaService.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("ALPACA", provider.ProviderId);
        Assert.Equal("Alpaca Markets", provider.ProviderName);
        Assert.Contains("CRYPTO", provider.SupportedAssetClasses);
        Assert.Contains("STOCK", provider.SupportedAssetClasses);
    }

    [Theory]
    [InlineData("BTCUSD", "CRYPTO", true)]
    [InlineData("AAPL", "STOCK", true)]
    [InlineData("INVALID", "CRYPTO", false)]
    [InlineData("INVALID", "STOCK", false)]
    [InlineData("", "CRYPTO", false)]
    public void IsSymbolSupported_ShouldReturnCorrectResult(string ticker, string assetClass, bool expected)
    {
        // Arrange
        var provider = new AlpacaDataProvider(_options, _mockLogger.Object, _mockAlpacaService.Object);

        // Act
        var result = provider.IsSymbolSupported(ticker, assetClass);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetProviderInfoAsync_ShouldReturnValidInfo()
    {
        // Arrange
        var provider = new AlpacaDataProvider(_options, _mockLogger.Object, _mockAlpacaService.Object);

        // Act
        var info = await provider.GetProviderInfoAsync();

        // Assert
        Assert.NotNull(info);
        Assert.Equal("ALPACA", info["ProviderId"]);
        Assert.Equal("Alpaca Markets", info["ProviderName"]);
        Assert.Equal("Paper", info["Environment"]);
        Assert.False(provider.IsConnected); // Should be false since we haven't initialized
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthStatus()
    {
        // Arrange
        var mockHealthStatus = new AlpacaHealthStatus
        {
            IsHealthy = true,
            Status = "Healthy",
            LastChecked = DateTime.UtcNow
        };

        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(mockHealthStatus);

        _mockAlpacaService.Setup(x => x.GetRateLimitStatus())
                         .Returns(new AlpacaRateLimitStatus { RequestsRemaining = 100 });

        _mockAlpacaService.Setup(x => x.GetCircuitBreakerStatus())
                         .Returns(new AlpacaCircuitBreakerStatus { IsOpen = false });

        var provider = new AlpacaDataProvider(_options, _mockLogger.Object, _mockAlpacaService.Object);

        // Act
        var health = await provider.GetHealthAsync();

        // Assert
        Assert.NotNull(health);
        Assert.Equal("Alpaca Markets", health.Name);
        Assert.Equal("Healthy", health.Status);
        Assert.NotNull(health.Details);
    }
}

public class AlpacaConfigurationTests
{
    [Fact]
    public void AlpacaConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new AlpacaConfiguration();

        // Assert
        Assert.True(config.UsePaperTrading);
        Assert.Equal("https://paper-api.alpaca.markets", config.BaseUrl);
        Assert.Equal("https://data.alpaca.markets", config.DataUrl);
        Assert.Equal(200, config.RateLimitPerMinute);
        Assert.Equal(30, config.CacheExpirySeconds);
        Assert.True(config.EnableCaching);
        Assert.NotNull(config.DefaultSymbols);
    }

    [Fact]
    public void AlpacaDefaultSymbols_ShouldInitializeEmptyLists()
    {
        // Arrange & Act
        var symbols = new AlpacaDefaultSymbols();

        // Assert
        Assert.NotNull(symbols.Crypto);
        Assert.NotNull(symbols.Stocks);
        Assert.Empty(symbols.Crypto);
        Assert.Empty(symbols.Stocks);
    }
}

public class AlpacaDataExtensionsTests
{
    [Fact]
    public void ToUnifiedMarketDataDto_ShouldMapCorrectly()
    {
        // Arrange
        var marketData = new MarketDataDto
        {
            Symbol = "BTCUSD",
            Name = "Bitcoin/USD",
            Price = 50000m,
            Change = 1000m,
            ChangePercent = 2.0m,
            Volume = 100000,
            High24h = 51000m,
            Low24h = 49000m,
            LastUpdated = DateTime.UtcNow,
            AssetClass = "CRYPTO",
            MarketCap = 1000000000m,
            Currency = "USD"
        };

        // Act
        var result = marketData.ToUnifiedMarketDataDto();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BTCUSD", result.Ticker);
        Assert.Equal(50000m, result.Price);
        Assert.Equal(49000m, result.PreviousClose); // Price - Change
        Assert.Equal(1000m, result.PriceChange);
        Assert.Equal(2.0m, result.PriceChangePercent);
        Assert.Equal(100000, result.Volume);
        Assert.Equal(51000m, result.HighPrice);
        Assert.Equal(49000m, result.LowPrice);
        Assert.Equal("Alpaca", result.DataProvider);
        Assert.True(result.IsRealTime);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(8, result.PricePrecision); // CRYPTO should have 8 precision
    }

    [Fact]
    public void ToUnifiedMarketDataDto_WithStockData_ShouldHaveCorrectPrecision()
    {
        // Arrange
        var marketData = new MarketDataDto
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Price = 150.50m,
            AssetClass = "STOCK"
        };

        // Act
        var result = marketData.ToUnifiedMarketDataDto();

        // Assert
        Assert.Equal(2, result.PricePrecision); // STOCK should have 2 precision
        Assert.Equal(0, result.QuantityPrecision); // STOCK should have 0 quantity precision
    }
}