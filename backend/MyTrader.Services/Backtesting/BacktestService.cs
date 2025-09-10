using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using System.Text.Json;

namespace MyTrader.Services.Backtesting;

public class BacktestService : IBacktestService
{
    private readonly TradingDbContext _context;
    private readonly ILogger<BacktestService> _logger;

    public BacktestService(TradingDbContext context, ILogger<BacktestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BacktestResults> RunBacktestAsync(BacktestConfiguration configuration, bool allowDuplicates = false)
    {
        try
        {
            // Generate hash for the configuration
            configuration.GenerateConfigurationHash();
            
            // Check for existing results if duplicates not allowed
            if (!allowDuplicates)
            {
                var existingResult = await FindExistingResultAsync(configuration);
                if (existingResult != null)
                {
                    _logger.LogInformation("Found existing backtest result {ResultId} for configuration hash {Hash}", 
                        existingResult.Id, configuration.ConfigurationHash);
                    return existingResult;
                }
            }
            
            // Create or get the configuration
            var savedConfig = await CreateOrGetConfigurationAsync(configuration);
            
            // Create new backtest result
            var result = new BacktestResults
            {
                UserId = configuration.UserId,
                StrategyId = configuration.StrategyId,
                SymbolId = configuration.SymbolId,
                ConfigurationId = savedConfig.Id,
                Timeframe = configuration.Timeframe,
                StartDate = configuration.StartDate,
                EndDate = configuration.EndDate,
                Status = BacktestStatus.Running,
                EngineVersion = "1.0.0",
                DataVersion = await GetDataVersionAsync(configuration.SymbolId, configuration.StartDate, configuration.EndDate)
            };
            
            _context.BacktestResults.Add(result);
            await _context.SaveChangesAsync();
            
            try
            {
                // TODO: Implement actual backtesting engine
                // For now, simulate backtest with mock data
                await SimulateBacktestAsync(result, savedConfig);
                
                result.Status = BacktestStatus.Completed;
                result.CompletedAt = DateTime.UtcNow;
                result.GenerateResultsHash();
                
                _logger.LogInformation("Backtest {ResultId} completed successfully", result.Id);
            }
            catch (Exception ex)
            {
                result.Status = BacktestStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                
                _logger.LogError(ex, "Backtest {ResultId} failed", result.Id);
            }
            
            await _context.SaveChangesAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running backtest for configuration");
            throw;
        }
    }

    public async Task<BacktestResults?> FindExistingResultAsync(BacktestConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.ConfigurationHash))
        {
            configuration.GenerateConfigurationHash();
        }
        
        var existingConfig = await _context.BacktestConfigurations
            .FirstOrDefaultAsync(c => c.ConfigurationHash == configuration.ConfigurationHash);
            
        if (existingConfig == null)
        {
            return null;
        }
        
        return await _context.BacktestResults
            .Where(r => r.ConfigurationId == existingConfig.Id && r.Status == BacktestStatus.Completed)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<BacktestConfiguration> CreateOrGetConfigurationAsync(BacktestConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.ConfigurationHash))
        {
            configuration.GenerateConfigurationHash();
        }
        
        var existingConfig = await _context.BacktestConfigurations
            .FirstOrDefaultAsync(c => c.ConfigurationHash == configuration.ConfigurationHash);
            
        if (existingConfig != null)
        {
            _logger.LogInformation("Found existing configuration {ConfigId} with hash {Hash}", 
                existingConfig.Id, configuration.ConfigurationHash);
            return existingConfig;
        }
        
        _context.BacktestConfigurations.Add(configuration);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created new configuration {ConfigId} with hash {Hash}", 
            configuration.Id, configuration.ConfigurationHash);
        
        return configuration;
    }

    public async Task<BacktestResults> ReproduceBacktestAsync(Guid originalResultId)
    {
        var originalResult = await _context.BacktestResults
            .Include(r => r.Configuration)
            .FirstOrDefaultAsync(r => r.Id == originalResultId);
            
        if (originalResult == null)
        {
            throw new ArgumentException($"Original backtest result {originalResultId} not found");
        }
        
        if (originalResult.Status != BacktestStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot reproduce incomplete backtest {originalResultId}");
        }
        
        // Create reproduction result
        var reproductionResult = new BacktestResults
        {
            UserId = originalResult.UserId,
            StrategyId = originalResult.StrategyId,
            SymbolId = originalResult.SymbolId,
            ConfigurationId = originalResult.ConfigurationId,
            Timeframe = originalResult.Timeframe,
            StartDate = originalResult.StartDate,
            EndDate = originalResult.EndDate,
            Status = BacktestStatus.Running,
            EngineVersion = "1.0.0",
            DataVersion = originalResult.DataVersion,
            IsReproduced = true,
            OriginalResultId = originalResultId
        };
        
        _context.BacktestResults.Add(reproductionResult);
        await _context.SaveChangesAsync();
        
        try
        {
            // Run the same backtest with same configuration
            await SimulateBacktestAsync(reproductionResult, originalResult.Configuration);
            
            reproductionResult.Status = BacktestStatus.Completed;
            reproductionResult.CompletedAt = DateTime.UtcNow;
            reproductionResult.GenerateResultsHash();
            
            // Verify reproduction accuracy
            var isIdentical = reproductionResult.HasSameResultsAs(originalResult);
            _logger.LogInformation("Reproduction {ReproductionId} of {OriginalId} - Identical: {IsIdentical}", 
                reproductionResult.Id, originalResultId, isIdentical);
                
        }
        catch (Exception ex)
        {
            reproductionResult.Status = BacktestStatus.Failed;
            reproductionResult.ErrorMessage = ex.Message;
            reproductionResult.CompletedAt = DateTime.UtcNow;
            
            _logger.LogError(ex, "Reproduction {ReproductionId} failed", reproductionResult.Id);
        }
        
        await _context.SaveChangesAsync();
        return reproductionResult;
    }

    public async Task<BacktestComparison> CompareResultsAsync(Guid result1Id, Guid result2Id)
    {
        var result1 = await _context.BacktestResults.FindAsync(result1Id);
        var result2 = await _context.BacktestResults.FindAsync(result2Id);
        
        if (result1 == null || result2 == null)
        {
            throw new ArgumentException("One or both backtest results not found");
        }
        
        var differences = new Dictionary<string, object>();
        var metricDifferences = new List<string>();
        
        // Compare key metrics
        CompareMetric("TotalReturnPercentage", result1.TotalReturnPercentage, result2.TotalReturnPercentage, differences, metricDifferences);
        CompareMetric("MaxDrawdownPercentage", result1.MaxDrawdownPercentage, result2.MaxDrawdownPercentage, differences, metricDifferences);
        CompareMetric("SharpeRatio", result1.SharpeRatio, result2.SharpeRatio, differences, metricDifferences);
        CompareMetric("TotalTrades", result1.TotalTrades, result2.TotalTrades, differences, metricDifferences);
        CompareMetric("WinRate", result1.WinRate, result2.WinRate, differences, metricDifferences);
        CompareMetric("ProfitFactor", result1.ProfitFactor, result2.ProfitFactor, differences, metricDifferences);
        
        var areIdentical = differences.Count == 0;
        var similarityScore = CalculateSimilarityScore(result1, result2);
        
        return new BacktestComparison
        {
            Result1 = result1,
            Result2 = result2,
            Differences = differences,
            AreIdentical = areIdentical,
            SimilarityScore = similarityScore
        };
    }

    public async Task<List<BacktestConfiguration>> GetConfigurationsAsync(Guid userId, Guid? strategyId = null, Guid? symbolId = null)
    {
        var query = _context.BacktestConfigurations
            .Where(c => c.UserId == userId);
            
        if (strategyId.HasValue)
        {
            query = query.Where(c => c.StrategyId == strategyId);
        }
        
        if (symbolId.HasValue)
        {
            query = query.Where(c => c.SymbolId == symbolId);
        }
        
        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<BacktestResults>> GetResultsAsync(Guid userId, Guid? configurationId = null, BacktestStatus? status = null)
    {
        var query = _context.BacktestResults
            .Where(r => r.UserId == userId);
            
        if (configurationId.HasValue)
        {
            query = query.Where(r => r.ConfigurationId == configurationId);
        }
        
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status);
        }
        
        return await query
            .Include(r => r.Configuration)
            .Include(r => r.Strategy)
            .Include(r => r.Symbol)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    private async Task SimulateBacktestAsync(BacktestResults result, BacktestConfiguration config)
    {
        // Simulate backtest execution with mock data
        await Task.Delay(100); // Simulate processing time
        
        var random = new Random();
        
        // Generate realistic mock results
        result.StartingCapital = config.InitialCapital;
        result.TotalReturn = (decimal)(random.NextDouble() * 2000 - 1000); // -1000 to +1000
        result.TotalReturnPercentage = result.TotalReturn / result.StartingCapital * 100;
        result.EndingCapital = result.StartingCapital + result.TotalReturn;
        result.MaxDrawdown = Math.Abs((decimal)(random.NextDouble() * 500)); // 0 to 500
        result.MaxDrawdownPercentage = result.MaxDrawdown / result.StartingCapital * 100;
        result.SharpeRatio = (decimal)(random.NextDouble() * 3 - 1); // -1 to 2
        result.SortinoRatio = result.SharpeRatio * (decimal)1.2;
        result.Volatility = (decimal)(random.NextDouble() * 30 + 10); // 10% to 40%
        
        result.TotalTrades = random.Next(10, 100);
        result.WinningTrades = random.Next(result.TotalTrades / 3, result.TotalTrades * 2 / 3);
        result.LosingTrades = result.TotalTrades - result.WinningTrades;
        result.WinRate = (decimal)result.WinningTrades / result.TotalTrades * 100;
        
        result.AverageWin = (decimal)(random.NextDouble() * 100 + 50); // 50 to 150
        result.AverageLoss = -(decimal)(random.NextDouble() * 80 + 30); // -30 to -110
        result.ProfitFactor = Math.Abs(result.AverageWin * result.WinningTrades / (result.AverageLoss * result.LosingTrades));
        
        result.TradingDays = (int)(result.EndDate - result.StartDate).TotalDays;
        result.AverageHoldingPeriod = (decimal)(random.NextDouble() * 48 + 2); // 2 to 50 hours
        
        result.TotalFees = result.TotalTrades * config.CommissionPercent / 100 * config.InitialCapital / 10;
        result.FeeImpactPercentage = result.TotalFees / result.StartingCapital * 100;
        
        result.PeakCapital = result.EndingCapital + result.MaxDrawdown;
        result.LowestCapital = result.EndingCapital - result.MaxDrawdown;
        
        // Store detailed results as JSON
        var detailedResults = new
        {
            trades = new List<object>(),
            equityCurve = new List<object>(),
            metrics = new
            {
                configuration = config.ConfigurationHash,
                executionTime = DateTime.UtcNow.Subtract(result.CreatedAt).TotalMilliseconds
            }
        };
        
        result.DetailedResults = JsonSerializer.Serialize(detailedResults);
        result.StrategyConfig = config.StrategyParameters;
    }

    private async Task<string> GetDataVersionAsync(Guid symbolId, DateTime startDate, DateTime endDate)
    {
        // Calculate a version based on available market data
        var dataCount = await _context.Candles
            .Where(c => c.SymbolId == symbolId && c.Timestamp >= startDate && c.Timestamp <= endDate)
            .CountAsync();
            
        return $"v1.0-{dataCount}-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";
    }

    private void CompareMetric<T>(string name, T value1, T value2, Dictionary<string, object> differences, List<string> metricDifferences) where T : IComparable<T>
    {
        if (value1.CompareTo(value2) != 0)
        {
            differences[name] = new { Result1 = value1, Result2 = value2 };
            metricDifferences.Add(name);
        }
    }

    private double CalculateSimilarityScore(BacktestResults result1, BacktestResults result2)
    {
        var metrics = new (decimal val1, decimal val2, double weight)[]
        {
            (result1.TotalReturnPercentage, result2.TotalReturnPercentage, 0.25),
            (result1.MaxDrawdownPercentage, result2.MaxDrawdownPercentage, 0.2),
            (result1.SharpeRatio, result2.SharpeRatio, 0.2),
            (result1.WinRate, result2.WinRate, 0.15),
            (result1.ProfitFactor, result2.ProfitFactor, 0.2)
        };
        
        double totalScore = 0;
        double totalWeight = 0;
        
        foreach (var (val1, val2, weight) in metrics)
        {
            if (val1 != 0 || val2 != 0)
            {
                var diff = Math.Abs((double)(val1 - val2));
                var avg = Math.Abs((double)(val1 + val2)) / 2;
                var similarity = Math.Max(0, 1 - (diff / Math.Max(avg, 1)));
                totalScore += similarity * weight;
                totalWeight += weight;
            }
        }
        
        return totalWeight > 0 ? totalScore / totalWeight : 0;
    }
}