using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Enums;
using MyTrader.Core.Interfaces;

namespace MyTrader.Core.Services;

/// <summary>
/// Service for determining market hours, trading schedules, and timezone-aware status
/// </summary>
public class MarketHoursService : IMarketHoursService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MarketHoursService> _logger;

    // Cache configuration
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(1);
    private const string CACHE_KEY_PREFIX = "market_hours_";

    // Timezone information
    private readonly TimeZoneInfo _bistTimeZone;
    private readonly TimeZoneInfo _usEasternTimeZone;

    // Trading hours (in local time)
    private readonly Dictionary<Exchange, TradingSchedule> _tradingSchedules;

    // Holiday calendars for 2025
    private readonly Dictionary<Exchange, HashSet<DateTime>> _holidays;

    public MarketHoursService(
        IMemoryCache cache,
        ILogger<MarketHoursService> logger)
    {
        _cache = cache;
        _logger = logger;

        // Initialize timezones
        _bistTimeZone = GetTimeZone("Turkey Standard Time", "Europe/Istanbul");
        _usEasternTimeZone = GetTimeZone("Eastern Standard Time", "America/New_York");

        // Initialize trading schedules
        _tradingSchedules = InitializeTradingSchedules();

        // Initialize holiday calendars
        _holidays = InitializeHolidays();
    }

    /// <inheritdoc />
    public MarketHoursInfo GetMarketStatus(Exchange exchange)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{exchange}";

        // Try to get from cache
        if (_cache.TryGetValue<MarketHoursInfo>(cacheKey, out var cachedStatus) && cachedStatus != null)
        {
            _logger.LogDebug("Returning cached market hours for {Exchange}", exchange);
            return cachedStatus;
        }

        // Calculate fresh status
        var status = CalculateMarketStatus(exchange);

        // Cache the result
        _cache.Set(cacheKey, status, _cacheExpiration);

        _logger.LogDebug("Calculated market hours for {Exchange}: {State}", exchange, status.State);
        return status;
    }

    /// <inheritdoc />
    public DateTime? GetNextOpenTime(Exchange exchange)
    {
        if (exchange == Exchange.CRYPTO)
        {
            return null; // Crypto markets are always open
        }

        var status = GetMarketStatus(exchange);
        return status.NextOpenTime;
    }

    /// <inheritdoc />
    public DateTime? GetNextCloseTime(Exchange exchange)
    {
        if (exchange == Exchange.CRYPTO)
        {
            return null; // Crypto markets never close
        }

        var status = GetMarketStatus(exchange);
        return status.NextCloseTime;
    }

    /// <inheritdoc />
    public bool IsMarketOpen(Exchange exchange)
    {
        var status = GetMarketStatus(exchange);
        return status.State == Enums.MarketStatus.OPEN ||
               status.State == Enums.MarketStatus.PRE_MARKET ||
               status.State == Enums.MarketStatus.AFTER_HOURS;
    }

    /// <inheritdoc />
    public Dictionary<Exchange, MarketHoursInfo> GetAllMarketStatuses()
    {
        var statuses = new Dictionary<Exchange, MarketHoursInfo>();

        foreach (Exchange exchange in Enum.GetValues(typeof(Exchange)))
        {
            if (exchange != Exchange.UNKNOWN)
            {
                statuses[exchange] = GetMarketStatus(exchange);
            }
        }

        return statuses;
    }

    /// <inheritdoc />
    public Exchange GetExchangeForSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return Exchange.UNKNOWN;
        }

        // BIST symbols end with .IS
        if (symbol.EndsWith(".IS"))
        {
            return Exchange.BIST;
        }

        // Crypto symbols typically contain USDT, BTC, ETH, or other crypto identifiers
        if (symbol.Contains("USDT") || symbol.Contains("BTC") || symbol.Contains("ETH") ||
            symbol.Contains("BNB") || symbol.Contains("BUSD") || symbol.Contains("USD") &&
            (symbol.Contains("BTC") || symbol.Contains("ETH")))
        {
            return Exchange.CRYPTO;
        }

        // For US stocks, we could check against known NASDAQ/NYSE symbols
        // For now, default to NASDAQ for non-BIST, non-crypto symbols
        // In production, this should query a symbol database
        return Exchange.NASDAQ;
    }

    /// <inheritdoc />
    public bool IsHoliday(Exchange exchange, DateTime date)
    {
        if (_holidays.TryGetValue(exchange, out var holidaySet))
        {
            return holidaySet.Contains(date.Date);
        }

        return false;
    }

    private MarketHoursInfo CalculateMarketStatus(Exchange exchange)
    {
        var utcNow = DateTime.UtcNow;

        if (exchange == Exchange.CRYPTO)
        {
            return CreateCryptoMarketStatus(utcNow);
        }

        var schedule = _tradingSchedules[exchange];
        var timeZone = GetExchangeTimeZone(exchange);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);

        var status = new MarketHoursInfo
        {
            Exchange = exchange,
            LastCheckTime = utcNow,
            LocalTime = localTime,
            TimeZone = timeZone.DisplayName,
            TradingHours = schedule.GetTradingHoursString(),
            PreMarketHours = schedule.PreMarketStart.HasValue ?
                $"{schedule.PreMarketStart.Value:HH:mm}-{schedule.PreMarketEnd.Value:HH:mm}" : null,
            PostMarketHours = schedule.PostMarketStart.HasValue ?
                $"{schedule.PostMarketStart.Value:HH:mm}-{schedule.PostMarketEnd.Value:HH:mm}" : null
        };

        // Check if today is a weekend
        if (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday)
        {
            status.State = Enums.MarketStatus.CLOSED;
            status.ClosureReason = "Weekend";
            status.NextOpenTime = GetNextWeekdayOpen(localTime, timeZone, schedule);
            status.NextCloseTime = GetNextClose(status.NextOpenTime.Value, timeZone, schedule);
            return status;
        }

        // Check if today is a holiday
        if (IsHoliday(exchange, localTime.Date))
        {
            status.State = Enums.MarketStatus.CLOSED;
            status.ClosureReason = "Market Holiday";
            status.NextOpenTime = GetNextTradingDayOpen(localTime, timeZone, schedule, exchange);
            status.NextCloseTime = GetNextClose(status.NextOpenTime.Value, timeZone, schedule);
            return status;
        }

        var localTimeOnly = localTime.TimeOfDay;

        // Check pre-market hours (US markets only)
        if (schedule.PreMarketStart.HasValue && schedule.PreMarketEnd.HasValue)
        {
            if (localTimeOnly >= schedule.PreMarketStart.Value && localTimeOnly < schedule.PreMarketEnd.Value)
            {
                status.State = Enums.MarketStatus.PRE_MARKET;
                status.NextOpenTime = localTime.Date.Add(schedule.MarketOpen);
                status.NextOpenTime = TimeZoneInfo.ConvertTimeToUtc(status.NextOpenTime.Value, timeZone);
                status.NextCloseTime = localTime.Date.Add(schedule.MarketClose);
                status.NextCloseTime = TimeZoneInfo.ConvertTimeToUtc(status.NextCloseTime.Value, timeZone);
                return status;
            }
        }

        // Check regular market hours
        if (localTimeOnly >= schedule.MarketOpen && localTimeOnly < schedule.MarketClose)
        {
            status.State = Enums.MarketStatus.OPEN;
            status.NextOpenTime = GetNextTradingDayOpen(localTime, timeZone, schedule, exchange);
            status.NextCloseTime = localTime.Date.Add(schedule.MarketClose);
            status.NextCloseTime = TimeZoneInfo.ConvertTimeToUtc(status.NextCloseTime.Value, timeZone);
            return status;
        }

        // Check post-market hours (US markets only)
        if (schedule.PostMarketStart.HasValue && schedule.PostMarketEnd.HasValue)
        {
            if (localTimeOnly >= schedule.PostMarketStart.Value && localTimeOnly < schedule.PostMarketEnd.Value)
            {
                status.State = Enums.MarketStatus.AFTER_HOURS;
                status.NextOpenTime = GetNextTradingDayOpen(localTime, timeZone, schedule, exchange);
                status.NextCloseTime = localTime.Date.Add(schedule.PostMarketEnd.Value);
                status.NextCloseTime = TimeZoneInfo.ConvertTimeToUtc(status.NextCloseTime.Value, timeZone);
                return status;
            }
        }

        // Market is closed
        status.State = Enums.MarketStatus.CLOSED;

        // Determine closure reason and next open time
        if (localTimeOnly < schedule.MarketOpen)
        {
            // Before market open today
            status.ClosureReason = "Pre-market hours";
            status.NextOpenTime = localTime.Date.Add(schedule.MarketOpen);
            status.NextOpenTime = TimeZoneInfo.ConvertTimeToUtc(status.NextOpenTime.Value, timeZone);
            status.NextCloseTime = localTime.Date.Add(schedule.MarketClose);
            status.NextCloseTime = TimeZoneInfo.ConvertTimeToUtc(status.NextCloseTime.Value, timeZone);
        }
        else
        {
            // After market close today
            status.ClosureReason = "After-hours";
            status.NextOpenTime = GetNextTradingDayOpen(localTime, timeZone, schedule, exchange);
            status.NextCloseTime = GetNextClose(status.NextOpenTime.Value, timeZone, schedule);
        }

        return status;
    }

    private MarketHoursInfo CreateCryptoMarketStatus(DateTime utcNow)
    {
        return new MarketHoursInfo
        {
            Exchange = Exchange.CRYPTO,
            State = Enums.MarketStatus.OPEN,
            LastCheckTime = utcNow,
            LocalTime = utcNow,
            TimeZone = "UTC",
            TradingHours = "24/7",
            NextOpenTime = null,
            NextCloseTime = null,
            ClosureReason = null
        };
    }

    private DateTime GetNextWeekdayOpen(DateTime localTime, TimeZoneInfo timeZone, TradingSchedule schedule)
    {
        var nextDay = localTime.Date.AddDays(1);

        while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
        {
            nextDay = nextDay.AddDays(1);
        }

        var nextOpen = nextDay.Add(schedule.MarketOpen);
        return TimeZoneInfo.ConvertTimeToUtc(nextOpen, timeZone);
    }

    private DateTime GetNextTradingDayOpen(DateTime localTime, TimeZoneInfo timeZone, TradingSchedule schedule, Exchange exchange)
    {
        var nextDay = localTime.Date.AddDays(1);

        while (nextDay.DayOfWeek == DayOfWeek.Saturday ||
               nextDay.DayOfWeek == DayOfWeek.Sunday ||
               IsHoliday(exchange, nextDay))
        {
            nextDay = nextDay.AddDays(1);
        }

        var nextOpen = nextDay.Add(schedule.MarketOpen);
        return TimeZoneInfo.ConvertTimeToUtc(nextOpen, timeZone);
    }

    private DateTime GetNextClose(DateTime nextOpenUtc, TimeZoneInfo timeZone, TradingSchedule schedule)
    {
        var nextOpenLocal = TimeZoneInfo.ConvertTimeFromUtc(nextOpenUtc, timeZone);
        var nextClose = nextOpenLocal.Date.Add(schedule.MarketClose);
        return TimeZoneInfo.ConvertTimeToUtc(nextClose, timeZone);
    }

    private TimeZoneInfo GetExchangeTimeZone(Exchange exchange)
    {
        return exchange switch
        {
            Exchange.BIST => _bistTimeZone,
            Exchange.NASDAQ => _usEasternTimeZone,
            Exchange.NYSE => _usEasternTimeZone,
            _ => TimeZoneInfo.Utc
        };
    }

    private TimeZoneInfo GetTimeZone(string windowsId, string linuxId)
    {
        try
        {
            // Try Windows timezone ID first
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
        catch
        {
            try
            {
                // Fall back to Linux/IANA timezone ID
                return TimeZoneInfo.FindSystemTimeZoneById(linuxId);
            }
            catch
            {
                _logger.LogWarning("Could not find timezone {WindowsId}/{LinuxId}, using UTC", windowsId, linuxId);
                return TimeZoneInfo.Utc;
            }
        }
    }

    private Dictionary<Exchange, TradingSchedule> InitializeTradingSchedules()
    {
        return new Dictionary<Exchange, TradingSchedule>
        {
            {
                Exchange.BIST,
                new TradingSchedule
                {
                    MarketOpen = new TimeSpan(10, 0, 0),  // 10:00 AM
                    MarketClose = new TimeSpan(18, 0, 0), // 6:00 PM
                    PreMarketStart = new TimeSpan(9, 40, 0), // 9:40 AM
                    PreMarketEnd = new TimeSpan(10, 0, 0)    // 10:00 AM
                }
            },
            {
                Exchange.NASDAQ,
                new TradingSchedule
                {
                    MarketOpen = new TimeSpan(9, 30, 0),     // 9:30 AM EST
                    MarketClose = new TimeSpan(16, 0, 0),    // 4:00 PM EST
                    PreMarketStart = new TimeSpan(4, 0, 0),  // 4:00 AM EST
                    PreMarketEnd = new TimeSpan(9, 30, 0),   // 9:30 AM EST
                    PostMarketStart = new TimeSpan(16, 0, 0), // 4:00 PM EST
                    PostMarketEnd = new TimeSpan(20, 0, 0)    // 8:00 PM EST
                }
            },
            {
                Exchange.NYSE,
                new TradingSchedule
                {
                    MarketOpen = new TimeSpan(9, 30, 0),     // 9:30 AM EST
                    MarketClose = new TimeSpan(16, 0, 0),    // 4:00 PM EST
                    PreMarketStart = new TimeSpan(4, 0, 0),  // 4:00 AM EST
                    PreMarketEnd = new TimeSpan(9, 30, 0),   // 9:30 AM EST
                    PostMarketStart = new TimeSpan(16, 0, 0), // 4:00 PM EST
                    PostMarketEnd = new TimeSpan(20, 0, 0)    // 8:00 PM EST
                }
            },
            {
                Exchange.CRYPTO,
                new TradingSchedule
                {
                    MarketOpen = TimeSpan.Zero,
                    MarketClose = new TimeSpan(23, 59, 59)
                }
            }
        };
    }

    private Dictionary<Exchange, HashSet<DateTime>> InitializeHolidays()
    {
        return new Dictionary<Exchange, HashSet<DateTime>>
        {
            {
                Exchange.BIST,
                new HashSet<DateTime>
                {
                    // Turkish National Holidays 2025
                    new DateTime(2025, 1, 1),   // New Year's Day
                    new DateTime(2025, 4, 23),  // National Sovereignty and Children's Day
                    new DateTime(2025, 5, 1),   // Labor Day
                    new DateTime(2025, 5, 19),  // Youth and Sports Day
                    new DateTime(2025, 7, 15),  // Democracy and National Unity Day
                    new DateTime(2025, 8, 30),  // Victory Day
                    new DateTime(2025, 10, 29), // Republic Day
                    // Note: Religious holidays (Eid) dates vary each year and should be updated
                }
            },
            {
                Exchange.NASDAQ,
                GetUSMarketHolidays()
            },
            {
                Exchange.NYSE,
                GetUSMarketHolidays()
            },
            {
                Exchange.CRYPTO,
                new HashSet<DateTime>() // Crypto markets don't have holidays
            }
        };
    }

    private HashSet<DateTime> GetUSMarketHolidays()
    {
        return new HashSet<DateTime>
        {
            // US Market Holidays 2025
            new DateTime(2025, 1, 1),   // New Year's Day
            new DateTime(2025, 1, 20),  // Martin Luther King Jr. Day
            new DateTime(2025, 2, 17),  // Presidents' Day
            new DateTime(2025, 4, 18),  // Good Friday
            new DateTime(2025, 5, 26),  // Memorial Day
            new DateTime(2025, 6, 19),  // Juneteenth
            new DateTime(2025, 7, 4),   // Independence Day
            new DateTime(2025, 9, 1),   // Labor Day
            new DateTime(2025, 11, 27), // Thanksgiving Day
            new DateTime(2025, 12, 25), // Christmas Day

            // US Market Holidays 2026 (for forward-looking calculations)
            new DateTime(2026, 1, 1),   // New Year's Day
            new DateTime(2026, 1, 19),  // Martin Luther King Jr. Day
            new DateTime(2026, 2, 16),  // Presidents' Day
            new DateTime(2026, 4, 3),   // Good Friday
            new DateTime(2026, 5, 25),  // Memorial Day
            new DateTime(2026, 6, 19),  // Juneteenth
            new DateTime(2026, 7, 3),   // Independence Day (observed)
            new DateTime(2026, 9, 7),   // Labor Day
            new DateTime(2026, 11, 26), // Thanksgiving Day
            new DateTime(2026, 12, 25), // Christmas Day
        };
    }

    private class TradingSchedule
    {
        public TimeSpan MarketOpen { get; set; }
        public TimeSpan MarketClose { get; set; }
        public TimeSpan? PreMarketStart { get; set; }
        public TimeSpan? PreMarketEnd { get; set; }
        public TimeSpan? PostMarketStart { get; set; }
        public TimeSpan? PostMarketEnd { get; set; }

        public string GetTradingHoursString()
        {
            return $"{MarketOpen:hh\\:mm}-{MarketClose:hh\\:mm}";
        }
    }
}