using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Api.Hubs;
using MyTrader.Api.Services;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MyTrader.Services.Market;
using MyTrader.Tests.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Services;

public class MultiAssetDataBroadcastServiceTests : TestBase
{
    private readonly Mock<IBinanceWebSocketService> _mockBinanceService;
    private readonly Mock<IHubContext<MarketDataHub>> _mockHubContext;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<MultiAssetDataBroadcastService>> _mockLogger;
    private readonly Mock<IHubCallerClients> _mockHubClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly MultiAssetDataBroadcastService _service;

    public MultiAssetDataBroadcastServiceTests()
    {
        _mockBinanceService = new Mock<IBinanceWebSocketService>();
        _mockHubContext = new Mock<IHubContext<MarketDataHub>>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = MockServiceHelper.CreateMockLogger<MultiAssetDataBroadcastService>();
        _mockHubClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();

        // Setup hub context
        _mockHubContext.Setup(x => x.Clients).Returns(_mockHubClients.Object);
        _mockHubClients.Setup(x => x.All).Returns(_mockClientProxy.Object);

        // Setup service scope
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        _service = new MultiAssetDataBroadcastService(
            _mockBinanceService.Object,
            _mockHubContext.Object,
            _mockScopeFactory.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldSubscribeToMarketDataUpdates()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _service.StartAsync(cancellationToken);

        // Assert
        _mockBinanceService.Verify(x => x.StartAsync(cancellationToken), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MultiAssetDataBroadcastService started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldUnsubscribeFromMarketDataUpdates()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Start first
        await _service.StartAsync(cancellationToken);

        // Act
        await _service.StopAsync(cancellationToken);

        // Assert
        _mockBinanceService.Verify(x => x.StopAsync(cancellationToken), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MultiAssetDataBroadcastService stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MarketDataReceived_ShouldBroadcastToSignalRClients()
    {
        // Arrange
        var testMarketData = new MarketData
        {
            Id = 1,
            SymbolId = 1,
            Price = 45000.50m,
            Volume = 1000000,
            Timestamp = DateTime.UtcNow,
            Change = 1000.25m,
            ChangePercent = 2.27m,
            DayHigh = 46000.00m,
            DayLow = 44500.00m,
            Open = 44800.00m,
            PreviousClose = 44000.25m
        };

        var cancellationToken = CancellationToken.None;

        // Mock the Binance service to raise the OnMarketDataReceived event
        _mockBinanceService.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Callback(() => {
                // Simulate receiving market data after a short delay
                Task.Run(async () =>
                {
                    await Task.Delay(100); // Small delay to ensure subscription is set up
                    _mockBinanceService.Raise(x => x.OnMarketDataReceived += null, testMarketData);
                });
            });

        // Setup SignalR mock to capture the broadcast
        object? broadcastedData = null;
        _mockClientProxy.Setup(x => x.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                broadcastedData = args[0];
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(cancellationToken);

        // Wait a bit for the event to be processed
        await Task.Delay(200);

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "MarketDataUpdate",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        broadcastedData.Should().NotBeNull();
    }

    [Fact]
    public async Task MarketDataReceived_RapidUpdates_ShouldThrottleBroadcasts()
    {
        // Arrange
        var testMarketData = new MarketData
        {
            Id = 1,
            SymbolId = 1,
            Price = 45000.50m,
            Volume = 1000000,
            Timestamp = DateTime.UtcNow,
            Change = 1000.25m,
            ChangePercent = 2.27m,
            DayHigh = 46000.00m,
            DayLow = 44500.00m,
            Open = 44800.00m,
            PreviousClose = 44000.25m
        };

        var cancellationToken = CancellationToken.None;

        // Setup SignalR mock to count broadcasts
        int broadcastCount = 0;
        _mockClientProxy.Setup(x => x.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref broadcastCount))
            .Returns(Task.CompletedTask);

        // Mock the Binance service to raise multiple rapid events
        _mockBinanceService.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Callback(() => {
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    // Send rapid updates (faster than throttle limit)
                    for (int i = 0; i < 10; i++)
                    {
                        _mockBinanceService.Raise(x => x.OnMarketDataReceived += null, testMarketData);
                        await Task.Delay(1); // Very fast updates
                    }
                });
            });

        // Act
        await _service.StartAsync(cancellationToken);
        await Task.Delay(500); // Wait for processing

        // Assert
        // Should have fewer broadcasts than events due to throttling
        broadcastCount.Should().BeLessThan(10);
    }

    [Fact]
    public async Task MarketDataReceived_SignalRException_ShouldLogErrorAndContinue()
    {
        // Arrange
        var testMarketData = new MarketData
        {
            Id = 1,
            SymbolId = 1,
            Price = 45000.50m,
            Volume = 1000000,
            Timestamp = DateTime.UtcNow
        };

        var cancellationToken = CancellationToken.None;

        // Setup SignalR mock to throw exception
        _mockClientProxy.Setup(x => x.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SignalR connection failed"));

        // Mock the Binance service to raise an event
        _mockBinanceService.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Callback(() => {
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    _mockBinanceService.Raise(x => x.OnMarketDataReceived += null, testMarketData);
                });
            });

        // Act
        await _service.StartAsync(cancellationToken);
        await Task.Delay(300);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error broadcasting market data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var service = new MultiAssetDataBroadcastService(
            _mockBinanceService.Object,
            _mockHubContext.Object,
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Act
        service.Dispose();

        // Assert
        // No exception should be thrown
        // Service should handle disposal gracefully
    }

    [Fact]
    public async Task StartAsync_BinanceServiceException_ShouldLogErrorAndThrow()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        _mockBinanceService.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Binance connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _service.StartAsync(cancellationToken);
        });

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error starting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task MultipleStartStop_ShouldHandleGracefully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _service.StartAsync(cancellationToken);
        await _service.StartAsync(cancellationToken); // Second start should be handled gracefully
        await _service.StopAsync(cancellationToken);
        await _service.StopAsync(cancellationToken); // Second stop should be handled gracefully

        // Assert
        // No exceptions should be thrown
        _mockBinanceService.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mockBinanceService.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    public override void Dispose()
    {
        _service?.Dispose();
        base.Dispose();
    }
}

// Test interfaces and classes
public interface IBinanceWebSocketService
{
    event Action<MarketData> OnMarketDataReceived;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class MarketDataHub : Hub
{
    // Hub implementation
}