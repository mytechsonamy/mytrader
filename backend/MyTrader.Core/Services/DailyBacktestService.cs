using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Models;
using MyTrader.Core.Data;

namespace MyTrader.Core.Services;

public class DailyBacktestService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyBacktestService> _logger;
    private Timer? _timer;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24); // Run daily
    private readonly TimeSpan _initialDelay;

    public DailyBacktestService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyBacktestService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Calculate delay until 6 AM UTC next day
        var now = DateTime.UtcNow;
        var nextRun = DateTime.Today.AddDays(1).AddHours(6); // 6 AM UTC tomorrow
        if (now.Hour < 6) // If before 6 AM today, run at 6 AM today
        {
            nextRun = DateTime.Today.AddHours(6);
        }
        _initialDelay = nextRun - now;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Daily Backtest Service starting. Next run in {Delay}", _initialDelay);
        
        _timer = new Timer(RunDailyBacktest, null, _initialDelay, _runInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Daily Backtest Service stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void RunDailyBacktest(object? state)
    {
        using var scope = _scopeFactory.CreateScope();
        var strategyManagementService = scope.ServiceProvider.GetRequiredService<IStrategyManagementService>();
        var backtestEngine = scope.ServiceProvider.GetRequiredService<IBacktestEngine>();
        var performanceTracker = scope.ServiceProvider.GetRequiredService<IPerformanceTrackingService>();

        try
        {
            _logger.LogInformation("Starting daily backtest automation at {Time}", DateTime.UtcNow);

            // Update default strategies with new optimizations
            await strategyManagementService.UpdateDefaultStrategiesAsync();

            // Track performance of existing strategies
            await performanceTracker.UpdateDailyPerformanceAsync();

            // Send summary report
            await GenerateDailyReport(scope.ServiceProvider);

            _logger.LogInformation("Daily backtest automation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during daily backtest automation");
        }
    }

    private async Task GenerateDailyReport(IServiceProvider serviceProvider)
    {
        try
        {
            var strategyService = serviceProvider.GetRequiredService<IStrategyManagementService>();
            var performanceService = serviceProvider.GetRequiredService<IPerformanceTrackingService>();

            var strategies = await strategyService.GetDefaultStrategiesAsync();
            var performanceReport = await performanceService.GenerateDailyReportAsync();

            _logger.LogInformation("Daily Report Generated: {StrategyCount} active strategies, Average Performance: {AvgPerformance:F2}%", 
                strategies.Count, performanceReport.AveragePerformance);

            // Here you could send email notifications, Slack messages, etc.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate daily report");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

public interface IPerformanceTrackingService
{
    Task UpdateDailyPerformanceAsync();
    Task<PerformanceReport> GenerateDailyReportAsync();
    Task<List<StrategyPerformance>> GetTopPerformingStrategiesAsync(int count = 10);
    Task<List<StrategyPerformance>> GetUnderPerformingStrategiesAsync(decimal threshold = -5.0m);
}

public class PerformanceTrackingService : IPerformanceTrackingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PerformanceTrackingService> _logger;

    public PerformanceTrackingService(
        IServiceScopeFactory scopeFactory,
        ILogger<PerformanceTrackingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task UpdateDailyPerformanceAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

        try
        {
            var strategies = await context.Strategies
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var strategy in strategies)
            {
                await UpdateStrategyPerformance(strategy, context);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Updated performance for {Count} strategies", strategies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update daily performance");
            throw;
        }
    }

    public async Task<PerformanceReport> GenerateDailyReportAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

        var recentResults = await context.BacktestResults
            .Where(br => br.CreatedAt >= DateTime.UtcNow.AddDays(-7) && br.Status == "Completed")
            .ToListAsync();

        var report = new PerformanceReport
        {
            Date = DateTime.UtcNow.Date,
            TotalStrategies = await context.Strategies.CountAsync(s => s.IsActive),
            ActiveBacktests = recentResults.Count,
            AveragePerformance = recentResults.Any() ? recentResults.Average(r => r.TotalReturnPercentage) : 0,
            BestPerformingStrategy = recentResults.Any() 
                ? recentResults.OrderByDescending(r => r.TotalReturnPercentage).First().StrategyId 
                : null,
            TotalTrades = recentResults.Sum(r => r.TotalTrades),
            OverallWinRate = recentResults.Any() ? recentResults.Average(r => r.WinRate) : 0
        };

        return report;
    }

    public async Task<List<StrategyPerformance>> GetTopPerformingStrategiesAsync(int count = 10)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

        var topStrategies = await context.BacktestResults
            .Where(br => br.CreatedAt >= DateTime.UtcNow.AddDays(-30) && br.Status == "Completed")
            .GroupBy(br => br.StrategyId)
            .Select(g => new StrategyPerformance
            {
                StrategyId = g.Key,
                AverageReturn = g.Average(x => x.TotalReturnPercentage),
                AverageSharpeRatio = g.Average(x => x.SharpeRatio),
                AverageWinRate = g.Average(x => x.WinRate),
                TotalTrades = g.Sum(x => x.TotalTrades),
                BacktestCount = g.Count()
            })
            .OrderByDescending(sp => sp.AverageSharpeRatio)
            .Take(count)
            .ToListAsync();

        return topStrategies;
    }

    public async Task<List<StrategyPerformance>> GetUnderPerformingStrategiesAsync(decimal threshold = -5.0m)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ITradingDbContext>();

        var underPerforming = await context.BacktestResults
            .Where(br => br.CreatedAt >= DateTime.UtcNow.AddDays(-30) && 
                        br.Status == "Completed" && 
                        br.TotalReturnPercentage < threshold)
            .GroupBy(br => br.StrategyId)
            .Select(g => new StrategyPerformance
            {
                StrategyId = g.Key,
                AverageReturn = g.Average(x => x.TotalReturnPercentage),
                AverageSharpeRatio = g.Average(x => x.SharpeRatio),
                AverageWinRate = g.Average(x => x.WinRate),
                TotalTrades = g.Sum(x => x.TotalTrades),
                BacktestCount = g.Count()
            })
            .ToListAsync();

        return underPerforming;
    }

    private async Task UpdateStrategyPerformance(Strategy strategy, ITradingDbContext context)
    {
        var recentResults = await context.BacktestResults
            .Where(br => br.StrategyId == strategy.Id && 
                        br.CreatedAt >= DateTime.UtcNow.AddDays(-30) &&
                        br.Status == "Completed")
            .ToListAsync();

        if (recentResults.Any())
        {
            var avgSharpe = recentResults.Average(r => r.SharpeRatio);
            var avgReturn = recentResults.Average(r => r.TotalReturnPercentage);
            var avgWinRate = recentResults.Average(r => r.WinRate);

            // Update strategy performance score (weighted combination)
            var performanceScore = (avgSharpe * 0.5m) + (avgReturn * 0.3m) + (avgWinRate * 0.2m);
            
            if (Math.Abs((strategy.PerformanceScore ?? 0) - performanceScore) > 0.1m)
            {
                strategy.PerformanceScore = performanceScore;
                strategy.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Updated performance score for strategy {StrategyId} to {Score:F2}", 
                    strategy.Id, performanceScore);
            }
        }
    }
}

// Supporting classes
public class PerformanceReport
{
    public DateTime Date { get; set; }
    public int TotalStrategies { get; set; }
    public int ActiveBacktests { get; set; }
    public decimal AveragePerformance { get; set; }
    public Guid? BestPerformingStrategy { get; set; }
    public int TotalTrades { get; set; }
    public decimal OverallWinRate { get; set; }
}

public class StrategyPerformance
{
    public Guid StrategyId { get; set; }
    public decimal AverageReturn { get; set; }
    public decimal AverageSharpeRatio { get; set; }
    public decimal AverageWinRate { get; set; }
    public int TotalTrades { get; set; }
    public int BacktestCount { get; set; }
}