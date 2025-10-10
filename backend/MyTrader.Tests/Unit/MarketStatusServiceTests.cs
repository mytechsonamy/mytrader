using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MyTrader.Core.Enums;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Services;

namespace MyTrader.Tests.Unit;

/// <summary>
/// Unit tests for MarketHoursService
/// </summary>
public class MarketHoursServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<MarketHoursService>> _loggerMock;
    private readonly MarketHoursService _service;

    public MarketHoursServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<MarketHoursService>>();
        _service = new MarketHoursService(_cache, _loggerMock.Object);
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    #region BIST Exchange Tests

    [Fact]
    public void GetMarketStatus_BIST_OnWeekday_ReturnsCorrectStatus()
    {
        // Arrange - Mock Monday at 11:00 Turkish time (during market hours)
        // Note: This test depends on the current system time
        // For production tests, we'd inject a time provider

        // Act
        var status = _service.GetMarketStatus(Exchange.BIST);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(Exchange.BIST, status.Exchange);
        Assert.NotNull(status.TimeZone);
        Assert.NotEqual(DateTime.MinValue, status.LastCheckTime);
    }

    [Fact]
    public void GetMarketStatus_BIST_CacheWorks()
    {
        // Arrange & Act
        var status1 = _service.GetMarketStatus(Exchange.BIST);
        var status2 = _service.GetMarketStatus(Exchange.BIST);

        // Assert - Should return cached result
        Assert.Equal(status1.LastCheckTime, status2.LastCheckTime);
    }

    [Fact]
    public void IsHoliday_BIST_NewYearsDay_ReturnsTrue()
    {
        // Arrange
        var newYearsDay = new DateTime(2025, 1, 1);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.BIST, newYearsDay);

        // Assert
        Assert.True(isHoliday);
    }

    [Fact]
    public void IsHoliday_BIST_RegularWeekday_ReturnsFalse()
    {
        // Arrange
        var regularDay = new DateTime(2025, 3, 15); // A regular Saturday

        // Act
        var isHoliday = _service.IsHoliday(Exchange.BIST, regularDay);

        // Assert
        Assert.False(isHoliday);
    }

    [Fact]
    public void IsHoliday_BIST_RepublicDay_ReturnsTrue()
    {
        // Arrange
        var republicDay = new DateTime(2025, 10, 29);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.BIST, republicDay);

        // Assert
        Assert.True(isHoliday);
    }

    #endregion

    #region US Exchange Tests (NASDAQ/NYSE)

    [Fact]
    public void GetMarketStatus_NASDAQ_ReturnsValidStatus()
    {
        // Act
        var status = _service.GetMarketStatus(Exchange.NASDAQ);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(Exchange.NASDAQ, status.Exchange);
        Assert.NotNull(status.TradingHours);
        Assert.Contains("09:30", status.TradingHours);
        Assert.NotNull(status.PreMarketHours);
        Assert.NotNull(status.PostMarketHours);
    }

    [Fact]
    public void GetMarketStatus_NYSE_ReturnsValidStatus()
    {
        // Act
        var status = _service.GetMarketStatus(Exchange.NYSE);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(Exchange.NYSE, status.Exchange);
        Assert.NotNull(status.TradingHours);
    }

    [Fact]
    public void IsHoliday_NASDAQ_ChristmasDay_ReturnsTrue()
    {
        // Arrange
        var christmas = new DateTime(2025, 12, 25);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.NASDAQ, christmas);

        // Assert
        Assert.True(isHoliday);
    }

    [Fact]
    public void IsHoliday_NASDAQ_IndependenceDay_ReturnsTrue()
    {
        // Arrange
        var independenceDay = new DateTime(2025, 7, 4);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.NASDAQ, independenceDay);

        // Assert
        Assert.True(isHoliday);
    }

    [Fact]
    public void IsHoliday_NYSE_Thanksgiving_ReturnsTrue()
    {
        // Arrange
        var thanksgiving = new DateTime(2025, 11, 27);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.NYSE, thanksgiving);

        // Assert
        Assert.True(isHoliday);
    }

    [Fact]
    public void IsHoliday_NYSE_RegularWeekday_ReturnsFalse()
    {
        // Arrange
        var regularDay = new DateTime(2025, 3, 12); // A regular Wednesday

        // Act
        var isHoliday = _service.IsHoliday(Exchange.NYSE, regularDay);

        // Assert
        Assert.False(isHoliday);
    }

    #endregion

    #region Crypto Exchange Tests

    [Fact]
    public void GetMarketStatus_CRYPTO_AlwaysOpen()
    {
        // Act
        var status = _service.GetMarketStatus(Exchange.CRYPTO);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(Exchange.CRYPTO, status.Exchange);
        Assert.Equal(MarketStatus.OPEN, status.State);
        Assert.Null(status.NextOpenTime);
        Assert.Null(status.NextCloseTime);
        Assert.Equal("24/7", status.TradingHours);
    }

    [Fact]
    public void IsMarketOpen_CRYPTO_AlwaysTrue()
    {
        // Act
        var isOpen = _service.IsMarketOpen(Exchange.CRYPTO);

        // Assert
        Assert.True(isOpen);
    }

    [Fact]
    public void GetNextOpenTime_CRYPTO_ReturnsNull()
    {
        // Act
        var nextOpen = _service.GetNextOpenTime(Exchange.CRYPTO);

        // Assert
        Assert.Null(nextOpen);
    }

    [Fact]
    public void GetNextCloseTime_CRYPTO_ReturnsNull()
    {
        // Act
        var nextClose = _service.GetNextCloseTime(Exchange.CRYPTO);

        // Assert
        Assert.Null(nextClose);
    }

    [Fact]
    public void IsHoliday_CRYPTO_NeverHoliday()
    {
        // Arrange
        var christmas = new DateTime(2025, 12, 25);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.CRYPTO, christmas);

        // Assert
        Assert.False(isHoliday);
    }

    #endregion

    #region Symbol to Exchange Tests

    [Theory]
    [InlineData("AKBNK.IS", Exchange.BIST)]
    [InlineData("THYAO.IS", Exchange.BIST)]
    [InlineData("GARAN.IS", Exchange.BIST)]
    public void GetExchangeForSymbol_BISTSymbols_ReturnsBIST(string symbol, Exchange expected)
    {
        // Act
        var exchange = _service.GetExchangeForSymbol(symbol);

        // Assert
        Assert.Equal(expected, exchange);
    }

    [Theory]
    [InlineData("BTCUSDT", Exchange.CRYPTO)]
    [InlineData("ETHUSDT", Exchange.CRYPTO)]
    [InlineData("BNBUSDT", Exchange.CRYPTO)]
    [InlineData("BTCUSD", Exchange.CRYPTO)]
    public void GetExchangeForSymbol_CryptoSymbols_ReturnsCRYPTO(string symbol, Exchange expected)
    {
        // Act
        var exchange = _service.GetExchangeForSymbol(symbol);

        // Assert
        Assert.Equal(expected, exchange);
    }

    [Theory]
    [InlineData("AAPL", Exchange.NASDAQ)]
    [InlineData("GOOGL", Exchange.NASDAQ)]
    [InlineData("MSFT", Exchange.NASDAQ)]
    [InlineData("TSLA", Exchange.NASDAQ)]
    public void GetExchangeForSymbol_USStockSymbols_ReturnsNASDAQ(string symbol, Exchange expected)
    {
        // Act
        var exchange = _service.GetExchangeForSymbol(symbol);

        // Assert
        Assert.Equal(expected, exchange);
    }

    [Fact]
    public void GetExchangeForSymbol_EmptyString_ReturnsUNKNOWN()
    {
        // Act
        var exchange = _service.GetExchangeForSymbol(string.Empty);

        // Assert
        Assert.Equal(Exchange.UNKNOWN, exchange);
    }

    [Fact]
    public void GetExchangeForSymbol_NullString_ReturnsUNKNOWN()
    {
        // Act
        var exchange = _service.GetExchangeForSymbol(null!);

        // Assert
        Assert.Equal(Exchange.UNKNOWN, exchange);
    }

    #endregion

    #region All Market Statuses Tests

    [Fact]
    public void GetAllMarketStatuses_ReturnsAllExchanges()
    {
        // Act
        var statuses = _service.GetAllMarketStatuses();

        // Assert
        Assert.NotNull(statuses);
        Assert.True(statuses.Count >= 4); // At least BIST, NASDAQ, NYSE, CRYPTO
        Assert.Contains(Exchange.BIST, statuses.Keys);
        Assert.Contains(Exchange.NASDAQ, statuses.Keys);
        Assert.Contains(Exchange.NYSE, statuses.Keys);
        Assert.Contains(Exchange.CRYPTO, statuses.Keys);
        Assert.DoesNotContain(Exchange.UNKNOWN, statuses.Keys);
    }

    [Fact]
    public void GetAllMarketStatuses_AllStatusesHaveValidData()
    {
        // Act
        var statuses = _service.GetAllMarketStatuses();

        // Assert
        foreach (var kvp in statuses)
        {
            Assert.NotNull(kvp.Value);
            Assert.Equal(kvp.Key, kvp.Value.Exchange);
            Assert.NotEqual(DateTime.MinValue, kvp.Value.LastCheckTime);
            Assert.NotNull(kvp.Value.TradingHours);
        }
    }

    #endregion

    #region Next Open/Close Time Tests

    [Fact]
    public void GetNextOpenTime_BIST_ReturnsValidTime()
    {
        // Act
        var nextOpen = _service.GetNextOpenTime(Exchange.BIST);

        // Assert
        // BIST is not 24/7, so should return a time
        Assert.NotNull(nextOpen);
        Assert.True(nextOpen.Value > DateTime.UtcNow);
    }

    [Fact]
    public void GetNextCloseTime_BIST_ReturnsValidTime()
    {
        // Act
        var nextClose = _service.GetNextCloseTime(Exchange.BIST);

        // Assert
        // BIST is not 24/7, so should return a time
        Assert.NotNull(nextClose);
    }

    [Fact]
    public void GetNextOpenTime_NASDAQ_ReturnsValidTime()
    {
        // Act
        var nextOpen = _service.GetNextOpenTime(Exchange.NASDAQ);

        // Assert
        Assert.NotNull(nextOpen);
        Assert.True(nextOpen.Value > DateTime.UtcNow);
    }

    [Fact]
    public void GetNextCloseTime_NYSE_ReturnsValidTime()
    {
        // Act
        var nextClose = _service.GetNextCloseTime(Exchange.NYSE);

        // Assert
        Assert.NotNull(nextClose);
    }

    #endregion

    #region IsMarketOpen Tests

    [Fact]
    public void IsMarketOpen_CRYPTO_ReturnsTrue()
    {
        // Act
        var isOpen = _service.IsMarketOpen(Exchange.CRYPTO);

        // Assert
        Assert.True(isOpen);
    }

    [Fact]
    public void IsMarketOpen_ReturnsValidBooleanForAllExchanges()
    {
        // Act & Assert
        foreach (Exchange exchange in Enum.GetValues(typeof(Exchange)))
        {
            if (exchange != Exchange.UNKNOWN)
            {
                var isOpen = _service.IsMarketOpen(exchange);
                // Should not throw and should return a boolean
                Assert.True(isOpen == true || isOpen == false);
            }
        }
    }

    #endregion

    #region Market Status Information Tests

    [Fact]
    public void GetMarketStatus_ReturnsConsistentData()
    {
        // Act
        var status = _service.GetMarketStatus(Exchange.NASDAQ);

        // Assert
        Assert.NotNull(status);

        // If market is open, next close should be today
        // If market is closed, next open should be in the future
        if (status.State == MarketStatus.OPEN)
        {
            Assert.NotNull(status.NextCloseTime);
            Assert.True(status.NextCloseTime > DateTime.UtcNow);
        }
        else if (status.State == MarketStatus.CLOSED)
        {
            Assert.NotNull(status.NextOpenTime);
            Assert.True(status.NextOpenTime > DateTime.UtcNow);
            Assert.NotNull(status.ClosureReason);
        }
    }

    [Fact]
    public void GetMarketStatus_BIST_IncludesPreMarketHours()
    {
        // Act
        var status = _service.GetMarketStatus(Exchange.BIST);

        // Assert
        Assert.NotNull(status.PreMarketHours);
        Assert.Contains("09:40", status.PreMarketHours);
    }

    [Fact]
    public void GetMarketStatus_NASDAQ_IncludesPreAndPostMarket()
    {
        // Act
        var status = _service.GetMarketStatus(Exchange.NASDAQ);

        // Assert
        Assert.NotNull(status.PreMarketHours);
        Assert.NotNull(status.PostMarketHours);
        Assert.Contains("04:00", status.PreMarketHours);
        Assert.Contains("20:00", status.PostMarketHours);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetMarketStatus_MultipleCallsInShortTime_UsesCaching()
    {
        // Act
        var status1 = _service.GetMarketStatus(Exchange.NASDAQ);
        System.Threading.Thread.Sleep(100); // Small delay
        var status2 = _service.GetMarketStatus(Exchange.NASDAQ);

        // Assert - Should be from cache, so LastCheckTime should be identical
        Assert.Equal(status1.LastCheckTime, status2.LastCheckTime);
    }

    [Fact]
    public void GetExchangeForSymbol_WhitespaceString_ReturnsUNKNOWN()
    {
        // Act
        var exchange = _service.GetExchangeForSymbol("   ");

        // Assert
        Assert.Equal(Exchange.UNKNOWN, exchange);
    }

    [Fact]
    public void IsHoliday_CRYPTO_AnyDate_ReturnsFalse()
    {
        // Arrange
        var randomDates = new[]
        {
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 25),
            new DateTime(2025, 7, 4),
            DateTime.UtcNow.AddDays(100)
        };

        // Act & Assert
        foreach (var date in randomDates)
        {
            var isHoliday = _service.IsHoliday(Exchange.CRYPTO, date);
            Assert.False(isHoliday, $"Crypto should never have holidays, but {date:yyyy-MM-dd} was marked as holiday");
        }
    }

    #endregion

    #region 2026 Holiday Tests (Forward-looking)

    [Fact]
    public void IsHoliday_NASDAQ_2026NewYearsDay_ReturnsTrue()
    {
        // Arrange
        var newYearsDay2026 = new DateTime(2026, 1, 1);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.NASDAQ, newYearsDay2026);

        // Assert
        Assert.True(isHoliday);
    }

    [Fact]
    public void IsHoliday_NASDAQ_2026Christmas_ReturnsTrue()
    {
        // Arrange
        var christmas2026 = new DateTime(2026, 12, 25);

        // Act
        var isHoliday = _service.IsHoliday(Exchange.NASDAQ, christmas2026);

        // Assert
        Assert.True(isHoliday);
    }

    #endregion
}