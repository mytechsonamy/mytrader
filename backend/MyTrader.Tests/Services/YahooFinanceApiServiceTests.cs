using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using MyTrader.Core.DTOs;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using MyTrader.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Services;

public class YahooFinanceApiServiceTests : TestBase
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<YahooFinanceApiService>> _mockLogger;
    private readonly Mock<IOptions<YahooFinanceConfiguration>> _mockOptions;
    private readonly HttpClient _httpClient;
    private readonly YahooFinanceApiService _service;
    private readonly YahooFinanceConfiguration _config;

    public YahooFinanceApiServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = MockServiceHelper.CreateMockLogger<YahooFinanceApiService>();
        _mockOptions = new Mock<IOptions<YahooFinanceConfiguration>>();

        _config = new YahooFinanceConfiguration
        {
            BaseUrl = "https://query1.finance.yahoo.com",
            RequestTimeoutSeconds = 30,
            MaxConcurrentRequests = 5,
            RateLimitDelayMs = 200,
            UserAgent = "MyTrader/1.0",
            MaxRetryAttempts = 3,
            RetryDelayMs = 1000
        };

        _mockOptions.Setup(x => x.Value).Returns(_config);

        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        _service = new YahooFinanceApiService(_httpClient, _mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task GetHistoricalDataAsync_ValidSymbol_ReturnsSuccessResult()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var mockResponse = new
        {
            chart = new
            {
                result = new[]
                {
                    new
                    {
                        meta = new { symbol = "AAPL" },
                        timestamp = new[] { 1640995200L, 1641081600L }, // Unix timestamps
                        indicators = new
                        {
                            quote = new[]
                            {
                                new
                                {
                                    open = new decimal?[] { 177.83m, 179.61m },
                                    high = new decimal?[] { 182.88m, 180.17m },
                                    low = new decimal?[] { 177.71m, 178.26m },
                                    close = new decimal?[] { 182.01m, 179.70m },
                                    volume = new long?[] { 103049300L, 108923700L }
                                }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(mockResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetHistoricalDataAsync(symbol, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);

        var firstCandle = result.Data[0];
        firstCandle.Open.Should().Be(177.83m);
        firstCandle.High.Should().Be(182.88m);
        firstCandle.Low.Should().Be(177.71m);
        firstCandle.Close.Should().Be(182.01m);
        firstCandle.Volume.Should().Be(103049300L);
    }

    [Fact]
    public async Task GetHistoricalDataAsync_InvalidSymbol_ReturnsErrorResult()
    {
        // Arrange
        var symbol = "INVALID";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"chart\":{\"result\":null,\"error\":{\"code\":\"Not Found\",\"description\":\"No data found, symbol may be delisted\"}}}")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetHistoricalDataAsync(symbol, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetHistoricalDataAsync_NetworkError_ReturnsErrorResult()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.GetHistoricalDataAsync(symbol, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Network error");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching historical data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHistoricalDataAsync_RateLimiting_EnforcesDelay()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        var mockResponse = new
        {
            chart = new
            {
                result = new[]
                {
                    new
                    {
                        meta = new { symbol = "AAPL" },
                        timestamp = new[] { 1640995200L },
                        indicators = new
                        {
                            quote = new[]
                            {
                                new
                                {
                                    open = new decimal?[] { 177.83m },
                                    high = new decimal?[] { 182.88m },
                                    low = new decimal?[] { 177.71m },
                                    close = new decimal?[] { 182.01m },
                                    volume = new long?[] { 103049300L }
                                }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(mockResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act - Make multiple rapid requests
        var startTime = DateTime.UtcNow;
        var task1 = _service.GetHistoricalDataAsync(symbol, startDate, endDate);
        var task2 = _service.GetHistoricalDataAsync(symbol, startDate, endDate);

        await Task.WhenAll(task1, task2);
        var endTime = DateTime.UtcNow;

        // Assert
        var totalTime = endTime - startTime;
        totalTime.Should().BeGreaterThan(TimeSpan.FromMilliseconds(_config.RateLimitDelayMs));

        task1.Result.IsSuccess.Should().BeTrue();
        task2.Result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetHistoricalDataAsync_Timeout_ReturnsErrorResult()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        var result = await _service.GetHistoricalDataAsync(symbol, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timeout");
    }

    [Theory]
    [InlineData("AAPL", "NASDAQ")]
    [InlineData("MSFT", "NASDAQ")]
    [InlineData("THYAO.IS", "BIST")]
    [InlineData("BTC-USD", "CRYPTO")]
    public async Task GetHistoricalDataAsync_DifferentMarkets_HandlesCorrectly(string symbol, string market)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        var mockResponse = new
        {
            chart = new
            {
                result = new[]
                {
                    new
                    {
                        meta = new { symbol = symbol },
                        timestamp = new[] { 1640995200L },
                        indicators = new
                        {
                            quote = new[]
                            {
                                new
                                {
                                    open = new decimal?[] { 100.00m },
                                    high = new decimal?[] { 110.00m },
                                    low = new decimal?[] { 95.00m },
                                    close = new decimal?[] { 105.00m },
                                    volume = new long?[] { 1000000L }
                                }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(mockResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetHistoricalDataAsync(symbol, startDate, endDate, market);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHistoricalDataAsync_MalformedJson_ReturnsErrorResult()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{invalid json}", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetHistoricalDataAsync(symbol, startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("parsing");
    }

    [Fact]
    public async Task GetHistoricalDataAsync_CancellationToken_CancelsRequest()
    {
        // Arrange
        var symbol = "AAPL";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var cancellationTokenSource = new CancellationTokenSource();

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken token) =>
            {
                await Task.Delay(100, token); // Simulate delay
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var task = _service.GetHistoricalDataAsync(symbol, startDate, endDate, cancellationToken: cancellationTokenSource.Token);
        cancellationTokenSource.Cancel(); // Cancel immediately

        var result = await task;

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancel");
    }

    public override void Dispose()
    {
        _httpClient?.Dispose();
        base.Dispose();
    }
}

// Test DTOs and Configuration
public class YahooFinanceConfiguration
{
    public string BaseUrl { get; set; } = "https://query1.finance.yahoo.com";
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 5;
    public int RateLimitDelayMs { get; set; } = 200;
    public string UserAgent { get; set; } = "MyTrader/1.0";
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class YahooFinanceResult<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; } = default!;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class HistoricalMarketData
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}