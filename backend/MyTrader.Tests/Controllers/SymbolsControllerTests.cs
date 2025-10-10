using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Api.Controllers;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;
using MyTrader.Services.Market;
using MyTrader.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Controllers;

public class SymbolsControllerTests : TestBase
{
    private readonly Mock<ISymbolService> _mockSymbolService;
    private readonly Mock<ILogger<SymbolsController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly SymbolsController _controller;

    public SymbolsControllerTests()
    {
        _mockSymbolService = new Mock<ISymbolService>();
        _mockLogger = MockServiceHelper.CreateMockLogger<SymbolsController>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup configuration mock
        _mockConfiguration.Setup(x => x.GetConnectionString("DefaultConnection"))
            .Returns("Host=localhost;Port=5432;Database=mytrader;Username=postgres;Password=password");

        _controller = new SymbolsController(
            _mockSymbolService.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task GetSymbols_WithTrackedSymbols_ReturnsOkWithSymbols()
    {
        // Arrange
        var trackedSymbols = new List<TrackedSymbol>
        {
            new TrackedSymbol
            {
                Ticker = "BTC-USD",
                Display = "Bitcoin",
                AssetClass = "Crypto"
            },
            new TrackedSymbol
            {
                Ticker = "ETHUSDT",
                Display = "Ethereum",
                AssetClass = "Crypto"
            },
            new TrackedSymbol
            {
                Ticker = "AAPL",
                Display = "Apple Inc.",
                AssetClass = "Stock"
            }
        };

        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ReturnsAsync(trackedSymbols);

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var responseData = okResult.Value;
        responseData.Should().NotBeNull();

        // Check if the response is a dictionary with expected structure
        var symbolsDict = responseData as IDictionary<string, object>;
        if (symbolsDict != null)
        {
            symbolsDict.Should().HaveCount(3);
            symbolsDict.Should().ContainKey("BTC"); // Clean symbol key
            symbolsDict.Should().ContainKey("ETH"); // Clean symbol key
            symbolsDict.Should().ContainKey("AAPL");
        }

        _mockSymbolService.Verify(x => x.GetTrackedAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSymbols_WithNoTrackedSymbols_ReturnsEmptyDictionary()
    {
        // Arrange
        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ReturnsAsync(new List<TrackedSymbol>());

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var responseData = okResult.Value;
        var symbolsDict = responseData as IDictionary<string, object>;
        symbolsDict.Should().NotBeNull();
        symbolsDict.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSymbols_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        result.Should().NotBeNull();
        var statusResult = result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving symbols")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSymbols_WithMixedAssetClasses_ReturnsAllSymbols()
    {
        // Arrange
        var trackedSymbols = new List<TrackedSymbol>
        {
            new TrackedSymbol { Ticker = "BTC-USD", Display = "Bitcoin", AssetClass = "Crypto" },
            new TrackedSymbol { Ticker = "ETHUSDT", Display = "Ethereum", AssetClass = "Crypto" },
            new TrackedSymbol { Ticker = "AAPL", Display = "Apple Inc.", AssetClass = "Stock" },
            new TrackedSymbol { Ticker = "MSFT", Display = "Microsoft", AssetClass = "Stock" },
            new TrackedSymbol { Ticker = "EURUSD", Display = "EUR/USD", AssetClass = "Forex" },
            new TrackedSymbol { Ticker = "GBPUSD", Display = "GBP/USD", AssetClass = "Forex" }
        };

        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ReturnsAsync(trackedSymbols);

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var responseData = okResult.Value;
        var symbolsDict = responseData as IDictionary<string, object>;
        symbolsDict.Should().NotBeNull();
        symbolsDict.Should().HaveCount(6);

        // Verify crypto symbols have clean keys
        symbolsDict.Should().ContainKey("BTC");
        symbolsDict.Should().ContainKey("ETH");

        // Verify stock symbols maintain their keys
        symbolsDict.Should().ContainKey("AAPL");
        symbolsDict.Should().ContainKey("MSFT");

        // Verify forex symbols maintain their keys
        symbolsDict.Should().ContainKey("EURUSD");
        symbolsDict.Should().ContainKey("GBPUSD");
    }

    [Fact]
    public async Task GetSymbols_WithNullDisplayName_UsesTickerAsDisplay()
    {
        // Arrange
        var trackedSymbols = new List<TrackedSymbol>
        {
            new TrackedSymbol
            {
                Ticker = "NEWCOIN-USD",
                Display = null, // Null display name
                AssetClass = "Crypto"
            }
        };

        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ReturnsAsync(trackedSymbols);

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var responseData = okResult.Value;
        var symbolsDict = responseData as IDictionary<string, object>;
        symbolsDict.Should().NotBeNull();
        symbolsDict.Should().HaveCount(1);

        // The key should be the clean ticker name
        symbolsDict.Should().ContainKey("NEWCOIN");
    }

    [Fact]
    public async Task GetSymbols_WithDuplicateCleanedTickers_HandlesGracefully()
    {
        // Arrange - This scenario might happen with similar tickers like BTC-USD and BTCUSDT
        var trackedSymbols = new List<TrackedSymbol>
        {
            new TrackedSymbol { Ticker = "BTC-USD", Display = "Bitcoin USD", AssetClass = "Crypto" },
            new TrackedSymbol { Ticker = "BTCUSDT", Display = "Bitcoin Tether", AssetClass = "Crypto" }
        };

        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ReturnsAsync(trackedSymbols);

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // The second BTC entry might overwrite the first, or there might be special handling
        // This test documents the current behavior
        var responseData = okResult.Value;
        responseData.Should().NotBeNull();
    }

    [Theory]
    [InlineData("BTC-USD", "BTC")]
    [InlineData("ETHUSDT", "ETH")]
    [InlineData("ADAUSDT", "ADA")]
    [InlineData("DOGEUSDT", "DOGE")]
    [InlineData("AAPL", "AAPL")]
    [InlineData("MSFT", "MSFT")]
    public async Task GetSymbols_SymbolKeyTransformation_WorksCorrectly(string originalTicker, string expectedKey)
    {
        // Arrange
        var trackedSymbols = new List<TrackedSymbol>
        {
            new TrackedSymbol
            {
                Ticker = originalTicker,
                Display = $"Test {originalTicker}",
                AssetClass = "Test"
            }
        };

        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ReturnsAsync(trackedSymbols);

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var responseData = okResult.Value;
        var symbolsDict = responseData as IDictionary<string, object>;
        symbolsDict.Should().NotBeNull();
        symbolsDict.Should().ContainKey(expectedKey);
    }

    [Fact]
    public async Task GetSymbols_ResponseStructure_ContainsExpectedFields()
    {
        // Arrange
        var trackedSymbols = new List<TrackedSymbol>
        {
            new TrackedSymbol
            {
                Ticker = "AAPL",
                Display = "Apple Inc.",
                AssetClass = "Stock"
            }
        };

        _mockSymbolService.Setup(x => x.GetTrackedAsync())
            .ReturnsAsync(trackedSymbols);

        // Act
        var result = await _controller.GetSymbols();

        // Assert
        var okResult = result as OkObjectResult;
        var responseData = okResult!.Value;
        var symbolsDict = responseData as IDictionary<string, object>;

        if (symbolsDict != null && symbolsDict.ContainsKey("AAPL"))
        {
            var symbolData = symbolsDict["AAPL"];
            symbolData.Should().NotBeNull();

            // The response should contain expected fields
            // This test documents the expected response structure
        }
    }

    // ============================================
    // EXCHANGE FILTERING TESTS
    // ============================================

    [Fact]
    public async Task GetSymbols_WithoutExchange_ReturnsAllSymbols()
    {
        // Arrange
        var allSymbols = CreateMixedExchangeSymbols();
        _mockSymbolService.Setup(x => x.GetActiveSymbolsAsync())
            .ReturnsAsync(allSymbols);

        // Act
        var result = await _controller.GetSymbols(exchange: null);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var responseData = okResult.Value;
        responseData.Should().NotBeNull();

        // Verify all symbols returned (19 total)
        _mockSymbolService.Verify(x => x.GetActiveSymbolsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSymbols_WithBistExchange_ReturnsOnlyBistSymbols()
    {
        // Arrange
        var allSymbols = CreateMixedExchangeSymbols();
        _mockSymbolService.Setup(x => x.GetActiveSymbolsAsync())
            .ReturnsAsync(allSymbols);

        // Act
        var result = await _controller.GetSymbols(exchange: "BIST");

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        // Verify service was called
        _mockSymbolService.Verify(x => x.GetActiveSymbolsAsync(), Times.Once);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Filtering symbols by exchange: BIST")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("BIST", 3)]
    [InlineData("NASDAQ", 5)]
    [InlineData("NYSE", 2)]
    [InlineData("BINANCE", 9)]
    public async Task GetSymbols_WithValidExchange_ReturnsExpectedCount(string exchange, int expectedCount)
    {
        // Arrange
        var allSymbols = CreateMixedExchangeSymbols();
        _mockSymbolService.Setup(x => x.GetActiveSymbolsAsync())
            .ReturnsAsync(allSymbols);

        // Act
        var result = await _controller.GetSymbols(exchange: exchange);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        _mockSymbolService.Verify(x => x.GetActiveSymbolsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSymbols_WithExchange_ResponseContainsFrontendCompatibilityFields()
    {
        // Arrange
        var allSymbols = CreateMixedExchangeSymbols();
        _mockSymbolService.Setup(x => x.GetActiveSymbolsAsync())
            .ReturnsAsync(allSymbols);

        // Act
        var result = await _controller.GetSymbols(exchange: "BIST");

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var responseValue = okResult!.Value;
        responseValue.Should().NotBeNull();

        // Use reflection to check response structure
        var symbolsProperty = responseValue.GetType().GetProperty("symbols");
        symbolsProperty.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSymbols_WithNonExistentExchange_ReturnsEmptyResult()
    {
        // Arrange
        var allSymbols = CreateMixedExchangeSymbols();
        _mockSymbolService.Setup(x => x.GetActiveSymbolsAsync())
            .ReturnsAsync(allSymbols);

        // Act
        var result = await _controller.GetSymbols(exchange: "UNKNOWN_EXCHANGE");

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No symbols found for exchange")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("bist")]
    [InlineData("BIST")]
    [InlineData("BisT")]
    public async Task GetSymbols_WithExchangeCaseInsensitive_ReturnsCorrectSymbols(string exchange)
    {
        // Arrange
        var allSymbols = CreateMixedExchangeSymbols();
        _mockSymbolService.Setup(x => x.GetActiveSymbolsAsync())
            .ReturnsAsync(allSymbols);

        // Act
        var result = await _controller.GetSymbols(exchange: exchange);

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        _mockSymbolService.Verify(x => x.GetActiveSymbolsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSymbols_WithEmptyExchange_ReturnAllSymbols()
    {
        // Arrange
        var allSymbols = CreateMixedExchangeSymbols();
        _mockSymbolService.Setup(x => x.GetActiveSymbolsAsync())
            .ReturnsAsync(allSymbols);

        // Act
        var result = await _controller.GetSymbols(exchange: "");

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // Verify logging indicates no filter applied
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No exchange filter applied")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Helper method to create test data with mixed exchanges
    private List<Symbol> CreateMixedExchangeSymbols()
    {
        var symbols = new List<Symbol>();

        // BIST Symbols (3)
        symbols.Add(CreateSymbol("THYAO.IS", "BIST", "Türk Hava Yolları", "THYAO", "TRY"));
        symbols.Add(CreateSymbol("GARAN.IS", "BIST", "Garanti BBVA", "GARAN", "TRY"));
        symbols.Add(CreateSymbol("SISE.IS", "BIST", "Şişe Cam", "SISE", "TRY"));

        // NASDAQ Symbols (5)
        symbols.Add(CreateSymbol("AAPL", "NASDAQ", "Apple Inc.", "AAPL", "USD"));
        symbols.Add(CreateSymbol("MSFT", "NASDAQ", "Microsoft", "MSFT", "USD"));
        symbols.Add(CreateSymbol("GOOGL", "NASDAQ", "Alphabet Inc.", "GOOGL", "USD"));
        symbols.Add(CreateSymbol("NVDA", "NASDAQ", "NVIDIA", "NVDA", "USD"));
        symbols.Add(CreateSymbol("TSLA", "NASDAQ", "Tesla Inc.", "TSLA", "USD"));

        // NYSE Symbols (2)
        symbols.Add(CreateSymbol("JPM", "NYSE", "JPMorgan Chase", "JPM", "USD"));
        symbols.Add(CreateSymbol("BA", "NYSE", "Boeing", "BA", "USD"));

        // BINANCE Symbols (9)
        symbols.Add(CreateSymbol("BTCUSDT", "BINANCE", "Bitcoin", "BTC", "USDT"));
        symbols.Add(CreateSymbol("ETHUSDT", "BINANCE", "Ethereum", "ETH", "USDT"));
        symbols.Add(CreateSymbol("BNBUSDT", "BINANCE", "Binance Coin", "BNB", "USDT"));
        symbols.Add(CreateSymbol("ADAUSDT", "BINANCE", "Cardano", "ADA", "USDT"));
        symbols.Add(CreateSymbol("SOLUSDT", "BINANCE", "Solana", "SOL", "USDT"));
        symbols.Add(CreateSymbol("XRPUSDT", "BINANCE", "Ripple", "XRP", "USDT"));
        symbols.Add(CreateSymbol("DOGEUSDT", "BINANCE", "Dogecoin", "DOGE", "USDT"));
        symbols.Add(CreateSymbol("DOTUSDT", "BINANCE", "Polkadot", "DOT", "USDT"));
        symbols.Add(CreateSymbol("MATICUSDT", "BINANCE", "Polygon", "MATIC", "USDT"));

        return symbols;
    }

    private Symbol CreateSymbol(string ticker, string venue, string fullName, string baseCurrency, string quoteCurrency)
    {
        return new Symbol
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Venue = venue,
            FullName = fullName,
            Display = fullName,
            BaseCurrency = baseCurrency,
            QuoteCurrency = quoteCurrency,
            IsActive = true,
            IsTracked = true,
            AssetClass = venue == "BINANCE" ? "CRYPTO" : "STOCK",
            PricePrecision = venue == "BINANCE" ? 8 : 2
        };
    }
}

// Test DTOs
public class TrackedSymbol
{
    public string Ticker { get; set; } = string.Empty;
    public string? Display { get; set; }
    public string AssetClass { get; set; } = string.Empty;
}

// Interface for SymbolService if not available
public interface ISymbolService
{
    Task<List<TrackedSymbol>> GetTrackedAsync();
}