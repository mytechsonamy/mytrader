using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Api.Controllers;
using MyTrader.Core.DTOs;
using MyTrader.Core.Interfaces;
using MyTrader.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Controllers;

public class MarketDataControllerTests : TestBase
{
    private readonly Mock<IMultiAssetDataService> _mockMultiAssetDataService;
    private readonly Mock<IDataProviderOrchestrator> _mockDataProviderOrchestrator;
    private readonly Mock<ILogger<MarketDataController>> _mockLogger;
    private readonly MarketDataController _controller;

    public MarketDataControllerTests()
    {
        _mockMultiAssetDataService = MockServiceHelper.CreateMockMultiAssetDataService();
        _mockDataProviderOrchestrator = new Mock<IDataProviderOrchestrator>();
        _mockLogger = MockServiceHelper.CreateMockLogger<MarketDataController>();

        _controller = new MarketDataController(
            _mockMultiAssetDataService.Object,
            _mockDataProviderOrchestrator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetRealtimeMarketData_ValidSymbolId_ReturnsOkWithData()
    {
        // Arrange
        var symbolId = Guid.NewGuid();
        var expectedData = new UnifiedMarketDataDto
        {
            Symbol = "AAPL",
            Price = 150.25m,
            Change = 2.50m,
            ChangePercent = 1.69m,
            Volume = 1000000,
            Timestamp = DateTime.UtcNow,
            AssetClass = "Stock"
        };

        _mockMultiAssetDataService.Setup(x => x.GetMarketDataAsync(symbolId))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetRealtimeMarketData(symbolId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<UnifiedMarketDataDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Symbol.Should().Be("AAPL");
        response.Data.Price.Should().Be(150.25m);

        _mockMultiAssetDataService.Verify(x => x.GetMarketDataAsync(symbolId), Times.Once);
    }

    [Fact]
    public async Task GetRealtimeMarketData_SymbolNotFound_ReturnsNotFound()
    {
        // Arrange
        var symbolId = Guid.NewGuid();

        _mockMultiAssetDataService.Setup(x => x.GetMarketDataAsync(symbolId))
            .ReturnsAsync((UnifiedMarketDataDto?)null);

        // Act
        var result = await _controller.GetRealtimeMarketData(symbolId);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);

        var response = notFoundResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetRealtimeMarketData_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var symbolId = Guid.NewGuid();

        _mockMultiAssetDataService.Setup(x => x.GetMarketDataAsync(symbolId))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetRealtimeMarketData(symbolId);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);

        var response = statusResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetBatchMarketData_ValidRequest_ReturnsOkWithBatchData()
    {
        // Arrange
        var symbolIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new MarketDataRequest
        {
            SymbolIds = symbolIds
        };

        var expectedBatchData = new BatchMarketDataDto
        {
            TotalSymbols = 2,
            SuccessfulSymbols = 2,
            FailedSymbols = 0,
            Data = new List<UnifiedMarketDataDto>
            {
                new UnifiedMarketDataDto
                {
                    Symbol = "AAPL",
                    Price = 150.25m,
                    Change = 2.50m,
                    ChangePercent = 1.69m,
                    Volume = 1000000,
                    Timestamp = DateTime.UtcNow,
                    AssetClass = "Stock"
                },
                new UnifiedMarketDataDto
                {
                    Symbol = "BTC-USD",
                    Price = 45000.00m,
                    Change = 1000.00m,
                    ChangePercent = 2.27m,
                    Volume = 500000,
                    Timestamp = DateTime.UtcNow,
                    AssetClass = "Crypto"
                }
            },
            Errors = new List<string>()
        };

        _mockMultiAssetDataService.Setup(x => x.GetBatchMarketDataAsync(symbolIds))
            .ReturnsAsync(expectedBatchData);

        // Act
        var result = await _controller.GetBatchMarketData(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<BatchMarketDataDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.TotalSymbols.Should().Be(2);
        response.Data.SuccessfulSymbols.Should().Be(2);
        response.Data.Data.Should().HaveCount(2);

        _mockMultiAssetDataService.Verify(x => x.GetBatchMarketDataAsync(symbolIds), Times.Once);
    }

    [Fact]
    public async Task GetBatchMarketData_PartialSuccess_ReturnsOkWithPartialData()
    {
        // Arrange
        var symbolIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new MarketDataRequest
        {
            SymbolIds = symbolIds
        };

        var expectedBatchData = new BatchMarketDataDto
        {
            TotalSymbols = 2,
            SuccessfulSymbols = 1,
            FailedSymbols = 1,
            Data = new List<UnifiedMarketDataDto>
            {
                new UnifiedMarketDataDto
                {
                    Symbol = "AAPL",
                    Price = 150.25m,
                    Change = 2.50m,
                    ChangePercent = 1.69m,
                    Volume = 1000000,
                    Timestamp = DateTime.UtcNow,
                    AssetClass = "Stock"
                }
            },
            Errors = new List<string> { "Failed to retrieve data for symbol: BTC-USD" }
        };

        _mockMultiAssetDataService.Setup(x => x.GetBatchMarketDataAsync(symbolIds))
            .ReturnsAsync(expectedBatchData);

        // Act
        var result = await _controller.GetBatchMarketData(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<BatchMarketDataDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.TotalSymbols.Should().Be(2);
        response.Data.SuccessfulSymbols.Should().Be(1);
        response.Data.FailedSymbols.Should().Be(1);
        response.Data.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHistoricalData_ValidRequest_ReturnsOkWithHistoricalData()
    {
        // Arrange
        var symbolId = Guid.NewGuid();
        var request = new HistoricalDataRequest
        {
            SymbolId = symbolId,
            Interval = "1d",
            StartTime = DateTime.UtcNow.AddDays(-30),
            EndTime = DateTime.UtcNow,
            Limit = 30
        };

        var expectedHistoricalData = new HistoricalMarketDataDto
        {
            Symbol = "AAPL",
            AssetClass = "Stock",
            Interval = "1d",
            CandleCount = 30,
            Candles = new List<CandleDto>
            {
                new CandleDto
                {
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    Open = 148.00m,
                    High = 152.00m,
                    Low = 147.00m,
                    Close = 150.25m,
                    Volume = 1000000
                }
            },
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        _mockMultiAssetDataService.Setup(x => x.GetHistoricalDataAsync(
            symbolId,
            request.Interval,
            request.StartTime,
            request.EndTime,
            request.Limit))
            .ReturnsAsync(expectedHistoricalData);

        // Act
        var result = await _controller.GetHistoricalData(symbolId, request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<HistoricalMarketDataDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Symbol.Should().Be("AAPL");
        response.Data.CandleCount.Should().Be(30);
        response.Data.Candles.Should().HaveCount(1);

        _mockMultiAssetDataService.Verify(x => x.GetHistoricalDataAsync(
            symbolId,
            request.Interval,
            request.StartTime,
            request.EndTime,
            request.Limit), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalData_NoDataAvailable_ReturnsNotFound()
    {
        // Arrange
        var symbolId = Guid.NewGuid();
        var request = new HistoricalDataRequest
        {
            SymbolId = symbolId,
            Interval = "1d",
            StartTime = DateTime.UtcNow.AddDays(-30),
            EndTime = DateTime.UtcNow,
            Limit = 30
        };

        _mockMultiAssetDataService.Setup(x => x.GetHistoricalDataAsync(
            symbolId,
            request.Interval,
            request.StartTime,
            request.EndTime,
            request.Limit))
            .ReturnsAsync((HistoricalMarketDataDto?)null);

        // Act
        var result = await _controller.GetHistoricalData(symbolId, request);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);

        var response = notFoundResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetHistoricalData_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var symbolId = Guid.NewGuid();
        var request = new HistoricalDataRequest
        {
            SymbolId = symbolId,
            Interval = "1d",
            StartTime = DateTime.UtcNow.AddDays(-30),
            EndTime = DateTime.UtcNow,
            Limit = 30
        };

        _mockMultiAssetDataService.Setup(x => x.GetHistoricalDataAsync(
            symbolId,
            request.Interval,
            request.StartTime,
            request.EndTime,
            request.Limit))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetHistoricalData(symbolId, request);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);

        var response = statusResult.Value as ApiResponse<object>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("5m")]
    [InlineData("1h")]
    [InlineData("1d")]
    public async Task GetHistoricalData_ValidIntervals_ShouldWork(string interval)
    {
        // Arrange
        var symbolId = Guid.NewGuid();
        var request = new HistoricalDataRequest
        {
            SymbolId = symbolId,
            Interval = interval,
            StartTime = DateTime.UtcNow.AddDays(-7),
            EndTime = DateTime.UtcNow,
            Limit = 100
        };

        var expectedData = new HistoricalMarketDataDto
        {
            Symbol = "AAPL",
            AssetClass = "Stock",
            Interval = interval,
            CandleCount = 10,
            Candles = new List<CandleDto>()
        };

        _mockMultiAssetDataService.Setup(x => x.GetHistoricalDataAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int?>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetHistoricalData(symbolId, request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as ApiResponse<HistoricalMarketDataDto>;
        response!.Data!.Interval.Should().Be(interval);
    }

    [Fact]
    public async Task GetBatchMarketData_EmptySymbolIds_ShouldHandleGracefully()
    {
        // Arrange
        var request = new MarketDataRequest
        {
            SymbolIds = new List<Guid>()
        };

        var expectedBatchData = new BatchMarketDataDto
        {
            TotalSymbols = 0,
            SuccessfulSymbols = 0,
            FailedSymbols = 0,
            Data = new List<UnifiedMarketDataDto>(),
            Errors = new List<string>()
        };

        _mockMultiAssetDataService.Setup(x => x.GetBatchMarketDataAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(expectedBatchData);

        // Act
        var result = await _controller.GetBatchMarketData(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as ApiResponse<BatchMarketDataDto>;
        response!.Data!.TotalSymbols.Should().Be(0);
        response.Data.Data.Should().BeEmpty();
    }
}

// Additional DTOs for testing (these would typically be in Core project)
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResult(T data, string message)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200
        };
    }

    public static ApiResponse<T> ErrorResult(string message, int statusCode)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}

public class MarketDataRequest
{
    public List<Guid> SymbolIds { get; set; } = new();
}

public class HistoricalDataRequest
{
    public Guid SymbolId { get; set; }
    public string Interval { get; set; } = "1d";
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Limit { get; set; }
}

public class BatchMarketDataDto
{
    public int TotalSymbols { get; set; }
    public int SuccessfulSymbols { get; set; }
    public int FailedSymbols { get; set; }
    public List<UnifiedMarketDataDto> Data { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class HistoricalMarketDataDto
{
    public string Symbol { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public int CandleCount { get; set; }
    public List<CandleDto> Candles { get; set; } = new();
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class CandleDto
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class ValidationErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

public interface IDataProviderOrchestrator
{
    // Interface methods would be defined here
}