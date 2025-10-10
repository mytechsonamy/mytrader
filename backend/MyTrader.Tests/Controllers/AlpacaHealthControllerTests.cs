using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Api.Controllers;
using MyTrader.Core.Services;
using MyTrader.Infrastructure.Services;
using Xunit;

namespace MyTrader.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for AlpacaHealthController
/// Tests health monitoring endpoints, failover operations, and error handling
/// </summary>
public class AlpacaHealthControllerTests
{
    private readonly Mock<IAlpacaStreamingService> _mockAlpacaService;
    private readonly Mock<IDataSourceRouter> _mockRouter;
    private readonly Mock<ILogger<AlpacaHealthController>> _mockLogger;
    private readonly AlpacaHealthController _controller;

    public AlpacaHealthControllerTests()
    {
        _mockAlpacaService = new Mock<IAlpacaStreamingService>();
        _mockRouter = new Mock<IDataSourceRouter>();
        _mockLogger = new Mock<ILogger<AlpacaHealthController>>();

        _controller = new AlpacaHealthController(
            _mockAlpacaService.Object,
            _mockRouter.Object,
            _mockLogger.Object);
    }

    #region Helper Methods

    private static AlpacaConnectionHealth CreateHealthyConnectionHealth()
    {
        return new AlpacaConnectionHealth
        {
            IsConnected = true,
            IsAuthenticated = true,
            SubscribedSymbols = 10,
            LastMessageReceived = DateTime.UtcNow.AddSeconds(-5),
            MessagesPerMinute = 120,
            ConnectionUptime = TimeSpan.FromHours(2),
            ConsecutiveFailures = 0,
            LastError = null,
            State = WebSocketState.Open
        };
    }

    private static AlpacaConnectionHealth CreateUnhealthyConnectionHealth()
    {
        return new AlpacaConnectionHealth
        {
            IsConnected = false,
            IsAuthenticated = false,
            SubscribedSymbols = 10,
            LastMessageReceived = DateTime.UtcNow.AddMinutes(-5),
            MessagesPerMinute = 0,
            ConnectionUptime = null,
            ConsecutiveFailures = 3,
            LastError = "WebSocket connection lost",
            State = WebSocketState.Closed
        };
    }

    private static DataSourceRouterStatus CreateHealthyRouterStatus()
    {
        return new DataSourceRouterStatus
        {
            CurrentState = RoutingState.PRIMARY_ACTIVE,
            StateChangedAt = DateTime.UtcNow.AddMinutes(-30),
            StateChangeReason = "Alpaca connected and receiving data",
            AlpacaStatus = new DataProviderStatus
            {
                Name = "Alpaca",
                IsHealthy = true,
                LastMessageReceivedAt = DateTime.UtcNow.AddSeconds(-2),
                ConsecutiveFailures = 0,
                MessagesReceivedCount = 5000,
                LastError = null
            },
            YahooStatus = new DataProviderStatus
            {
                Name = "Yahoo",
                IsHealthy = true,
                LastMessageReceivedAt = DateTime.UtcNow.AddSeconds(-10),
                ConsecutiveFailures = 0,
                MessagesReceivedCount = 1000,
                LastError = null
            },
            FallbackActivationCount = 0,
            LastFallbackActivation = null,
            TotalFallbackDuration = TimeSpan.Zero,
            UptimePercent = 100.0
        };
    }

    private static DataSourceRouterStatus CreateDegradedRouterStatus()
    {
        return new DataSourceRouterStatus
        {
            CurrentState = RoutingState.FALLBACK_ACTIVE,
            StateChangedAt = DateTime.UtcNow.AddMinutes(-5),
            StateChangeReason = "Alpaca unhealthy after multiple failures",
            AlpacaStatus = new DataProviderStatus
            {
                Name = "Alpaca",
                IsHealthy = false,
                LastMessageReceivedAt = DateTime.UtcNow.AddMinutes(-10),
                ConsecutiveFailures = 3,
                MessagesReceivedCount = 4500,
                LastError = "Connection timeout"
            },
            YahooStatus = new DataProviderStatus
            {
                Name = "Yahoo",
                IsHealthy = true,
                LastMessageReceivedAt = DateTime.UtcNow.AddSeconds(-5),
                ConsecutiveFailures = 0,
                MessagesReceivedCount = 1200,
                LastError = null
            },
            FallbackActivationCount = 2,
            LastFallbackActivation = DateTime.UtcNow.AddMinutes(-5),
            TotalFallbackDuration = TimeSpan.FromMinutes(15),
            UptimePercent = 92.5
        };
    }

    #endregion

    #region GetAlpacaHealth Tests

    [Fact]
    public async Task GetAlpacaHealth_WhenHealthy_ReturnsOkWithHealthyStatus()
    {
        // Arrange
        var healthStatus = CreateHealthyConnectionHealth();
        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync())
            .ReturnsAsync(healthStatus);

        // Act
        var result = await _controller.GetAlpacaHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var response = okResult.Value;
        var statusProp = response.GetType().GetProperty("status")?.GetValue(response) as string;
        Assert.Equal("Healthy", statusProp);

        _mockAlpacaService.Verify(x => x.GetHealthStatusAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAlpacaHealth_WhenUnhealthy_ReturnsOkWithUnhealthyStatus()
    {
        // Arrange
        var healthStatus = CreateUnhealthyConnectionHealth();
        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync())
            .ReturnsAsync(healthStatus);

        // Act
        var result = await _controller.GetAlpacaHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var statusProp = response.GetType().GetProperty("status")?.GetValue(response) as string;
        Assert.Equal("Unhealthy", statusProp);
    }

    [Fact]
    public async Task GetAlpacaHealth_ReturnsDetailedMetrics()
    {
        // Arrange
        var healthStatus = CreateHealthyConnectionHealth();
        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync())
            .ReturnsAsync(healthStatus);

        // Act
        var result = await _controller.GetAlpacaHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;

        var alpacaStatus = response.GetType().GetProperty("alpacaStatus")?.GetValue(response);
        Assert.NotNull(alpacaStatus);

        var connected = (bool)alpacaStatus!.GetType().GetProperty("connected")!.GetValue(alpacaStatus)!;
        var authenticated = (bool)alpacaStatus.GetType().GetProperty("authenticated")!.GetValue(alpacaStatus)!;
        var subscribedSymbols = (int)alpacaStatus.GetType().GetProperty("subscribedSymbols")!.GetValue(alpacaStatus)!;

        Assert.True(connected);
        Assert.True(authenticated);
        Assert.Equal(10, subscribedSymbols);
    }

    [Fact]
    public async Task GetAlpacaHealth_WhenExceptionOccurs_Returns500Error()
    {
        // Arrange
        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync())
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act
        var result = await _controller.GetAlpacaHealth();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        var response = statusCodeResult.Value;
        var error = response?.GetType().GetProperty("error")?.GetValue(response) as string;
        Assert.Contains("Failed to retrieve", error);
    }

    #endregion

    #region GetDataSourceHealth Tests

    [Fact]
    public void GetDataSourceHealth_WithHealthyRouter_ReturnsHealthyStatus()
    {
        // Arrange
        var routerStatus = CreateHealthyRouterStatus();
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = _controller.GetDataSourceHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var status = response.GetType().GetProperty("status")?.GetValue(response) as string;
        Assert.Equal("Healthy", status);
    }

    [Fact]
    public void GetDataSourceHealth_WithFallbackActive_ReturnsDegradedStatus()
    {
        // Arrange
        var routerStatus = CreateDegradedRouterStatus();
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = _controller.GetDataSourceHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var status = response.GetType().GetProperty("status")?.GetValue(response) as string;
        Assert.Equal("Degraded", status);
    }

    [Fact]
    public void GetDataSourceHealth_WithBothUnavailable_ReturnsUnhealthyStatus()
    {
        // Arrange
        var routerStatus = CreateHealthyRouterStatus();
        routerStatus.CurrentState = RoutingState.BOTH_UNAVAILABLE;
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = _controller.GetDataSourceHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var status = response.GetType().GetProperty("status")?.GetValue(response) as string;
        Assert.Equal("Unhealthy", status);
    }

    [Fact]
    public void GetDataSourceHealth_ReturnsProviderMetrics()
    {
        // Arrange
        var routerStatus = CreateHealthyRouterStatus();
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = _controller.GetDataSourceHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;

        var alpacaStatus = response.GetType().GetProperty("alpacaStatus")?.GetValue(response);
        var yahooStatus = response.GetType().GetProperty("yahooStatus")?.GetValue(response);

        Assert.NotNull(alpacaStatus);
        Assert.NotNull(yahooStatus);

        var alpacaHealthy = (bool)alpacaStatus!.GetType().GetProperty("isHealthy")!.GetValue(alpacaStatus)!;
        var yahooHealthy = (bool)yahooStatus!.GetType().GetProperty("isHealthy")!.GetValue(yahooStatus)!;

        Assert.True(alpacaHealthy);
        Assert.True(yahooHealthy);
    }

    [Fact]
    public void GetDataSourceHealth_ReturnsFailoverMetrics()
    {
        // Arrange
        var routerStatus = CreateDegradedRouterStatus();
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = _controller.GetDataSourceHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;

        var fallbackCount = (int)response.GetType().GetProperty("fallbackCount")!.GetValue(response)!;
        var uptimePercent = (double)response.GetType().GetProperty("uptimePercent")!.GetValue(response)!;

        Assert.Equal(2, fallbackCount);
        Assert.Equal(92.5, uptimePercent);
    }

    [Fact]
    public void GetDataSourceHealth_WhenExceptionOccurs_Returns500Error()
    {
        // Arrange
        _mockRouter.Setup(x => x.GetStatus())
            .Throws(new Exception("Router error"));

        // Act
        var result = _controller.GetDataSourceHealth();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetStocksHealth Tests

    [Fact]
    public async Task GetStocksHealth_WithPrimaryActive_ReturnsHealthyStatus()
    {
        // Arrange
        var alpacaHealth = CreateHealthyConnectionHealth();
        var routerStatus = CreateHealthyRouterStatus();

        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync()).ReturnsAsync(alpacaHealth);
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = await _controller.GetStocksHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var status = response.GetType().GetProperty("status")?.GetValue(response) as string;
        var dataSource = response.GetType().GetProperty("currentDataSource")?.GetValue(response) as string;

        Assert.Equal("Healthy", status);
        Assert.Equal("Alpaca (Real-time)", dataSource);
    }

    [Fact]
    public async Task GetStocksHealth_WithFallbackActive_ReturnsDegradedStatus()
    {
        // Arrange
        var alpacaHealth = CreateUnhealthyConnectionHealth();
        var routerStatus = CreateDegradedRouterStatus();

        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync()).ReturnsAsync(alpacaHealth);
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = await _controller.GetStocksHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var status = response.GetType().GetProperty("status")?.GetValue(response) as string;
        var dataSource = response.GetType().GetProperty("currentDataSource")?.GetValue(response) as string;

        Assert.Equal("Degraded", status);
        Assert.Equal("Yahoo (Fallback)", dataSource);
    }

    [Fact]
    public async Task GetStocksHealth_ReturnsCombinedMetrics()
    {
        // Arrange
        var alpacaHealth = CreateHealthyConnectionHealth();
        var routerStatus = CreateHealthyRouterStatus();

        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync()).ReturnsAsync(alpacaHealth);
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = await _controller.GetStocksHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;

        var alpaca = response.GetType().GetProperty("alpaca")?.GetValue(response);
        var yahoo = response.GetType().GetProperty("yahoo")?.GetValue(response);
        var metrics = response.GetType().GetProperty("metrics")?.GetValue(response);

        Assert.NotNull(alpaca);
        Assert.NotNull(yahoo);
        Assert.NotNull(metrics);
    }

    [Fact]
    public async Task GetStocksHealth_WhenExceptionOccurs_Returns500Error()
    {
        // Arrange
        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetStocksHealth();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region ForceFailover Tests

    [Fact]
    public async Task ForceFailover_ExecutesSuccessfully_ReturnsOk()
    {
        // Arrange
        _mockRouter.Setup(x => x.ForceFailoverAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForceFailover();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var message = response.GetType().GetProperty("message")?.GetValue(response) as string;

        Assert.Contains("Failover", message);
        _mockRouter.Verify(x => x.ForceFailoverAsync(), Times.Once);
    }

    [Fact]
    public async Task ForceFailover_LogsWarning()
    {
        // Arrange
        _mockRouter.Setup(x => x.ForceFailoverAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _controller.ForceFailover();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Manual failover")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task ForceFailover_WhenExceptionOccurs_Returns500Error()
    {
        // Arrange
        _mockRouter.Setup(x => x.ForceFailoverAsync())
            .ThrowsAsync(new Exception("Failover failed"));

        // Act
        var result = await _controller.ForceFailover();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        var response = statusCodeResult.Value;
        var error = response?.GetType().GetProperty("error")?.GetValue(response) as string;
        Assert.Contains("Failed to force failover", error);
    }

    #endregion

    #region ForceReconnect Tests

    [Fact]
    public async Task ForceReconnect_ExecutesSuccessfully_ReturnsOk()
    {
        // Arrange
        _mockAlpacaService.Setup(x => x.ForceReconnectAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForceReconnect();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var message = response.GetType().GetProperty("message")?.GetValue(response) as string;

        Assert.Contains("reconnection initiated", message);
        _mockAlpacaService.Verify(x => x.ForceReconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task ForceReconnect_LogsWarning()
    {
        // Arrange
        _mockAlpacaService.Setup(x => x.ForceReconnectAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _controller.ForceReconnect();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Manual reconnection")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task ForceReconnect_WhenExceptionOccurs_Returns500Error()
    {
        // Arrange
        _mockAlpacaService.Setup(x => x.ForceReconnectAsync())
            .ThrowsAsync(new Exception("Reconnection failed"));

        // Act
        var result = await _controller.ForceReconnect();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task GetAlpacaHealth_IncludesTimestamp()
    {
        // Arrange
        var healthStatus = CreateHealthyConnectionHealth();
        _mockAlpacaService.Setup(x => x.GetHealthStatusAsync())
            .ReturnsAsync(healthStatus);

        var beforeCall = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = await _controller.GetAlpacaHealth();
        var afterCall = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var timestamp = (DateTime)response.GetType().GetProperty("timestamp")!.GetValue(response)!;

        Assert.True(timestamp >= beforeCall && timestamp <= afterCall);
    }

    [Fact]
    public void GetDataSourceHealth_IncludesTimestamp()
    {
        // Arrange
        var routerStatus = CreateHealthyRouterStatus();
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        var beforeCall = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = _controller.GetDataSourceHealth();
        var afterCall = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var timestamp = (DateTime)response.GetType().GetProperty("timestamp")!.GetValue(response)!;

        Assert.True(timestamp >= beforeCall && timestamp <= afterCall);
    }

    #endregion

    #region State Mapping Tests

    [Theory]
    [InlineData(RoutingState.PRIMARY_ACTIVE, "Healthy")]
    [InlineData(RoutingState.FALLBACK_ACTIVE, "Degraded")]
    [InlineData(RoutingState.BOTH_UNAVAILABLE, "Unhealthy")]
    [InlineData(RoutingState.STARTUP, "Unknown")]
    public void GetDataSourceHealth_MapsRoutingStatesToStatusCorrectly(RoutingState state, string expectedStatus)
    {
        // Arrange
        var routerStatus = CreateHealthyRouterStatus();
        routerStatus.CurrentState = state;
        _mockRouter.Setup(x => x.GetStatus()).Returns(routerStatus);

        // Act
        var result = _controller.GetDataSourceHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var status = response.GetType().GetProperty("status")?.GetValue(response) as string;

        Assert.Equal(expectedStatus, status);
    }

    #endregion
}
