using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Enums;
using MyTrader.Core.Models;
using MarketStatusUpdate = MyTrader.Core.Models.MarketStatusUpdate;
using System.Collections.Concurrent;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// Service for monitoring market status across different timezones and exchanges
/// Tracks market hours, holidays, and special trading sessions
/// </summary>
public class MarketStatusMonitoringService : IHostedService, IDisposable
{
    private readonly ILogger<MarketStatusMonitoringService> _logger;
    private readonly Timer _monitoringTimer;
    private readonly ConcurrentDictionary<string, MarketInfo> _markets;
    private readonly ConcurrentDictionary<string, Core.Enums.MarketStatus> _currentStatuses;
    private bool _disposed;

    // Market monitoring interval (check every minute)
    private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(1);

    public event Action<MarketStatusUpdate>? MarketStatusChanged;

    public MarketStatusMonitoringService(ILogger<MarketStatusMonitoringService> logger)
    {
        _logger = logger;
        _markets = new ConcurrentDictionary<string, MarketInfo>();
        _currentStatuses = new ConcurrentDictionary<string, Core.Enums.MarketStatus>();
        _monitoringTimer = new Timer(CheckAllMarketStatuses, null, Timeout.Infinite, Timeout.Infinite);

        InitializeMarkets();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MarketStatusMonitoringService starting...");

        try
        {
            // Perform initial market status check
            await CheckAllMarketStatusesAsync();

            // Start periodic monitoring
            _monitoringTimer.Change(TimeSpan.Zero, _monitoringInterval);

            _logger.LogInformation("MarketStatusMonitoringService started successfully. Monitoring {MarketCount} markets",
                _markets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MarketStatusMonitoringService");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MarketStatusMonitoringService stopping...");

        try
        {
            // Stop the monitoring timer
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("MarketStatusMonitoringService stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MarketStatusMonitoringService");
            throw;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Get current status for all monitored markets
    /// </summary>
    public async Task<IEnumerable<MarketStatusUpdate>> GetCurrentMarketStatusesAsync()
    {
        var updates = new List<MarketStatusUpdate>();

        foreach (var (marketName, marketInfo) in _markets)
        {
            var currentStatus = await GetMarketStatusAsync(marketInfo);
            var nextEvents = GetNextMarketEvents(marketInfo, currentStatus);

            updates.Add(new MarketStatusUpdate
            {
                Market = marketName,
                Status = ConvertMarketStatus(currentStatus),
                NextOpen = nextEvents.NextOpen,
                NextClose = nextEvents.NextClose,
                Timezone = marketInfo.TimeZone.Id,
                Timestamp = DateTime.UtcNow
            });
        }

        return updates;
    }

    /// <summary>
    /// Get current status for a specific market
    /// </summary>
    public async Task<MarketStatusUpdate?> GetMarketStatusAsync(string marketName)
    {
        if (!_markets.TryGetValue(marketName, out var marketInfo))
        {
            return null;
        }

        var currentStatus = await GetMarketStatusAsync(marketInfo);
        var nextEvents = GetNextMarketEvents(marketInfo, currentStatus);

        return new MarketStatusUpdate
        {
            Market = marketName,
            Status = ConvertMarketStatus(currentStatus),
            NextOpen = nextEvents.NextOpen,
            NextClose = nextEvents.NextClose,
            Timezone = marketInfo.TimeZone.Id,
            Timestamp = DateTime.UtcNow
        };
    }

    private void InitializeMarkets()
    {
        try
        {
            // Crypto markets (24/7)
            _markets.TryAdd("CRYPTO", new MarketInfo
            {
                Name = "CRYPTO",
                TimeZone = TimeZoneInfo.Utc,
                OpenTime = TimeSpan.Zero,
                CloseTime = new TimeSpan(23, 59, 59),
                TradingDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                IsAlways24Hours = true
            });

            // NASDAQ (US Eastern Time)
            var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            _markets.TryAdd("NASDAQ", new MarketInfo
            {
                Name = "NASDAQ",
                TimeZone = easternTimeZone,
                PreMarketStart = new TimeSpan(4, 0, 0), // 4:00 AM ET
                OpenTime = new TimeSpan(9, 30, 0), // 9:30 AM ET
                CloseTime = new TimeSpan(16, 0, 0), // 4:00 PM ET
                AfterHoursEnd = new TimeSpan(20, 0, 0), // 8:00 PM ET
                TradingDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                Holidays = GetUSMarketHolidays()
            });

            // NYSE (same as NASDAQ for simplicity)
            _markets.TryAdd("NYSE", new MarketInfo
            {
                Name = "NYSE",
                TimeZone = easternTimeZone,
                PreMarketStart = new TimeSpan(4, 0, 0),
                OpenTime = new TimeSpan(9, 30, 0),
                CloseTime = new TimeSpan(16, 0, 0),
                AfterHoursEnd = new TimeSpan(20, 0, 0),
                TradingDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                Holidays = GetUSMarketHolidays()
            });

            // BIST (Turkey Time)
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            _markets.TryAdd("BIST", new MarketInfo
            {
                Name = "BIST",
                TimeZone = turkeyTimeZone,
                OpenTime = new TimeSpan(9, 30, 0), // 9:30 AM TRT
                CloseTime = new TimeSpan(18, 0, 0), // 6:00 PM TRT
                TradingDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                Holidays = GetTurkishMarketHolidays()
            });

            _logger.LogInformation("Initialized {MarketCount} markets for monitoring", _markets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing markets");
        }
    }

    private void CheckAllMarketStatuses(object? state)
    {
        _ = Task.Run(async () => await CheckAllMarketStatusesAsync());
    }

    private async Task CheckAllMarketStatusesAsync()
    {
        try
        {
            var statusCheckTasks = _markets.Select(async kvp =>
            {
                var (marketName, marketInfo) = kvp;
                var newStatus = await GetMarketStatusAsync(marketInfo);
                var previousStatus = _currentStatuses.GetValueOrDefault(marketName, Core.Enums.MarketStatus.UNKNOWN);

                if (newStatus != previousStatus)
                {
                    _currentStatuses.AddOrUpdate(marketName, newStatus, (_, _) => newStatus);

                    var nextEvents = GetNextMarketEvents(marketInfo, newStatus);
                    var statusUpdate = new MarketStatusUpdate
                    {
                        Market = marketName,
                        Status = ConvertMarketStatus(newStatus),
                        NextOpen = nextEvents.NextOpen,
                        NextClose = nextEvents.NextClose,
                        Timezone = marketInfo.TimeZone.Id,
                        Timestamp = DateTime.UtcNow
                    };

                    _logger.LogInformation("Market status changed: {Market} {PreviousStatus} -> {NewStatus}",
                        marketName, previousStatus, newStatus);

                    MarketStatusChanged?.Invoke(statusUpdate);
                }
            });

            await Task.WhenAll(statusCheckTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking market statuses");
        }
    }

    private async Task<Core.Enums.MarketStatus> GetMarketStatusAsync(MarketInfo marketInfo)
    {
        try
        {
            if (marketInfo.IsAlways24Hours)
            {
                return Core.Enums.MarketStatus.OPEN;
            }

            var marketTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, marketInfo.TimeZone);
            var currentTime = marketTime.TimeOfDay;
            var dayOfWeek = marketTime.DayOfWeek;

            // Check if it's a holiday
            if (IsHoliday(marketInfo, marketTime.Date))
            {
                return Core.Enums.MarketStatus.CLOSED;
            }

            // Check if it's a trading day
            if (!marketInfo.TradingDays.Contains(dayOfWeek))
            {
                return Core.Enums.MarketStatus.CLOSED;
            }

            // Check pre-market, market hours, and after-hours
            if (marketInfo.PreMarketStart.HasValue && currentTime >= marketInfo.PreMarketStart.Value && currentTime < marketInfo.OpenTime)
            {
                return Core.Enums.MarketStatus.PRE_MARKET;
            }

            if (currentTime >= marketInfo.OpenTime && currentTime < marketInfo.CloseTime)
            {
                return Core.Enums.MarketStatus.OPEN;
            }

            if (marketInfo.AfterHoursEnd.HasValue && currentTime >= marketInfo.CloseTime && currentTime < marketInfo.AfterHoursEnd.Value)
            {
                return Core.Enums.MarketStatus.AFTER_HOURS;
            }

            return Core.Enums.MarketStatus.CLOSED;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market status for {MarketName}", marketInfo.Name);
            return Core.Enums.MarketStatus.UNKNOWN;
        }
    }

    private (DateTime? NextOpen, DateTime? NextClose) GetNextMarketEvents(MarketInfo marketInfo, Core.Enums.MarketStatus currentStatus)
    {
        if (marketInfo.IsAlways24Hours)
        {
            return (null, null);
        }

        var marketTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, marketInfo.TimeZone);
        DateTime? nextOpen = null;
        DateTime? nextClose = null;

        if (currentStatus == Core.Enums.MarketStatus.OPEN)
        {
            // Market is open, find next close time
            nextClose = marketTime.Date.Add(marketInfo.CloseTime);
            if (nextClose <= marketTime)
            {
                nextClose = GetNextTradingDay(marketInfo, marketTime.Date).Add(marketInfo.CloseTime);
            }
        }
        else
        {
            // Market is closed, find next open time
            var nextTradingDay = GetNextTradingDay(marketInfo, marketTime.Date);
            nextOpen = nextTradingDay.Add(marketInfo.OpenTime);

            if (currentStatus == Core.Enums.MarketStatus.OPEN || currentStatus == Core.Enums.MarketStatus.PRE_MARKET || currentStatus == Core.Enums.MarketStatus.AFTER_HOURS)
            {
                nextClose = nextTradingDay.Add(marketInfo.CloseTime);
            }
        }

        // Convert back to UTC
        var nextOpenUtc = nextOpen.HasValue ? TimeZoneInfo.ConvertTimeToUtc(nextOpen.Value, marketInfo.TimeZone) : (DateTime?)null;
        var nextCloseUtc = nextClose.HasValue ? TimeZoneInfo.ConvertTimeToUtc(nextClose.Value, marketInfo.TimeZone) : (DateTime?)null;

        return (nextOpenUtc, nextCloseUtc);
    }

    private DateTime GetNextTradingDay(MarketInfo marketInfo, DateTime currentDate)
    {
        var nextDay = currentDate.AddDays(1);

        // Find the next trading day
        while (!marketInfo.TradingDays.Contains(nextDay.DayOfWeek) || IsHoliday(marketInfo, nextDay))
        {
            nextDay = nextDay.AddDays(1);

            // Prevent infinite loop
            if (nextDay > currentDate.AddDays(30))
            {
                _logger.LogWarning("Could not find next trading day for {MarketName} within 30 days", marketInfo.Name);
                break;
            }
        }

        return nextDay;
    }

    private bool IsHoliday(MarketInfo marketInfo, DateTime date)
    {
        return marketInfo.Holidays.Any(holiday => holiday.Date == date.Date);
    }

    private List<DateTime> GetUSMarketHolidays()
    {
        var year = DateTime.Now.Year;
        var holidays = new List<DateTime>();

        // Add major US market holidays for current year
        holidays.Add(new DateTime(year, 1, 1)); // New Year's Day
        holidays.Add(GetMLKDay(year)); // Martin Luther King Jr. Day
        holidays.Add(GetPresidentsDay(year)); // Presidents' Day
        holidays.Add(GetGoodFriday(year)); // Good Friday
        holidays.Add(GetMemorialDay(year)); // Memorial Day
        holidays.Add(new DateTime(year, 7, 4)); // Independence Day
        holidays.Add(GetLaborDay(year)); // Labor Day
        holidays.Add(GetThanksgiving(year)); // Thanksgiving
        holidays.Add(new DateTime(year, 12, 25)); // Christmas Day

        return holidays;
    }

    private List<DateTime> GetTurkishMarketHolidays()
    {
        var year = DateTime.Now.Year;
        var holidays = new List<DateTime>();

        // Add major Turkish holidays for current year
        holidays.Add(new DateTime(year, 1, 1)); // New Year's Day
        holidays.Add(new DateTime(year, 4, 23)); // National Sovereignty and Children's Day
        holidays.Add(new DateTime(year, 5, 1)); // Labour and Solidarity Day
        holidays.Add(new DateTime(year, 5, 19)); // Commemoration of Atatürk, Youth and Sports Day
        holidays.Add(new DateTime(year, 7, 15)); // Democracy and National Unity Day
        holidays.Add(new DateTime(year, 8, 30)); // Victory Day
        holidays.Add(new DateTime(year, 10, 29)); // Republic Day

        // Note: Religious holidays (Eid) are lunar calendar based and would need special calculation

        return holidays;
    }

    // Helper methods for US holidays
    private DateTime GetMLKDay(int year) => GetNthWeekdayOfMonth(year, 1, DayOfWeek.Monday, 3);
    private DateTime GetPresidentsDay(int year) => GetNthWeekdayOfMonth(year, 2, DayOfWeek.Monday, 3);
    private DateTime GetMemorialDay(int year) => GetLastWeekdayOfMonth(year, 5, DayOfWeek.Monday);
    private DateTime GetLaborDay(int year) => GetNthWeekdayOfMonth(year, 9, DayOfWeek.Monday, 1);
    private DateTime GetThanksgiving(int year) => GetNthWeekdayOfMonth(year, 11, DayOfWeek.Thursday, 4);

    private DateTime GetGoodFriday(int year)
    {
        // Easter calculation (simplified)
        var easter = GetEaster(year);
        return easter.AddDays(-2);
    }

    private DateTime GetEaster(int year)
    {
        // Simplified Easter calculation (Gregorian calendar)
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var n = (h + l - 7 * m + 114) / 31;
        var p = (h + l - 7 * m + 114) % 31;
        return new DateTime(year, n, p + 1);
    }

    private DateTime GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        var firstDay = new DateTime(year, month, 1);
        var firstWeekday = firstDay.AddDays(((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7);
        return firstWeekday.AddDays((n - 1) * 7);
    }

    private DateTime GetLastWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        var lastWeekday = lastDay.AddDays(((int)dayOfWeek - (int)lastDay.DayOfWeek - 7) % 7);
        return lastWeekday;
    }

    private static Core.Enums.MarketStatus ConvertMarketStatus(Core.Enums.MarketStatus enumStatus)
    {
        // MarketStatusUpdate.Status expects Enums.MarketStatus, so just return as-is
        return enumStatus;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _monitoringTimer?.Dispose();
            _disposed = true;
            _logger.LogInformation("MarketStatusMonitoringService disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing MarketStatusMonitoringService");
        }
    }
}

/// <summary>
/// Information about a market's trading schedule
/// </summary>
public class MarketInfo
{
    public string Name { get; set; } = string.Empty;
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    public TimeSpan? PreMarketStart { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public TimeSpan? AfterHoursEnd { get; set; }
    public DayOfWeek[] TradingDays { get; set; } = Array.Empty<DayOfWeek>();
    public List<DateTime> Holidays { get; set; } = new();
    public bool IsAlways24Hours { get; set; }
}