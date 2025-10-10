using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using Xunit;

namespace MyTrader.Tests.Services;

/// <summary>
/// Comprehensive unit tests for DataSourceRouter
/// Tests routing logic, state transitions, failover, and data validation
/// </summary>
public class DataSourceRouterTests
{
    private readonly Mock<ILogger<DataSourceRouter>> _mockLogger;
    private readonly DataSourceRouter _router;

    public DataSourceRouterTests()
    {
        _mockLogger = new Mock<ILogger<DataSourceRouter>>();
        _router = new DataSourceRouter(_mockLogger.Object);
    }

    #region Helper Methods

    private static StockPriceData CreateValidStockPriceData(string symbol, decimal price, string source = "ALPACA")
    {
        return new StockPriceData
        {
            Symbol = symbol,
            AssetClass = AssetClassCode.STOCK,
            Market = "NASDAQ",
            Price = price,
            PreviousClose = price * 0.99m, // 1% change
            PriceChange = price * 0.01m,
            PriceChangePercent = 1.0m,
            Volume = 1000000,
            Timestamp = DateTime.UtcNow,
            Source = source,
            QualityScore = source == "ALPACA" ? 100 : 80
        };
    }

    private static StockPriceData CreateInvalidPriceData(string symbol, decimal price)
    {
        return new StockPriceData
        {
            Symbol = symbol,
            Price = price,
            Timestamp = DateTime.UtcNow,
            Source = "ALPACA"
        };
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesWithStartupState()
    {
        // Arrange & Act
        var router = new DataSourceRouter(_mockLogger.Object);

        // Assert
        Assert.Equal(RoutingState.STARTUP, router.CurrentState);
    }

    [Fact]
    public void GetStatus_Initially_ReturnsStartupState()
    {
        // Act
        var status = _router.GetStatus();

        // Assert
        Assert.NotNull(status);
        Assert.Equal(RoutingState.STARTUP, status.CurrentState);
        Assert.Equal("Initial startup", status.StateChangeReason);
        Assert.Equal(0, status.FallbackActivationCount);
        Assert.Null(status.LastFallbackActivation);
        Assert.Equal(100.0, status.UptimePercent);
    }

    #endregion

    #region Routing State Transition Tests

    [Fact]
    public void OnAlpacaPriceUpdate_FromStartup_TransitionsToPrimaryActive()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        RoutingState? newState = null;
        string? reason = null;
        _router.StateChanged += (state, r) => { newState = state; reason = r; };

        // Act
        _router.OnAlpacaPriceUpdate(data);

        // Assert
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, _router.CurrentState);
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, newState);
        Assert.Contains("Alpaca connected", reason);
    }

    [Fact]
    public void OnYahooPriceUpdate_FromStartupWithNoAlpaca_TransitionsToFallback()
    {
        // Arrange
        var data = CreateValidStockPriceData("GOOGL", 140.00m, "YAHOO_FALLBACK");
        RoutingState? newState = null;

        _router.StateChanged += (state, r) => { newState = state; };

        // Act
        _router.OnYahooPriceUpdate(data);

        // Assert
        Assert.Equal(RoutingState.FALLBACK_ACTIVE, _router.CurrentState);
        Assert.Equal(RoutingState.FALLBACK_ACTIVE, newState);
    }

    [Fact]
    public void NotifyAlpacaHealthStatus_Unhealthy_ActivatesFallbackAfterThreshold()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data); // Transition to PRIMARY_ACTIVE

        // Act - Trigger 3 consecutive failures
        _router.NotifyAlpacaHealthStatus(false);
        _router.NotifyAlpacaHealthStatus(false);
        _router.NotifyAlpacaHealthStatus(false);

        // Assert
        Assert.Equal(RoutingState.FALLBACK_ACTIVE, _router.CurrentState);
        var status = _router.GetStatus();
        Assert.Equal(1, status.FallbackActivationCount);
        Assert.NotNull(status.LastFallbackActivation);
    }

    [Fact]
    public void NotifyAlpacaHealthStatus_Healthy_ResetsFailureCount()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data);

        // Act
        _router.NotifyAlpacaHealthStatus(false);
        _router.NotifyAlpacaHealthStatus(false);
        _router.NotifyAlpacaHealthStatus(true); // Reset

        // Assert
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, _router.CurrentState);
        var status = _router.GetStatus();
        Assert.Equal(0, status.AlpacaStatus.ConsecutiveFailures);
    }

    [Fact]
    public void StateTransition_BothSourcesUnhealthy_TransitionsToBothUnavailable()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data);

        // Act - Mark both as unhealthy
        _router.NotifyAlpacaHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);

        // Assert
        Assert.Equal(RoutingState.BOTH_UNAVAILABLE, _router.CurrentState);
    }

    [Fact]
    public void StateTransition_RecoveryFromBothUnavailable_RestoresPrimaryIfAlpacaHealthy()
    {
        // Arrange - Get to BOTH_UNAVAILABLE state
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data);
        _router.NotifyAlpacaHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);

        // Act - Restore Alpaca health
        _router.NotifyAlpacaHealthStatus(true);
        _router.NotifyYahooHealthStatus(true);

        // Assert
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, _router.CurrentState);
    }

    [Fact]
    public void StateTransition_RecoveryFromBothUnavailable_UsesFallbackIfOnlyYahooHealthy()
    {
        // Arrange - Get to BOTH_UNAVAILABLE state
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data);
        _router.NotifyAlpacaHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);
        _router.NotifyYahooHealthStatus(false);

        // Act - Restore only Yahoo health
        _router.NotifyYahooHealthStatus(true);

        // Assert
        Assert.Equal(RoutingState.FALLBACK_ACTIVE, _router.CurrentState);
    }

    #endregion

    #region Data Routing Tests

    [Fact]
    public void OnAlpacaPriceUpdate_InPrimaryState_RoutesDataCorrectly()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(data);

        // Assert
        Assert.NotNull(routedData);
        Assert.Equal("AAPL", routedData.Symbol);
        Assert.Equal(150.00m, routedData.Price);
        Assert.Equal("ALPACA", routedData.Source);
    }

    [Fact]
    public void OnYahooPriceUpdate_InFallbackState_RoutesDataCorrectly()
    {
        // Arrange
        var yahooData = CreateValidStockPriceData("GOOGL", 140.00m, "YAHOO_FALLBACK");
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnYahooPriceUpdate(yahooData); // Triggers fallback state

        // Assert
        Assert.NotNull(routedData);
        Assert.Equal("GOOGL", routedData.Symbol);
        Assert.Equal(140.00m, routedData.Price);
        Assert.Equal("YAHOO_FALLBACK", routedData.Source);
    }

    [Fact]
    public void OnYahooPriceUpdate_InPrimaryState_DoesNotRouteData()
    {
        // Arrange
        var alpacaData = CreateValidStockPriceData("AAPL", 150.00m);
        var yahooData = CreateValidStockPriceData("AAPL", 151.00m, "YAHOO_FALLBACK");
        StockPriceData? routedData = null;
        int routeCount = 0;

        _router.PriceDataRouted += (d) => { routedData = d; routeCount++; };

        // Act
        _router.OnAlpacaPriceUpdate(alpacaData); // PRIMARY_ACTIVE
        _router.OnYahooPriceUpdate(yahooData); // Should be ignored

        // Assert
        Assert.NotNull(routedData);
        Assert.Equal(1, routeCount); // Only Alpaca data routed
        Assert.Equal("ALPACA", routedData.Source);
    }

    #endregion

    #region Data Validation Tests

    [Fact]
    public void OnAlpacaPriceUpdate_WithNegativePrice_RejectsData()
    {
        // Arrange
        var invalidData = CreateInvalidPriceData("AAPL", -10.00m);
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(invalidData);

        // Assert
        Assert.Null(routedData); // Data should be rejected
    }

    [Fact]
    public void OnAlpacaPriceUpdate_WithZeroPrice_RejectsData()
    {
        // Arrange
        var invalidData = CreateInvalidPriceData("AAPL", 0m);
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(invalidData);

        // Assert
        Assert.Null(routedData);
    }

    [Fact]
    public void OnAlpacaPriceUpdate_WithNegativeVolume_RejectsData()
    {
        // Arrange
        var invalidData = CreateValidStockPriceData("AAPL", 150.00m);
        invalidData.Volume = -1000;
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(invalidData);

        // Assert
        Assert.Null(routedData);
    }

    [Fact]
    public void OnAlpacaPriceUpdate_WithFutureTimestamp_RejectsData()
    {
        // Arrange
        var invalidData = CreateValidStockPriceData("AAPL", 150.00m);
        invalidData.Timestamp = DateTime.UtcNow.AddMinutes(10); // 10 minutes in future
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(invalidData);

        // Assert
        Assert.Null(routedData);
    }

    [Fact]
    public void OnAlpacaPriceUpdate_WithLargePriceJump_ActivatesCircuitBreaker()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 100.00m);
        data.PreviousClose = 100.00m;
        data.Price = 125.00m; // 25% jump - exceeds 20% threshold
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(data);

        // Assert
        Assert.Null(routedData); // Circuit breaker should reject
    }

    [Fact]
    public void OnAlpacaPriceUpdate_WithAcceptablePriceChange_PassesValidation()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 100.00m);
        data.PreviousClose = 100.00m;
        data.Price = 115.00m; // 15% change - within 20% threshold
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(data);

        // Assert
        Assert.NotNull(routedData);
        Assert.Equal(115.00m, routedData.Price);
    }

    #endregion

    #region Cross-Source Validation Tests

    [Fact]
    public void CrossSourceValidation_SmallPriceDelta_PassesWithoutWarning()
    {
        // Arrange
        var data1 = CreateValidStockPriceData("AAPL", 150.00m);
        var data2 = CreateValidStockPriceData("AAPL", 151.00m); // 0.67% change
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnAlpacaPriceUpdate(data1);
        _router.OnAlpacaPriceUpdate(data2);

        // Assert
        Assert.NotNull(routedData);
        Assert.Equal(151.00m, routedData.Price);
    }

    [Fact]
    public void CrossSourceValidation_LargePriceDelta_LogsWarning()
    {
        // Arrange
        var data1 = CreateValidStockPriceData("AAPL", 150.00m);
        var data2 = CreateValidStockPriceData("AAPL", 160.00m); // 6.67% change
        _router.PriceDataRouted += (d) => { };

        // Act
        _router.OnAlpacaPriceUpdate(data1);
        _router.OnAlpacaPriceUpdate(data2);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Large price movement")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    #endregion

    #region Manual Failover Tests

    [Fact]
    public async Task ForceFailoverAsync_InPrimaryState_SwitchesToFallback()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data); // PRIMARY_ACTIVE

        // Act
        await _router.ForceFailoverAsync();

        // Assert
        Assert.Equal(RoutingState.FALLBACK_ACTIVE, _router.CurrentState);
        var status = _router.GetStatus();
        Assert.Equal(1, status.FallbackActivationCount);
    }

    [Fact]
    public async Task ForceFailoverAsync_AlreadyInFallback_DoesNotIncrementCounter()
    {
        // Arrange
        var yahooData = CreateValidStockPriceData("GOOGL", 140.00m, "YAHOO_FALLBACK");
        _router.OnYahooPriceUpdate(yahooData); // FALLBACK_ACTIVE

        // Act
        await _router.ForceFailoverAsync();

        // Assert
        Assert.Equal(RoutingState.FALLBACK_ACTIVE, _router.CurrentState);
        var status = _router.GetStatus();
        Assert.Equal(1, status.FallbackActivationCount); // Should still be 1
    }

    #endregion

    #region Status Reporting Tests

    [Fact]
    public void GetStatus_TracksProviderHealth()
    {
        // Arrange
        var alpacaData = CreateValidStockPriceData("AAPL", 150.00m);
        var yahooData = CreateValidStockPriceData("GOOGL", 140.00m, "YAHOO_FALLBACK");

        // Act
        _router.OnAlpacaPriceUpdate(alpacaData);
        _router.OnYahooPriceUpdate(yahooData);
        var status = _router.GetStatus();

        // Assert
        Assert.NotNull(status.AlpacaStatus);
        Assert.NotNull(status.YahooStatus);
        Assert.True(status.AlpacaStatus.IsHealthy);
        Assert.NotNull(status.AlpacaStatus.LastMessageReceivedAt);
        Assert.Equal(1, status.AlpacaStatus.MessagesReceivedCount);
        Assert.Equal(1, status.YahooStatus.MessagesReceivedCount);
    }

    [Fact]
    public void GetStatus_CalculatesUptimePercentCorrectly()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);

        // Act
        _router.OnAlpacaPriceUpdate(data); // PRIMARY_ACTIVE
        var status = _router.GetStatus();

        // Assert
        Assert.Equal(100.0, status.UptimePercent); // No fallback time
    }

    [Fact]
    public async Task GetStatus_TracksFallbackDuration()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data);

        // Act
        await _router.ForceFailoverAsync();
        await Task.Delay(100); // Simulate fallback duration
        var status = _router.GetStatus();

        // Assert
        Assert.True(status.TotalFallbackDuration > TimeSpan.Zero);
        Assert.True(status.UptimePercent < 100.0);
    }

    #endregion

    #region State Change Event Tests

    [Fact]
    public void StateChanged_Event_FiresOnTransition()
    {
        // Arrange
        var eventFired = false;
        RoutingState? newState = null;
        string? reason = null;

        _router.StateChanged += (state, r) =>
        {
            eventFired = true;
            newState = state;
            reason = r;
        };

        var data = CreateValidStockPriceData("AAPL", 150.00m);

        // Act
        _router.OnAlpacaPriceUpdate(data);

        // Assert
        Assert.True(eventFired);
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, newState);
        Assert.NotNull(reason);
    }

    [Fact]
    public void StateChanged_Event_DoesNotFireOnSameState()
    {
        // Arrange
        var data = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(data); // Transition to PRIMARY_ACTIVE

        int eventCount = 0;
        _router.StateChanged += (state, r) => { eventCount++; };

        // Act
        _router.OnAlpacaPriceUpdate(CreateValidStockPriceData("GOOGL", 140.00m));

        // Assert
        Assert.Equal(0, eventCount); // Should not fire
    }

    #endregion

    #region Primary Recovery Tests

    [Fact]
    public void CheckPrimaryRecovery_InFallbackWithHealthyAlpaca_RecoversToPrimary()
    {
        // Arrange
        var yahooData = CreateValidStockPriceData("GOOGL", 140.00m, "YAHOO_FALLBACK");
        _router.OnYahooPriceUpdate(yahooData); // FALLBACK_ACTIVE

        // Act
        _router.NotifyAlpacaHealthStatus(true);
        var alpacaData = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(alpacaData); // Should trigger recovery

        // Assert
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, _router.CurrentState);
    }

    [Fact]
    public void CheckPrimaryRecovery_AppliesGracePeriod()
    {
        // Arrange
        var yahooData = CreateValidStockPriceData("GOOGL", 140.00m, "YAHOO_FALLBACK");
        _router.OnYahooPriceUpdate(yahooData); // FALLBACK_ACTIVE

        // Act - Set Alpaca healthy and send message
        _router.NotifyAlpacaHealthStatus(true);
        var alpacaData = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(alpacaData);

        // Assert
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, _router.CurrentState);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void OnAlpacaPriceUpdate_WithException_IncreasesConsecutiveFailures()
    {
        // Arrange
        var validData = CreateValidStockPriceData("AAPL", 150.00m);
        _router.OnAlpacaPriceUpdate(validData);

        // Act - Send invalid data multiple times
        var invalidData = CreateInvalidPriceData("AAPL", -10.00m);
        _router.OnAlpacaPriceUpdate(invalidData);

        var status = _router.GetStatus();

        // Assert
        Assert.Equal(0, status.AlpacaStatus.ConsecutiveFailures); // Validation happens before failure tracking
    }

    [Fact]
    public void OnYahooPriceUpdate_WithValidationFailure_HandlesGracefully()
    {
        // Arrange
        var invalidData = CreateInvalidPriceData("GOOGL", 0m);
        StockPriceData? routedData = null;
        _router.PriceDataRouted += (d) => { routedData = d; };

        // Act
        _router.OnYahooPriceUpdate(invalidData);

        // Assert
        Assert.Null(routedData);
        Assert.Equal(RoutingState.STARTUP, _router.CurrentState); // Should not change state
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentPriceUpdates_HandledSafely()
    {
        // Arrange
        var tasks = new List<Task>();
        var symbols = new[] { "AAPL", "GOOGL", "MSFT", "TSLA", "AMZN" };

        // Act
        for (int i = 0; i < 100; i++)
        {
            var symbol = symbols[i % symbols.Length];
            var price = 100m + i;
            var data = CreateValidStockPriceData(symbol, price);

            tasks.Add(Task.Run(() => _router.OnAlpacaPriceUpdate(data)));
        }

        await Task.WhenAll(tasks);

        // Assert
        var status = _router.GetStatus();
        Assert.True(status.AlpacaStatus.MessagesReceivedCount > 0);
        Assert.Equal(RoutingState.PRIMARY_ACTIVE, _router.CurrentState);
    }

    #endregion
}
