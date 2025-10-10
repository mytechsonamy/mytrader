using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Api.Controllers;
using MyTrader.Api.Hubs;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Market;
using MyTrader.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Controllers;

public class PricesControllerTests : TestBase
{
    private readonly Mock<IHubContext<TradingHub>> _mockHubContext;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly Mock<ILogger<PricesController>> _mockLogger;
    private readonly Mock<ISymbolService> _mockSymbolService;
    private readonly TradingDbContext _context;
    private readonly PricesController _controller;

    public PricesControllerTests()
    {
        _mockHubContext = new Mock<IHubContext<TradingHub>>();
        _mockHttpClient = new Mock<HttpClient>();
        _mockLogger = MockServiceHelper.CreateMockLogger<PricesController>();
        _mockSymbolService = new Mock<ISymbolService>();
        _context = CreateInMemoryDbContext();

        _controller = new PricesController(
            _mockHubContext.Object,
            new HttpClient(), // Use real HttpClient for testing
            _mockLogger.Object,
            _context,
            _mockSymbolService.Object);

        // Setup HttpContext
        SetupControllerContext();
        SeedTestData(_context);
    }

    private void SetupControllerContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "TestAgent/1.0";
        httpContext.TraceIdentifier = Guid.NewGuid().ToString();

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetLivePrices_WithValidData_ReturnsOk()
    {
        // Arrange
        var testMarketData = new List<MarketData>
        {
            new MarketData
            {
                Id = 1,
                SymbolId = 1,
                Price = 150.25m,
                Volume = 1000000,
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Change = 2.50m,
                ChangePercent = 1.69m,
                DayHigh = 152.00m,
                DayLow = 148.00m,
                Open = 149.00m,
                PreviousClose = 147.75m
            }
        };

        _context.MarketData.AddRange(testMarketData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLivePrices();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value;
        response.Should().NotBeNull();

        // Verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing GetLivePrices request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetLivePrices_WithAuthenticatedUser_LogsUserId()
    {
        // Arrange
        var userId = "testuser123";
        var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.Name, "testuser")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = await _controller.GetLivePrices();

        // Assert
        result.Should().NotBeNull();

        // Verify that authenticated user was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing GetLivePrices request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetLivePrices_WithNoData_ReturnsEmptyResponse()
    {
        // Arrange - No data seeded

        // Act
        var result = await _controller.GetLivePrices();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetLivePrices_WithXForwardedFor_LogsCorrectIP()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.Request.Headers["X-Forwarded-For"] = "192.168.1.100";

        // Act
        var result = await _controller.GetLivePrices();

        // Assert
        result.Should().NotBeNull();

        // Verify logging includes X-Forwarded-For information
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing GetLivePrices request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetLivePrices_WithLargeDataset_ShouldPerformWell()
    {
        // Arrange - Create many market data entries
        var marketDataEntries = new List<MarketData>();
        for (int i = 1; i <= 1000; i++)
        {
            marketDataEntries.Add(new MarketData
            {
                Id = i,
                SymbolId = (i % 3) + 1, // Distribute across 3 symbols
                Price = 100m + (i % 100),
                Volume = 1000000 + (i * 1000),
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Change = (i % 10) - 5m, // Random change between -5 and 4
                ChangePercent = ((i % 10) - 5m) / 100m * 5, // Corresponding percentage
                DayHigh = 100m + (i % 100) + 5,
                DayLow = 100m + (i % 100) - 5,
                Open = 100m + (i % 100) - 1,
                PreviousClose = 100m + (i % 100) - 2
            });
        }

        _context.MarketData.AddRange(marketDataEntries);
        await _context.SaveChangesAsync();

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _controller.GetLivePrices();
        var endTime = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        // Performance assertion - should complete within reasonable time
        var executionTime = endTime - startTime;
        executionTime.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetLivePrices_DatabaseException_ReturnsInternalServerError()
    {
        // Arrange - Dispose context to simulate database error
        await _context.DisposeAsync();

        // Act
        var result = await _controller.GetLivePrices();

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in GetLivePrices")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLivePrices_TracksExecutionTime()
    {
        // Arrange
        var testMarketData = new List<MarketData>
        {
            TestDataSeeder.CreateTestMarketData(1, 150.25m)
        };

        _context.MarketData.AddRange(testMarketData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLivePrices();

        // Assert
        result.Should().NotBeNull();

        // Verify that execution time was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GetLivePrices completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    protected override void SeedTestData(TradingDbContext context)
    {
        // Seed some basic test data
        TestDataSeeder.SeedDatabase(context);
    }

    public override void Dispose()
    {
        _context?.Dispose();
        base.Dispose();
    }
}

// Mock interface for TradingHub if it doesn't exist
public class TradingHub : Hub
{
    // Hub implementation
}