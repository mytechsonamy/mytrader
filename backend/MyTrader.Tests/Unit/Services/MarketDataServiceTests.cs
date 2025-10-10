using FluentAssertions;
using Moq;
using AutoFixture;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Services;
using Xunit;

namespace MyTrader.Tests.Unit.Services;

/// <summary>
/// Unit tests for MarketDataService - Testing market data processing logic
/// </summary>
public class MarketDataServiceTests
{
    private readonly Mock<ILogger<MultiAssetDataService>> _loggerMock;
    private readonly Mock<IYahooFinanceApiService> _yahooServiceMock;
    private readonly Mock<IBinanceWebSocketService> _binanceServiceMock;
    private readonly MultiAssetDataService _service;
    private readonly IFixture _fixture;

    public MarketDataServiceTests()
    {
        _loggerMock = new Mock<ILogger<MultiAssetDataService>>();
        _yahooServiceMock = new Mock<IYahooFinanceApiService>();
        _binanceServiceMock = new Mock<IBinanceWebSocketService>();
        _fixture = new Fixture();
        
        _service = new MultiAssetDataService(
            _loggerMock.Object,
            _yahooServiceMock.Object,
            _binanceServiceMock.Object);
    }

    [Fact]
    public async Task GetMarketDataAsync_WithValidSymbol_ReturnsMarketData()
    {
        // Arrange
        var symbol = "AAPL";
        var expectedData = _fixture.Build<MarketData>()
            .With(x => x.Symbol, symbol)
            .With(x => x.Price, 150.00m)
            .Create();

        _yahooServiceMock.Setup(x => x.GetRealTimeQuoteAsync(symbol))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _service.GetMarketDataAsync(symbol);

        // Assert
        result.Should().NotBeNull();
        result.Symbol.Should().Be(symbol);
        result.Price.Should().Be(150.00m);
        
        _yahooServiceMock.Verify(x => x.GetRealTimeQuoteAsync(symbol), Times.Once);
    }

    [Fact]
    public async Task GetMarketDataAsync_WithInvalidSymbol_ReturnsNull()
    {
        // Arrange
        var invalidSymbol = "INVALID";
        _yahooServiceMock.Setup(x => x.GetRealTimeQuoteAsync(invalidSymbol))
            .ReturnsAsync((MarketData)null!);

        // Act
        var result = await _service.GetMarketDataAsync(invalidSymbol);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("BTCUSDT", "crypto")]
    [InlineData("AAPL", "stock")]
    [InlineData("EUR/USD", "forex")]
    public async Task GetMarketDataAsync_WithDifferentAssetClasses_UsesCorrectProvider(string symbol, string assetClass)
    {
        // Arrange
        var marketData = _fixture.Build<MarketData>()
            .With(x => x.Symbol, symbol)
            .With(x => x.AssetClass, assetClass)
            .Create();

        if (assetClass == "crypto")
        {
            _binanceServiceMock.Setup(x => x.GetMarketDataAsync(symbol))
                .ReturnsAsync(marketData);
        }
        else
        {
            _yahooServiceMock.Setup(x => x.GetRealTimeQuoteAsync(symbol))
                .ReturnsAsync(marketData);
        }

        // Act
        var result = await _service.GetMarketDataAsync(symbol);

        // Assert
        result.Should().NotBeNull();
        result.Symbol.Should().Be(symbol);
        result.AssetClass.Should().Be(assetClass);
    }

    [Fact]
    public async Task GetMultipleMarketDataAsync_WithValidSymbols_ReturnsAllData()
    {
        // Arrange
        var symbols = new[] { "AAPL", "GOOGL", "MSFT" };
        var marketDataList = symbols.Select(s => 
            _fixture.Build<MarketData>()
                .With(x => x.Symbol, s)
                .Create()).ToList();

        foreach (var data in marketDataList)
        {
            _yahooServiceMock.Setup(x => x.GetRealTimeQuoteAsync(data.Symbol))
                .ReturnsAsync(data);
        }

        // Act
        var results = await _service.GetMultipleMarketDataAsync(symbols);

        // Assert
        results.Should().HaveCount(3);
        results.Select(r => r.Symbol).Should().BeEquivalentTo(symbols);
    }

    [Fact]
    public void Subscribe_WithValidSymbol_AddsToSubscriptions()
    {
        // Arrange
        var symbol = "AAPL";
        var callback = new Mock<Action<MarketData>>();

        // Act
        _service.Subscribe(symbol, callback.Object);

        // Assert
        _service.IsSubscribed(symbol).Should().BeTrue();
    }

    [Fact]
    public void Unsubscribe_WithExistingSubscription_RemovesFromSubscriptions()
    {
        // Arrange
        var symbol = "AAPL";
        var callback = new Mock<Action<MarketData>>();
        _service.Subscribe(symbol, callback.Object);

        // Act
        _service.Unsubscribe(symbol, callback.Object);

        // Assert
        _service.IsSubscribed(symbol).Should().BeFalse();
    }
}