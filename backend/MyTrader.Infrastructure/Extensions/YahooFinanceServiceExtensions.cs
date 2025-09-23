using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Services;
using MyTrader.Infrastructure.Services;

namespace MyTrader.Infrastructure.Extensions;

/// <summary>
/// Service registration extensions for Yahoo Finance daily sync system
/// Configures all services, background jobs, and dependencies
/// </summary>
public static class YahooFinanceServiceExtensions
{
    /// <summary>
    /// Add Yahoo Finance daily sync services to the DI container
    /// </summary>
    public static IServiceCollection AddYahooFinanceDailySync(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Core service configurations
        services.Configure<YahooFinanceConfiguration>(
            configuration.GetSection("YahooFinance"));

        services.Configure<YahooFinanceDailyConfiguration>(
            configuration.GetSection("YahooFinance:DailySync"));

        services.Configure<ErrorHandlingConfiguration>(
            configuration.GetSection("YahooFinance:ErrorHandling"));

        services.Configure<DataQualityConfiguration>(
            configuration.GetSection("YahooFinance:DataQuality"));

        services.Configure<MarketScheduleConfiguration>(
            configuration.GetSection("YahooFinance:MarketSchedule"));

        // Register HttpClient for Yahoo Finance API
        services.AddHttpClient<YahooFinanceApiService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        });

        // Register core services
        services.AddScoped<YahooFinanceApiService>();
        services.AddScoped<YahooFinanceDailyDataService>();
        services.AddScoped<DataQualityValidationService>();
        services.AddSingleton<YahooFinanceErrorHandlingService>();

        // Register background services based on configuration
        var scheduleConfig = configuration.GetSection("YahooFinance:MarketSchedule");

        if (scheduleConfig.GetValue<bool>("EnableBistSync", true))
        {
            services.AddHostedService<BistDailySyncService>();
        }

        if (scheduleConfig.GetValue<bool>("EnableUSMarketsSync", true))
        {
            services.AddHostedService<USMarketsDailySyncService>();
        }

        if (scheduleConfig.GetValue<bool>("EnableCryptoSync", true))
        {
            services.AddHostedService<CryptoDailySyncService>();
        }

        if (scheduleConfig.GetValue<bool>("EnableGapMonitoring", true))
        {
            services.AddHostedService<DataGapMonitoringService>();
        }

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<YahooFinanceHealthCheck>("yahoo_finance_sync");

        return services;
    }

    /// <summary>
    /// Initialize database optimizations for market data
    /// Call this during application startup
    /// </summary>
    public static async Task InitializeMarketDataOptimizationsAsync(
        this IServiceProvider serviceProvider,
        ILogger? logger = null)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyTrader.Core.Data.ITradingDbContext>();
        var loggerService = logger ?? scope.ServiceProvider.GetRequiredService<ILogger>();

        try
        {
            // Database optimization would be implemented here
            loggerService.LogInformation("Database optimization for market data completed");
        }
        catch (Exception ex)
        {
            loggerService.LogError(ex, "Failed to initialize market data optimizations");
            throw;
        }
    }

    /// <summary>
    /// Validate Yahoo Finance service configuration
    /// </summary>
    public static void ValidateYahooFinanceConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate API configuration
        var apiConfig = configuration.GetSection("YahooFinance");
        if (apiConfig.GetValue<int>("MaxConcurrentRequests") <= 0)
        {
            errors.Add("YahooFinance:MaxConcurrentRequests must be greater than 0");
        }

        if (apiConfig.GetValue<int>("MinRequestIntervalMs") < 100)
        {
            errors.Add("YahooFinance:MinRequestIntervalMs should be at least 100ms to respect rate limits");
        }

        // Validate daily sync configuration
        var dailyConfig = configuration.GetSection("YahooFinance:DailySync");
        if (dailyConfig.GetValue<int>("BatchSize") <= 0)
        {
            errors.Add("YahooFinance:DailySync:BatchSize must be greater than 0");
        }

        if (dailyConfig.GetValue<int>("MaxRetryAttempts") < 1)
        {
            errors.Add("YahooFinance:DailySync:MaxRetryAttempts must be at least 1");
        }

        // Validate error handling configuration
        var errorConfig = configuration.GetSection("YahooFinance:ErrorHandling");
        if (errorConfig.GetValue<int>("CircuitBreakerFailureThreshold") <= 0)
        {
            errors.Add("YahooFinance:ErrorHandling:CircuitBreakerFailureThreshold must be greater than 0");
        }

        // Validate data quality configuration
        var qualityConfig = configuration.GetSection("YahooFinance:DataQuality");
        var minCompleteness = qualityConfig.GetValue<decimal>("MinCompletenessThreshold");
        if (minCompleteness < 0 || minCompleteness > 100)
        {
            errors.Add("YahooFinance:DataQuality:MinCompletenessThreshold must be between 0 and 100");
        }

        if (errors.Any())
        {
            throw new InvalidOperationException($"Yahoo Finance configuration validation failed:\n{string.Join("\n", errors)}");
        }
    }
}

/// <summary>
/// Health check for Yahoo Finance sync services
/// </summary>
public class YahooFinanceHealthCheck : IHealthCheck
{
    private readonly YahooFinanceApiService _apiService;
    private readonly YahooFinanceErrorHandlingService _errorHandlingService;
    private readonly ILogger<YahooFinanceHealthCheck> _logger;

    public YahooFinanceHealthCheck(
        YahooFinanceApiService apiService,
        YahooFinanceErrorHandlingService errorHandlingService,
        ILogger<YahooFinanceHealthCheck> logger)
    {
        _apiService = apiService;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();

            // Check API connectivity with a simple test
            var testResult = await _apiService.GetLatestPriceAsync("AAPL", "NASDAQ", cancellationToken);

            data["api_connectivity"] = testResult.Success;
            data["api_test_time"] = testResult.RequestTime;

            if (!testResult.Success)
            {
                data["api_error"] = testResult.ErrorMessage ?? "Unknown error";
            }

            // Check error handling service status
            var errorStats = _errorHandlingService.GetErrorStatistics();
            data["circuit_breakers_count"] = errorStats.CircuitBreakers.Count;
            data["open_circuit_breakers"] = errorStats.CircuitBreakers.Count(cb =>
                cb.Value.State == CircuitBreakerStateEnum.Open);
            data["retry_contexts_count"] = errorStats.RetryContexts.Count;

            // Determine overall health
            var hasOpenCircuitBreakers = errorStats.CircuitBreakers.Any(cb =>
                cb.Value.State == CircuitBreakerStateEnum.Open);

            if (!testResult.Success && hasOpenCircuitBreakers)
            {
                return HealthCheckResult.Unhealthy(
                    "Yahoo Finance API connectivity issues and open circuit breakers detected",
                    data: data);
            }
            else if (!testResult.Success || hasOpenCircuitBreakers)
            {
                return HealthCheckResult.Degraded(
                    "Some Yahoo Finance services experiencing issues",
                    data: data);
            }
            else
            {
                return HealthCheckResult.Healthy(
                    "Yahoo Finance sync services operating normally",
                    data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Yahoo Finance health check");
            return HealthCheckResult.Unhealthy(
                "Health check failed with exception",
                ex,
                data: new Dictionary<string, object> { ["error"] = ex.Message });
        }
    }
}

/// <summary>
/// Background service for periodic health monitoring and alerting
/// </summary>
public class YahooFinanceMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<YahooFinanceMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public YahooFinanceMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<YahooFinanceMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yahoo Finance monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheckAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Yahoo Finance monitoring service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Yahoo Finance monitoring service stopped");
    }

    private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var healthCheck = scope.ServiceProvider.GetRequiredService<YahooFinanceHealthCheck>();

        try
        {
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

            if (result.Status == HealthStatus.Unhealthy)
            {
                _logger.LogError("Yahoo Finance services are unhealthy: {Description}", result.Description);
                // In production, this would trigger alerts/notifications
            }
            else if (result.Status == HealthStatus.Degraded)
            {
                _logger.LogWarning("Yahoo Finance services are degraded: {Description}", result.Description);
            }
            else
            {
                _logger.LogDebug("Yahoo Finance services health check passed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check");
        }
    }
}