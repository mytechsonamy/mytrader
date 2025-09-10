using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.Services;
using MyTrader.Infrastructure.Data;

namespace MyTrader.BacktestRunner;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Starting MyTrader Backtest Runner...");
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Build service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add database
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Port=5434;Database=mytrader;Username=mytrader_user;Password=mytrader_dev_password";
            
        services.AddDbContext<TradingDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add our services
        services.AddScoped<IIndicatorCalculator, IndicatorCalculator>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<IBacktestEngine, BacktestEngine>();
        services.AddScoped<IStrategyManagementService, StrategyManagementService>();
        services.AddScoped<IPerformanceTrackingService, PerformanceTrackingService>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var strategyService = scope.ServiceProvider.GetRequiredService<IStrategyManagementService>();
            var backtestEngine = scope.ServiceProvider.GetRequiredService<IBacktestEngine>();
            
            logger.LogInformation("Connected to database successfully");
            
            // Get all active symbols
            var symbols = await context.Symbols
                .Where(s => s.IsActive && s.IsTracked)
                .ToListAsync();
                
            logger.LogInformation("Found {Count} symbols to process", symbols.Count);
            
            foreach (var symbol in symbols)
            {
                logger.LogInformation("Processing symbol: {Symbol} ({Id})", symbol.Ticker, symbol.Id);
                
                try
                {
                    // Check if we have sufficient data
                    var dataCount = await context.MarketData
                        .CountAsync(md => md.Symbol == symbol.Ticker && md.Timeframe == "1h");
                        
                    logger.LogInformation("Found {Count} 1h candles for {Symbol}", dataCount, symbol.Ticker);
                    
                    if (dataCount < 100)
                    {
                        logger.LogWarning("Insufficient data for {Symbol}, skipping", symbol.Ticker);
                        continue;
                    }
                    
                    // Define optimization parameters
                    var optimizationRequest = new OptimizationRequest
                    {
                        UserId = Guid.Empty, // System user
                        StrategyId = Guid.Empty,
                        SymbolId = symbol.Id,
                        ConfigurationId = Guid.NewGuid(),
                        StartDate = DateTime.UtcNow.AddDays(-7), // Use available data
                        EndDate = DateTime.UtcNow.AddHours(-1),
                        Timeframe = "1h",
                        InitialBalance = 10000m,
                        ParameterRanges = new Dictionary<string, ParameterRange>
                        {
                            ["RSIPeriod"] = new() { Min = 10, Max = 20, Step = 5 },
                            ["MACDFast"] = new() { Min = 8, Max = 16, Step = 4 },
                            ["MACDSlow"] = new() { Min = 20, Max = 30, Step = 5 },
                            ["BBPeriod"] = new() { Min = 15, Max = 25, Step = 5 }
                        }
                    };
                    
                    logger.LogInformation("Starting optimization for {Symbol}...", symbol.Ticker);
                    
                    // Run optimization
                    var optimizationResults = await backtestEngine.RunOptimizationAsync(optimizationRequest);
                    
                    logger.LogInformation("Optimization completed. Generated {Count} results for {Symbol}", 
                        optimizationResults.Count, symbol.Ticker);
                    
                    if (optimizationResults.Any())
                    {
                        // Create/update default strategy
                        var strategy = await strategyService.CreateDefaultStrategyAsync(symbol.Id, optimizationResults);
                        
                        logger.LogInformation("Created/updated default strategy {StrategyId} for {Symbol} with Sharpe ratio {SharpeRatio:F2}", 
                            strategy.Id, symbol.Ticker, strategy.PerformanceScore);
                    }
                    else
                    {
                        logger.LogWarning("No optimization results generated for {Symbol}", symbol.Ticker);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing symbol {Symbol}", symbol.Ticker);
                }
            }
            
            logger.LogInformation("✅ Backtest runner completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error in backtest runner");
        }
    }
}