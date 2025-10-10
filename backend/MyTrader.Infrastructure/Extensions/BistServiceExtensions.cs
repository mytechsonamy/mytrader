using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Interfaces;
using MyTrader.Infrastructure.Services;

namespace MyTrader.Infrastructure.Extensions;

/// <summary>
/// Service registration extensions for BIST market data services
/// </summary>
public static class BistServiceExtensions
{
    /// <summary>
    /// Register BIST market data services
    /// </summary>
    public static IServiceCollection AddBistServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register BIST configuration
        services.Configure<BistConfiguration>(
            configuration.GetSection("BistConfiguration"));

        // Register BIST market data service
        services.AddScoped<IBistMarketDataService, BistMarketDataService>();

        // Register background service for cache refresh
        services.AddHostedService<BistDataRefreshService>();

        return services;
    }
}

/// <summary>
/// Background service for automatic BIST data refresh
/// Ensures cache stays warm and data is up-to-date
/// </summary>
public class BistDataRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BistDataRefreshService> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);

    public BistDataRefreshService(
        IServiceProvider serviceProvider,
        ILogger<BistDataRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BIST data refresh service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshBistData(stoppingToken);
                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BIST data refresh service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait longer on error
            }
        }

        _logger.LogInformation("BIST data refresh service stopped");
    }

    private async Task RefreshBistData(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var bistService = scope.ServiceProvider.GetRequiredService<IBistMarketDataService>();

        try
        {
            // Only refresh during market hours or within 1 hour after close
            if (await IsMarketActiveTime(bistService, cancellationToken))
            {
                var result = await bistService.RefreshBistDataAsync(cancellationToken);

                if (result.Success)
                {
                    _logger.LogDebug("BIST data refreshed: {SymbolCount} symbols in {Duration}ms",
                        result.SymbolsUpdated, result.RefreshDuration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("BIST data refresh failed: {Message}", result.Message);
                }
            }
            else
            {
                _logger.LogDebug("Skipping BIST refresh - market closed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh BIST data");
        }
    }

    private async Task<bool> IsMarketActiveTime(IBistMarketDataService bistService, CancellationToken cancellationToken)
    {
        try
        {
            var marketStatus = await bistService.GetBistMarketStatusAsync(cancellationToken);

            // Refresh during market hours or within 1 hour after close
            var now = DateTime.UtcNow;
            var istanbulTime = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));

            // Market hours: 9:30 AM - 6:00 PM Istanbul time
            var isMarketHours = istanbulTime.DayOfWeek >= DayOfWeek.Monday &&
                               istanbulTime.DayOfWeek <= DayOfWeek.Friday &&
                               istanbulTime.TimeOfDay >= TimeSpan.FromHours(9.5) &&
                               istanbulTime.TimeOfDay <= TimeSpan.FromHours(19); // 1 hour after close

            return isMarketHours;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking market hours");
            return true; // Default to refreshing on error
        }
    }
}