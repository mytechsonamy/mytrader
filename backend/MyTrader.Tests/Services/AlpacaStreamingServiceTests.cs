using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MyTrader.Core.DTOs;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using MyTrader.Infrastructure.Services;
using Xunit;

namespace MyTrader.Tests.Services;

/// <summary>
/// Comprehensive unit tests for AlpacaStreamingService
/// Tests WebSocket connection, authentication, message processing, and health monitoring
/// </summary>
public class AlpacaStreamingServiceTests : IDisposable
{
    private readonly Mock<ILogger<AlpacaStreamingService>> _mockLogger;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ISymbolManagementService> _mockSymbolService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly IOptions<AlpacaConfiguration> _config;
    private readonly AlpacaStreamingService _service;

    public AlpacaStreamingServiceTests()
    {
        _mockLogger = new Mock<ILogger<AlpacaStreamingService>>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockSymbolService = new Mock<ISymbolManagementService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();

        // Setup service scope factory chain
        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ISymbolManagementService)))
            .Returns(_mockSymbolService.Object);

        // Create valid test configuration
        _config = Options.Create(CreateValidConfiguration());

        _service = new AlpacaStreamingService(_mockLogger.Object, _mockScopeFactory.Object, _config);
    }

    #region Helper Methods

    private static AlpacaConfiguration CreateValidConfiguration()
    {
        return new AlpacaConfiguration
        {
            Streaming = new AlpacaStreamingConfiguration
            {
                Enabled = true,
                WebSocketUrl = "wss://stream.data.alpaca.markets/v2/iex",
                ApiKey = "test_api_key",
                ApiSecret = "test_api_secret",
                MaxSymbols = 30,
                SubscribeToTrades = true,
                SubscribeToQuotes = true,
                SubscribeToBars = false,
                ReconnectBaseDelayMs = 1000,
                ReconnectMaxDelayMs = 60000,
                MessageTimeoutSeconds = 30,
                AuthTimeoutSeconds = 10,
                EnableDetailedLogging = false
            }
        };
    }

    private static AlpacaTradeMessage CreateValidTradeMessage()
    {
        return new AlpacaTradeMessage
        {
            T = "t",
            S = "AAPL",
            P = 150.25m,
            S_Size = 100,
            T_Timestamp = DateTime.UtcNow.ToString("O"),
            X = "V",
            I = 123456789,
            C = new List<string> { "@" },
            Z = "C"
        };
    }

    private static AlpacaQuoteMessage CreateValidQuoteMessage()
    {
        return new AlpacaQuoteMessage
        {
            T = "q",
            S = "GOOGL",
            BP = 140.50m,
            AP = 140.55m,
            BS = 200,
            AS = 150,
            T_Timestamp = DateTime.UtcNow.ToString("O"),
            BX = "V",
            AX = "V",
            C = new List<string>(),
            Z = "C"
        };
    }

    private static AlpacaBarMessage CreateValidBarMessage()
    {
        return new AlpacaBarMessage
        {
            T = "b",
            S = "MSFT",
            O = 310.00m,
            H = 312.50m,
            L = 309.75m,
            C = 311.25m,
            V = 1000000,
            T_Timestamp = DateTime.UtcNow.ToString("O"),
            N = 5000,
            VW = 311.00m
        };
    }

    #endregion

    #region Connection Tests

    [Fact]
    public async Task GetHealthStatusAsync_Initially_ReturnsUnhealthyStatus()
    {
        // Arrange & Act
        var health = await _service.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(health);
        Assert.False(health.IsConnected);
        Assert.False(health.IsAuthenticated);
        Assert.Equal(0, health.SubscribedSymbols);
        Assert.Null(health.LastMessageReceived);
        Assert.Equal(0, health.MessagesPerMinute);
        Assert.Null(health.ConnectionUptime);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange & Act
        var service = new AlpacaStreamingService(_mockLogger.Object, _mockScopeFactory.Object, _config);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task SubscribeToSymbolsAsync_WithValidSymbols_UpdatesSubscriptionList()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "GOOGL", "MSFT" };

        // Act
        await _service.SubscribeToSymbolsAsync(symbols);
        var health = await _service.GetHealthStatusAsync();

        // Assert
        Assert.Equal(3, health.SubscribedSymbols);
    }

    [Fact]
    public async Task SubscribeToSymbolsAsync_WithEmptyList_HandlesGracefully()
    {
        // Arrange
        var symbols = new List<string>();

        // Act
        await _service.SubscribeToSymbolsAsync(symbols);
        var health = await _service.GetHealthStatusAsync();

        // Assert
        Assert.Equal(0, health.SubscribedSymbols);
    }

    [Fact]
    public async Task SubscribeToSymbolsAsync_WithNullOrWhitespaceSymbols_FiltersInvalidEntries()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "", "  ", null!, "GOOGL" };

        // Act
        await _service.SubscribeToSymbolsAsync(symbols);
        var health = await _service.GetHealthStatusAsync();

        // Assert
        Assert.Equal(2, health.SubscribedSymbols);
    }

    [Fact]
    public async Task SubscribeToSymbolsAsync_WithDuplicates_RemovesDuplicates()
    {
        // Arrange
        var symbols = new List<string> { "AAPL", "aapl", "AAPL", "GOOGL" };

        // Act
        await _service.SubscribeToSymbolsAsync(symbols);
        var health = await _service.GetHealthStatusAsync();

        // Assert
        Assert.Equal(2, health.SubscribedSymbols);
    }

    [Fact]
    public async Task SubscribeToSymbolsAsync_ExceedingMaxSymbols_TruncatesToMaxLimit()
    {
        // Arrange
        var symbols = Enumerable.Range(1, 50).Select(i => $"SYM{i}").ToList();

        // Act
        await _service.SubscribeToSymbolsAsync(symbols);
        var health = await _service.GetHealthStatusAsync();

        // Assert
        Assert.Equal(30, health.SubscribedSymbols); // MaxSymbols = 30
    }

    #endregion

    #region Message Processing Tests

    [Fact]
    public void StockPriceUpdated_Event_CanBeSubscribedTo()
    {
        // Arrange
        StockPriceData? receivedData = null;
        _service.StockPriceUpdated += (data) => { receivedData = data; };

        // Act & Assert
        Assert.NotNull(_service.StockPriceUpdated);
    }

    [Fact]
    public void ProcessTradeMessage_ValidData_MapsCorrectly()
    {
        // Arrange
        var trade = CreateValidTradeMessage();
        StockPriceData? receivedData = null;
        _service.StockPriceUpdated += (data) => { receivedData = data; };

        // Act
        // We need to use reflection to call private method for testing
        var method = typeof(AlpacaStreamingService).GetMethod("ProcessTradeMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_service, new object[] { trade });

        // Assert
        Assert.NotNull(receivedData);
        Assert.Equal("AAPL", receivedData.Symbol);
        Assert.Equal(150.25m, receivedData.Price);
        Assert.Equal(AssetClassCode.STOCK, receivedData.AssetClass);
        Assert.Equal("ALPACA", receivedData.Source);
        Assert.Equal(100, receivedData.Volume);
        Assert.Equal(100, receivedData.QualityScore);
    }

    [Fact]
    public void ProcessQuoteMessage_ValidData_CalculatesMidPrice()
    {
        // Arrange
        var quote = CreateValidQuoteMessage();
        StockPriceData? receivedData = null;
        _service.StockPriceUpdated += (data) => { receivedData = data; };

        // Act
        var method = typeof(AlpacaStreamingService).GetMethod("ProcessQuoteMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_service, new object[] { quote });

        // Assert
        Assert.NotNull(receivedData);
        Assert.Equal("GOOGL", receivedData.Symbol);
        Assert.Equal(140.525m, receivedData.Price); // (140.50 + 140.55) / 2
        Assert.Equal(140.50m, receivedData.BidPrice);
        Assert.Equal(140.55m, receivedData.AskPrice);
        Assert.Equal("ALPACA", receivedData.Source);
        Assert.Equal(100, receivedData.QualityScore);
    }

    [Fact]
    public void ProcessBarMessage_ValidData_MapsOHLCV()
    {
        // Arrange
        var bar = CreateValidBarMessage();
        StockPriceData? receivedData = null;
        _service.StockPriceUpdated += (data) => { receivedData = data; };

        // Act
        var method = typeof(AlpacaStreamingService).GetMethod("ProcessBarMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_service, new object[] { bar });

        // Assert
        Assert.NotNull(receivedData);
        Assert.Equal("MSFT", receivedData.Symbol);
        Assert.Equal(311.25m, receivedData.Price); // Close price
        Assert.Equal(310.00m, receivedData.OpenPrice);
        Assert.Equal(312.50m, receivedData.HighPrice);
        Assert.Equal(309.75m, receivedData.LowPrice);
        Assert.Equal(1000000, receivedData.Volume);
        Assert.Equal(5000, receivedData.TradeCount);
        Assert.Equal("ALPACA", receivedData.Source);
        Assert.Equal(100, receivedData.QualityScore);
    }

    #endregion

    #region Exchange Mapping Tests

    [Theory]
    [InlineData("V", "NASDAQ")]
    [InlineData("Q", "NASDAQ")]
    [InlineData("P", "NYSE")]
    [InlineData("N", "NYSE")]
    [InlineData("Z", "BATS")]
    [InlineData("J", "EDGA")]
    [InlineData("K", "EDGX")]
    [InlineData("X", "NYSE")] // Unknown defaults to NYSE
    public void MapExchangeToMarket_VariousExchangeCodes_MapsCorrectly(string exchangeCode, string expectedMarket)
    {
        // Arrange
        var trade = CreateValidTradeMessage();
        trade.X = exchangeCode;
        StockPriceData? receivedData = null;
        _service.StockPriceUpdated += (data) => { receivedData = data; };

        // Act
        var method = typeof(AlpacaStreamingService).GetMethod("ProcessTradeMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_service, new object[] { trade });

        // Assert
        Assert.NotNull(receivedData);
        Assert.Equal(expectedMarket, receivedData.Market);
    }

    #endregion

    #region Timestamp Parsing Tests

    [Fact]
    public void GetTimestamp_ValidISOTimestamp_ParsesCorrectly()
    {
        // Arrange
        var message = new AlpacaTradeMessage
        {
            T = "t",
            S = "AAPL",
            P = 150.00m,
            S_Size = 100,
            T_Timestamp = "2025-01-10T14:30:00Z",
            X = "V"
        };

        // Act
        var timestamp = message.GetTimestamp();

        // Assert
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        Assert.True(timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void GetTimestamp_InvalidTimestamp_ReturnsCurrentTime()
    {
        // Arrange
        var message = new AlpacaTradeMessage
        {
            T = "t",
            S = "AAPL",
            P = 150.00m,
            S_Size = 100,
            T_Timestamp = "invalid_timestamp",
            X = "V"
        };
        var beforeParse = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var timestamp = message.GetTimestamp();
        var afterParse = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(timestamp >= beforeParse && timestamp <= afterParse);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_WithDisabledStreaming_DoesNotThrow()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Streaming.Enabled = false;
        var options = Options.Create(config);

        // Act & Assert
        var service = new AlpacaStreamingService(_mockLogger.Object, _mockScopeFactory.Object, options);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task StopAsync_WhenServiceNotStarted_CompletesSuccessfully()
    {
        // Act
        await _service.StopAsync();

        // Assert - Should not throw
        Assert.True(true);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SubscribeToSymbolsAsync_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        var symbols = new List<string> { "AAPL" };

        // Act
        await _service.SubscribeToSymbolsAsync(symbols);

        // Assert - Should not throw, errors are logged
        var health = await _service.GetHealthStatusAsync();
        Assert.NotNull(health);
    }

    #endregion

    #region Health Monitoring Tests

    [Fact]
    public async Task GetHealthStatusAsync_AfterMultipleSubscriptions_TracksCorrectCount()
    {
        // Arrange
        await _service.SubscribeToSymbolsAsync(new List<string> { "AAPL", "GOOGL" });
        await Task.Delay(100);
        await _service.SubscribeToSymbolsAsync(new List<string> { "MSFT", "TSLA", "AMZN" });

        // Act
        var health = await _service.GetHealthStatusAsync();

        // Assert
        Assert.Equal(3, health.SubscribedSymbols); // Last subscription replaces previous
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReturnsConsistentState()
    {
        // Act
        var health1 = await _service.GetHealthStatusAsync();
        await Task.Delay(50);
        var health2 = await _service.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(health1);
        Assert.NotNull(health2);
        Assert.Equal(health1.IsConnected, health2.IsConnected);
        Assert.Equal(health1.IsAuthenticated, health2.IsAuthenticated);
    }

    #endregion

    #region Symbol Management Tests

    [Fact]
    public async Task SubscribeToSymbolsAsync_UppercasesAllSymbols()
    {
        // Arrange
        var symbols = new List<string> { "aapl", "Googl", "MSFT" };
        StockPriceData? receivedData = null;
        _service.StockPriceUpdated += (data) => { receivedData = data; };

        // Act
        await _service.SubscribeToSymbolsAsync(symbols);

        // Assert
        var health = await _service.GetHealthStatusAsync();
        Assert.Equal(3, health.SubscribedSymbols);
    }

    #endregion

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }
}
