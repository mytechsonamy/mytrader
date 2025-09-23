using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.Data;
using System.Text.Json;

namespace MyTrader.Core.Services;

public interface IStrategyManagementService
{
    Task<List<Strategy>> GetDefaultStrategiesAsync();
    Task<Strategy> CreateDefaultStrategyAsync(Guid symbolId, List<BacktestResults> optimizationResults);
    Task<Strategy> GetBestStrategyForSymbolAsync(Guid symbolId);
    Task<UserStrategy> CreateUserStrategyAsync(Guid userId, Guid strategyId, StrategyParameters? customParameters = null);
    Task<List<UserStrategy>> GetUserStrategiesAsync(Guid userId);
    Task UpdateDefaultStrategiesAsync();
    Task<bool> IsStrategyPerformingWellAsync(Guid strategyId, int daysPeriod = 30);
}

public class StrategyManagementService : IStrategyManagementService
{
    private readonly ITradingDbContext _context;
    private readonly IBacktestEngine _backtestEngine;
    private readonly ILogger<StrategyManagementService> _logger;

    public StrategyManagementService(
        ITradingDbContext context,
        IBacktestEngine backtestEngine,
        ILogger<StrategyManagementService> logger)
    {
        _context = context;
        _backtestEngine = backtestEngine;
        _logger = logger;
    }

    public async Task<List<Strategy>> GetDefaultStrategiesAsync()
    {
        return await _context.Strategies
            .Where(s => s.IsDefault && s.IsActive)
            .OrderByDescending(s => s.PerformanceScore)
            .ToListAsync();
    }

    public async Task<Strategy> CreateDefaultStrategyAsync(Guid symbolId, List<BacktestResults> optimizationResults)
    {
        _logger.LogInformation("Creating default strategy for symbol {SymbolId} from {Count} optimization results", 
            symbolId, optimizationResults.Count);

        // Find the best performing strategy based on Sharpe ratio and return
        var bestResult = optimizationResults
            .Where(r => r.TotalTrades >= 10 && r.WinRate >= 30) // Minimum thresholds
            .OrderByDescending(r => r.SharpeRatio)
            .ThenByDescending(r => r.TotalReturnPercentage)
            .FirstOrDefault();

        if (bestResult == null)
        {
            throw new InvalidOperationException("No suitable strategy found in optimization results");
        }

        // Check if we already have a default strategy for this symbol
        var existingStrategy = await _context.Strategies
            .FirstOrDefaultAsync(s => s.SymbolId == symbolId && s.IsDefault);

        if (existingStrategy != null)
        {
            // Update existing strategy if new one is better
            if (bestResult.SharpeRatio > existingStrategy.PerformanceScore)
            {
                existingStrategy.Parameters = bestResult.StrategyConfig ?? JsonSerializer.Serialize(new StrategyParameters());
                existingStrategy.PerformanceScore = bestResult.SharpeRatio;
                existingStrategy.Description = $"Multi-indicator strategy optimized for {await GetSymbolName(symbolId)}";
                existingStrategy.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated existing default strategy for symbol {SymbolId} with Sharpe ratio {SharpeRatio}", 
                    symbolId, bestResult.SharpeRatio);

                return existingStrategy;
            }
            else
            {
                _logger.LogInformation("Existing strategy for symbol {SymbolId} is still better, keeping current", symbolId);
                return existingStrategy;
            }
        }

        // Create new default strategy
        var strategy = new Strategy
        {
            Id = Guid.NewGuid(),
            Name = $"Optimized Multi-Indicator Strategy",
            Description = $"Multi-indicator strategy optimized for {await GetSymbolName(symbolId)}",
            SymbolId = symbolId,
            Parameters = bestResult.StrategyConfig ?? JsonSerializer.Serialize(new StrategyParameters()),
            IsDefault = true,
            IsActive = true,
            PerformanceScore = bestResult.SharpeRatio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Strategies.Add(strategy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new default strategy {StrategyId} for symbol {SymbolId} with Sharpe ratio {SharpeRatio}", 
            strategy.Id, symbolId, bestResult.SharpeRatio);

        return strategy;
    }

    public async Task<Strategy> GetBestStrategyForSymbolAsync(Guid symbolId)
    {
        var strategy = await _context.Strategies
            .Where(s => s.SymbolId == symbolId && s.IsDefault && s.IsActive)
            .OrderByDescending(s => s.PerformanceScore)
            .FirstOrDefaultAsync();

        if (strategy == null)
        {
            throw new InvalidOperationException($"No default strategy found for symbol {symbolId}");
        }

        return strategy;
    }

    public async Task<UserStrategy> CreateUserStrategyAsync(Guid userId, Guid strategyId, StrategyParameters? customParameters = null)
    {
        var strategy = await _context.Strategies.FindAsync(strategyId);
        if (strategy == null)
        {
            throw new ArgumentException($"Strategy {strategyId} not found");
        }

        // Check if user already has this strategy
        var existingUserStrategy = await _context.UserStrategies
            .FirstOrDefaultAsync(us => us.UserId == userId && us.TemplateId == strategyId);

        if (existingUserStrategy != null)
        {
            if (customParameters != null)
            {
                existingUserStrategy.Parameters = JsonDocument.Parse(JsonSerializer.Serialize(customParameters));
                await _context.SaveChangesAsync();
            }
            
            return existingUserStrategy;
        }

        var userStrategy = new UserStrategy
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TemplateId = strategyId,
            IsActive = true,
            Parameters = customParameters != null ? JsonDocument.Parse(JsonSerializer.Serialize(customParameters)) : JsonDocument.Parse("{}"),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.UserStrategies.Add(userStrategy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user strategy {UserStrategyId} for user {UserId} with strategy {StrategyId}", 
            userStrategy.Id, userId, strategyId);

        return userStrategy;
    }

    public async Task<List<UserStrategy>> GetUserStrategiesAsync(Guid userId)
    {
        return await _context.UserStrategies
            .Where(us => us.UserId == userId && us.IsActive)
            .OrderByDescending(us => us.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateDefaultStrategiesAsync()
    {
        _logger.LogInformation("Starting daily update of default strategies");

        // Get all symbols that have sufficient data
        var symbols = await _context.Symbols
            .Where(s => s.IsActive && s.IsTracked)
            .ToListAsync();

        var updateTasks = symbols.Select(async symbol =>
        {
            try
            {
                await UpdateStrategyForSymbol(symbol.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update strategy for symbol {SymbolId}", symbol.Id);
            }
        });

        await Task.WhenAll(updateTasks);

        _logger.LogInformation("Completed daily update of default strategies");
    }

    public async Task<bool> IsStrategyPerformingWellAsync(Guid strategyId, int daysPeriod = 30)
    {
        var strategy = await _context.Strategies.FindAsync(strategyId);
        if (strategy == null) return false;

        // Check recent backtest results
        var recentResults = await _context.BacktestResults
            .Where(br => br.StrategyId == strategyId && 
                        br.CreatedAt >= DateTime.UtcNow.AddDays(-daysPeriod) &&
                        br.Status == "Completed")
            .ToListAsync();

        if (!recentResults.Any()) return false;

        var avgSharpeRatio = recentResults.Average(r => r.SharpeRatio);
        var avgWinRate = recentResults.Average(r => r.WinRate);
        var avgReturn = recentResults.Average(r => r.TotalReturnPercentage);

        // Define performance thresholds
        return avgSharpeRatio > 1.0m && avgWinRate > 40m && avgReturn > 5m;
    }

    private async Task UpdateStrategyForSymbol(Guid symbolId)
    {
        _logger.LogInformation("Updating strategy for symbol {SymbolId}", symbolId);

        // Define optimization parameters
        var optimizationRequest = new OptimizationRequest
        {
            UserId = Guid.Empty, // System user
            StrategyId = Guid.Empty, // Will be set after strategy creation
            SymbolId = symbolId,
            ConfigurationId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(-365), // 1 year of data
            EndDate = DateTime.UtcNow.AddDays(-1),
            Timeframe = "1h",
            InitialBalance = 10000m,
            ParameterRanges = new Dictionary<string, ParameterRange>
            {
                ["RSIPeriod"] = new() { Min = 10, Max = 20, Step = 2 },
                ["MACDFast"] = new() { Min = 8, Max = 16, Step = 2 },
                ["MACDSlow"] = new() { Min = 20, Max = 30, Step = 2 },
                ["BBPeriod"] = new() { Min = 15, Max = 25, Step = 2 }
            }
        };

        // Run optimization
        var optimizationResults = await _backtestEngine.RunOptimizationAsync(optimizationRequest);

        if (optimizationResults.Any())
        {
            await CreateDefaultStrategyAsync(symbolId, optimizationResults);
        }
        else
        {
            _logger.LogWarning("No optimization results for symbol {SymbolId}", symbolId);
        }
    }

    private async Task<string> GetSymbolName(Guid symbolId)
    {
        var symbol = await _context.Symbols.FindAsync(symbolId);
        return symbol?.Ticker ?? "Unknown";
    }
}