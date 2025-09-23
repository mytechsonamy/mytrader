using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTrader.Core.Services;
using System.Globalization;

namespace MyTrader.Infrastructure.Services;

/// <summary>
/// BIST market daily sync service - runs after market close (18:30 TR time)
/// </summary>
public class BistDailySyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BistDailySyncService> _logger;
    private readonly MarketScheduleConfiguration _config;

    public BistDailySyncService(
        IServiceProvider serviceProvider,
        ILogger<BistDailySyncService> logger,
        IOptions<MarketScheduleConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = configuration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BIST Daily Sync Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = CalculateNextBistSyncTime();
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next BIST sync scheduled for {NextRun} (in {Delay})",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss UTC"), delay);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested) break;

                await ExecuteBistSyncAsync(stoppingToken);

                // Wait until next day to avoid multiple runs
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BIST daily sync service");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // Wait before retry
            }
        }

        _logger.LogInformation("BIST Daily Sync Service stopped");
    }

    private async Task ExecuteBistSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dailyDataService = scope.ServiceProvider.GetRequiredService<YahooFinanceDailyDataService>();

        _logger.LogInformation("Starting BIST daily sync");

        try
        {
            var result = await dailyDataService.SyncMarketDataAsync("BIST", cancellationToken: cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("BIST sync completed successfully. " +
                    "Processed: {Successful}, Failed: {Failed}, Duration: {Duration}",
                    result.SuccessfulSymbols, result.FailedSymbols, result.Duration);

                // Send success notification if configured
                await SendSyncNotificationAsync("BIST", result, isSuccess: true, cancellationToken);
            }
            else
            {
                _logger.LogError("BIST sync failed: {Error}", result.ErrorMessage);
                await SendSyncNotificationAsync("BIST", result, isSuccess: false, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BIST sync execution");
            await SendSyncNotificationAsync("BIST", null, isSuccess: false, cancellationToken, ex.Message);
        }
    }

    private DateTime CalculateNextBistSyncTime()
    {
        // BIST sync: Daily at 18:30 Turkey time (after market close)
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var now = DateTime.UtcNow;
        var turkeyNow = TimeZoneInfo.ConvertTimeFromUtc(now, turkeyTimeZone);

        var targetTime = turkeyNow.Date.AddHours(18).AddMinutes(30);

        // If it's past 18:30 today, schedule for tomorrow
        if (turkeyNow > targetTime)
        {
            targetTime = targetTime.AddDays(1);
        }

        // Skip weekends
        while (targetTime.DayOfWeek == DayOfWeek.Saturday || targetTime.DayOfWeek == DayOfWeek.Sunday)
        {
            targetTime = targetTime.AddDays(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(targetTime, turkeyTimeZone);
    }

    private async Task SendSyncNotificationAsync(string market, DailySyncResult? result, bool isSuccess,
        CancellationToken cancellationToken, string? errorMessage = null)
    {
        try
        {
            // Implementation would depend on your notification system
            // Could be email, Slack, Teams, etc.
            _logger.LogInformation("Sync notification sent for {Market}: Success={Success}", market, isSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send sync notification for {Market}", market);
        }
    }
}

/// <summary>
/// US markets (NYSE/NASDAQ) daily sync service - runs after market close (16:30 ET)
/// </summary>
public class USMarketsDailySyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<USMarketsDailySyncService> _logger;
    private readonly MarketScheduleConfiguration _config;

    public USMarketsDailySyncService(
        IServiceProvider serviceProvider,
        ILogger<USMarketsDailySyncService> logger,
        IOptions<MarketScheduleConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = configuration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("US Markets Daily Sync Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = CalculateNextUSMarketsSyncTime();
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next US markets sync scheduled for {NextRun} (in {Delay})",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss UTC"), delay);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested) break;

                // Sync both NYSE and NASDAQ
                await ExecuteUSMarketsSyncAsync(stoppingToken);

                // Wait until next day
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in US markets daily sync service");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("US Markets Daily Sync Service stopped");
    }

    private async Task ExecuteUSMarketsSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dailyDataService = scope.ServiceProvider.GetRequiredService<YahooFinanceDailyDataService>();

        var markets = new[] { "NYSE", "NASDAQ" };

        foreach (var market in markets)
        {
            _logger.LogInformation("Starting {Market} daily sync", market);

            try
            {
                var result = await dailyDataService.SyncMarketDataAsync(market, cancellationToken: cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("{Market} sync completed successfully. " +
                        "Processed: {Successful}, Failed: {Failed}, Duration: {Duration}",
                        market, result.SuccessfulSymbols, result.FailedSymbols, result.Duration);
                }
                else
                {
                    _logger.LogError("{Market} sync failed: {Error}", market, result.ErrorMessage);
                }

                // Add delay between markets
                if (market != markets.Last())
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Market} sync execution", market);
            }
        }
    }

    private DateTime CalculateNextUSMarketsSyncTime()
    {
        // US markets sync: Daily at 16:30 ET (after market close)
        var etTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var now = DateTime.UtcNow;
        var etNow = TimeZoneInfo.ConvertTimeFromUtc(now, etTimeZone);

        var targetTime = etNow.Date.AddHours(16).AddMinutes(30);

        // If it's past 16:30 today, schedule for tomorrow
        if (etNow > targetTime)
        {
            targetTime = targetTime.AddDays(1);
        }

        // Skip weekends
        while (targetTime.DayOfWeek == DayOfWeek.Saturday || targetTime.DayOfWeek == DayOfWeek.Sunday)
        {
            targetTime = targetTime.AddDays(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(targetTime, etTimeZone);
    }
}

/// <summary>
/// Crypto markets daily sync service - runs daily at 00:01 UTC
/// </summary>
public class CryptoDailySyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CryptoDailySyncService> _logger;
    private readonly MarketScheduleConfiguration _config;

    public CryptoDailySyncService(
        IServiceProvider serviceProvider,
        ILogger<CryptoDailySyncService> logger,
        IOptions<MarketScheduleConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = configuration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crypto Daily Sync Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = CalculateNextCryptoSyncTime();
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next crypto sync scheduled for {NextRun} (in {Delay})",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss UTC"), delay);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested) break;

                await ExecuteCryptoSyncAsync(stoppingToken);

                // Wait until next day
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in crypto daily sync service");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("Crypto Daily Sync Service stopped");
    }

    private async Task ExecuteCryptoSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dailyDataService = scope.ServiceProvider.GetRequiredService<YahooFinanceDailyDataService>();

        _logger.LogInformation("Starting crypto daily sync");

        try
        {
            var result = await dailyDataService.SyncMarketDataAsync("CRYPTO", cancellationToken: cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Crypto sync completed successfully. " +
                    "Processed: {Successful}, Failed: {Failed}, Duration: {Duration}",
                    result.SuccessfulSymbols, result.FailedSymbols, result.Duration);
            }
            else
            {
                _logger.LogError("Crypto sync failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during crypto sync execution");
        }
    }

    private DateTime CalculateNextCryptoSyncTime()
    {
        // Crypto sync: Daily at 00:01 UTC
        var now = DateTime.UtcNow;
        var targetTime = now.Date.AddMinutes(1); // 00:01

        // If it's past 00:01 today, schedule for tomorrow
        if (now > targetTime)
        {
            targetTime = targetTime.AddDays(1);
        }

        return targetTime;
    }
}

/// <summary>
/// Data gap monitoring and filling service - runs weekly
/// </summary>
public class DataGapMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataGapMonitoringService> _logger;
    private readonly MarketScheduleConfiguration _config;

    public DataGapMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<DataGapMonitoringService> logger,
        IOptions<MarketScheduleConfiguration> configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = configuration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Gap Monitoring Service started");

        // Wait for initial delay
        await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = CalculateNextGapCheckTime();
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next gap monitoring scheduled for {NextRun} (in {Delay})",
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss UTC"), delay);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested) break;

                await ExecuteGapMonitoringAsync(stoppingToken);

                // Wait until next week
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data gap monitoring service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Data Gap Monitoring Service stopped");
    }

    private async Task ExecuteGapMonitoringAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dailyDataService = scope.ServiceProvider.GetRequiredService<YahooFinanceDailyDataService>();

        var markets = new[] { "BIST", "NYSE", "NASDAQ", "CRYPTO" };
        var endDate = DateTime.UtcNow.Date.AddDays(-1);
        var startDate = endDate.AddDays(-30); // Check last 30 days

        _logger.LogInformation("Starting data gap monitoring for last 30 days");

        foreach (var market in markets)
        {
            try
            {
                _logger.LogInformation("Checking gaps for market {Market}", market);

                var result = await dailyDataService.DetectAndFillGapsAsync(
                    market, startDate, endDate, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("Gap monitoring for {Market}: " +
                        "Detected: {Detected}, Filled: {Filled}, Failed: {Failed}",
                        market, result.GapsDetected, result.GapsFilled, result.GapsFailed);
                }
                else
                {
                    _logger.LogError("Gap monitoring failed for {Market}: {Error}",
                        market, result.ErrorMessage);
                }

                // Add delay between markets
                if (market != markets.Last())
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during gap monitoring for {Market}", market);
            }
        }
    }

    private DateTime CalculateNextGapCheckTime()
    {
        // Run every Sunday at 02:00 UTC
        var now = DateTime.UtcNow;
        var daysUntilSunday = (7 + (int)DayOfWeek.Sunday - (int)now.DayOfWeek) % 7;
        if (daysUntilSunday == 0 && now.Hour >= 2)
        {
            daysUntilSunday = 7; // Next Sunday
        }

        var targetTime = now.Date.AddDays(daysUntilSunday).AddHours(2);
        return targetTime;
    }
}

/// <summary>
/// Configuration for market schedule settings
/// </summary>
public class MarketScheduleConfiguration
{
    public bool EnableBistSync { get; set; } = true;
    public bool EnableUSMarketsSync { get; set; } = true;
    public bool EnableCryptoSync { get; set; } = true;
    public bool EnableGapMonitoring { get; set; } = true;

    public string BistSyncTime { get; set; } = "18:30"; // Turkey time
    public string USMarketsSyncTime { get; set; } = "16:30"; // ET time
    public string CryptoSyncTime { get; set; } = "00:01"; // UTC time

    public bool SendNotifications { get; set; } = true;
    public string? NotificationWebhookUrl { get; set; }
    public string? NotificationEmail { get; set; }

    public TimeSpan MaxSyncDuration { get; set; } = TimeSpan.FromHours(2);
    public int HealthCheckIntervalMinutes { get; set; } = 5;
}