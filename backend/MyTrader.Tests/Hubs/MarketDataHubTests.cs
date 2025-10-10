using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Api.Hubs;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MyTrader.Services.Market;
using MyTrader.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Hubs;

public class MarketDataHubTests : TestBase
{
    private readonly Mock<ILogger<MarketDataHub>> _mockLogger;
    private readonly Mock<IBinanceWebSocketService> _mockBinanceService;
    private readonly Mock<HubCallerContext> _mockHubContext;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockCallerProxy;
    private readonly Mock<IClientProxy> _mockAllProxy;
    private readonly MarketDataHub _hub;

    public MarketDataHubTests()
    {
        _mockLogger = MockServiceHelper.CreateMockLogger<MarketDataHub>();
        _mockBinanceService = new Mock<IBinanceWebSocketService>();
        _mockHubContext = new Mock<HubCallerContext>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockCallerProxy = new Mock<IClientProxy>();
        _mockAllProxy = new Mock<IClientProxy>();

        // Setup hub context
        _mockHubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        _mockHubContext.Setup(x => x.User).Returns((System.Security.Claims.ClaimsPrincipal)null!);

        // Setup clients
        _mockClients.Setup(x => x.Caller).Returns(_mockCallerProxy.Object);
        _mockClients.Setup(x => x.All).Returns(_mockAllProxy.Object);

        _hub = new MarketDataHub(_mockLogger.Object, _mockBinanceService.Object);

        // Set the hub context and clients using reflection since they're protected
        typeof(Hub).GetProperty("Context")!.SetValue(_hub, _mockHubContext.Object);
        typeof(Hub).GetProperty("Clients")!.SetValue(_hub, _mockClients.Object);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldSendConnectionStatusAndHeartbeat()
    {
        // Arrange
        var connectionStatusSent = false;
        var heartbeatSent = false;
        object? connectionStatusData = null;
        object? heartbeatData = null;

        _mockCallerProxy.Setup(x => x.SendCoreAsync("ConnectionStatus", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                connectionStatusSent = true;
                connectionStatusData = args[0];
            })
            .Returns(Task.CompletedTask);

        _mockCallerProxy.Setup(x => x.SendCoreAsync("Heartbeat", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                heartbeatSent = true;
                heartbeatData = args[0];
            })
            .Returns(Task.CompletedTask);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        connectionStatusSent.Should().BeTrue();
        heartbeatSent.Should().BeTrue();
        connectionStatusData.Should().NotBeNull();
        heartbeatData.Should().NotBeNull();

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client connected to MarketDataHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldLogDisconnection()
    {
        // Arrange
        var exception = new Exception("Test disconnect reason");

        // Act
        await _hub.OnDisconnectedAsync(exception);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected from MarketDataHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SubscribeToSymbol_ValidSymbol_ShouldAddToGroup()
    {
        // Arrange
        var symbol = "AAPL";
        var groupName = $"symbol_{symbol}";

        _mockCallerProxy.Setup(x => x.SendCoreAsync("SubscriptionConfirmed", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock the Groups.AddToGroupAsync method
        var mockGroups = new Mock<IGroupManager>();
        mockGroups.Setup(x => x.AddToGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        typeof(Hub).GetProperty("Groups")!.SetValue(_hub, mockGroups.Object);

        // Act
        await _hub.SubscribeToSymbol(symbol);

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()), Times.Once);

        _mockCallerProxy.Verify(
            x => x.SendCoreAsync("SubscriptionConfirmed", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubscribeToSymbol_InvalidSymbol_ShouldSendError()
    {
        // Arrange
        var symbol = ""; // Invalid empty symbol

        _mockCallerProxy.Setup(x => x.SendCoreAsync("SubscriptionError", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToSymbol(symbol);

        // Assert
        _mockCallerProxy.Verify(
            x => x.SendCoreAsync("SubscriptionError", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid symbol subscription attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromSymbol_ValidSymbol_ShouldRemoveFromGroup()
    {
        // Arrange
        var symbol = "AAPL";
        var groupName = $"symbol_{symbol}";

        _mockCallerProxy.Setup(x => x.SendCoreAsync("UnsubscriptionConfirmed", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock the Groups.RemoveFromGroupAsync method
        var mockGroups = new Mock<IGroupManager>();
        mockGroups.Setup(x => x.RemoveFromGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        typeof(Hub).GetProperty("Groups")!.SetValue(_hub, mockGroups.Object);

        // Act
        await _hub.UnsubscribeFromSymbol(symbol);

        // Assert
        mockGroups.Verify(x => x.RemoveFromGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()), Times.Once);

        _mockCallerProxy.Verify(
            x => x.SendCoreAsync("UnsubscriptionConfirmed", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubscribeToAssetClass_ValidAssetClass_ShouldAddToGroup()
    {
        // Arrange
        var assetClass = "CRYPTO";
        var groupName = $"assetclass_{assetClass.ToLower()}";

        _mockCallerProxy.Setup(x => x.SendCoreAsync("AssetClassSubscriptionConfirmed", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock the Groups.AddToGroupAsync method
        var mockGroups = new Mock<IGroupManager>();
        mockGroups.Setup(x => x.AddToGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        typeof(Hub).GetProperty("Groups")!.SetValue(_hub, mockGroups.Object);

        // Act
        await _hub.SubscribeToAssetClass(assetClass);

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()), Times.Once);

        _mockCallerProxy.Verify(
            x => x.SendCoreAsync("AssetClassSubscriptionConfirmed", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastMarketData_ToAllClients_ShouldSendToAll()
    {
        // Arrange
        var marketData = new MarketData
        {
            Id = 1,
            SymbolId = 1,
            Price = 150.25m,
            Volume = 1000000,
            Timestamp = DateTime.UtcNow,
            Change = 2.50m,
            ChangePercent = 1.69m
        };

        _mockAllProxy.Setup(x => x.SendCoreAsync("MarketDataUpdate", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.BroadcastMarketData(marketData);

        // Assert
        _mockAllProxy.Verify(
            x => x.SendCoreAsync("MarketDataUpdate",
                It.Is<object[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastToSymbolGroup_SpecificSymbol_ShouldSendToGroup()
    {
        // Arrange
        var symbol = "AAPL";
        var marketData = new MarketData
        {
            Id = 1,
            SymbolId = 1,
            Price = 150.25m,
            Volume = 1000000,
            Timestamp = DateTime.UtcNow
        };

        var mockGroupProxy = new Mock<IClientProxy>();
        mockGroupProxy.Setup(x => x.SendCoreAsync("SymbolUpdate", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClients.Setup(x => x.Group($"symbol_{symbol}")).Returns(mockGroupProxy.Object);

        // Act
        await _hub.BroadcastToSymbolGroup(symbol, marketData);

        // Assert
        mockGroupProxy.Verify(
            x => x.SendCoreAsync("SymbolUpdate",
                It.Is<object[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("AAPL")]
    [InlineData("BTC-USD")]
    [InlineData("ETHUSDT")]
    [InlineData("EURUSD")]
    public async Task SubscribeToSymbol_DifferentSymbolTypes_ShouldWork(string symbol)
    {
        // Arrange
        var groupName = $"symbol_{symbol}";

        _mockCallerProxy.Setup(x => x.SendCoreAsync("SubscriptionConfirmed", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockGroups = new Mock<IGroupManager>();
        mockGroups.Setup(x => x.AddToGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        typeof(Hub).GetProperty("Groups")!.SetValue(_hub, mockGroups.Object);

        // Act
        await _hub.SubscribeToSymbol(symbol);

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync("test-connection-id", groupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_SendError_ShouldLogError()
    {
        // Arrange
        _mockCallerProxy.Setup(x => x.SendCoreAsync("ConnectionStatus", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SignalR send failed"));

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error sending connection status")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConnectionStatus_ShouldReturnConnectionInfo()
    {
        // Arrange
        _mockCallerProxy.Setup(x => x.SendCoreAsync("ConnectionStatus", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.GetConnectionStatus();

        // Assert
        _mockCallerProxy.Verify(
            x => x.SendCoreAsync("ConnectionStatus", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public override void Dispose()
    {
        // Cleanup if needed
        base.Dispose();
    }
}

// Extension methods for the hub - these would be in the actual hub implementation
public static class MarketDataHubExtensions
{
    public static async Task SubscribeToSymbol(this MarketDataHub hub, string symbol)
    {
        // This method would be implemented in the actual hub
        await Task.CompletedTask;
    }

    public static async Task UnsubscribeFromSymbol(this MarketDataHub hub, string symbol)
    {
        // This method would be implemented in the actual hub
        await Task.CompletedTask;
    }

    public static async Task SubscribeToAssetClass(this MarketDataHub hub, string assetClass)
    {
        // This method would be implemented in the actual hub
        await Task.CompletedTask;
    }

    public static async Task BroadcastMarketData(this MarketDataHub hub, MarketData marketData)
    {
        // This method would be implemented in the actual hub
        await Task.CompletedTask;
    }

    public static async Task BroadcastToSymbolGroup(this MarketDataHub hub, string symbol, MarketData marketData)
    {
        // This method would be implemented in the actual hub
        await Task.CompletedTask;
    }

    public static async Task GetConnectionStatus(this MarketDataHub hub)
    {
        // This method would be implemented in the actual hub
        await Task.CompletedTask;
    }
}