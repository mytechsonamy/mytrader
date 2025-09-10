using MyTrader.Core.Models;

namespace MyTrader.Services.Backtesting;

public interface IBacktestService
{
    /// <summary>
    /// Run a backtest with the given configuration, checking for duplicates first
    /// </summary>
    Task<BacktestResults> RunBacktestAsync(BacktestConfiguration configuration, bool allowDuplicates = false);
    
    /// <summary>
    /// Find existing backtest results with the same configuration
    /// </summary>
    Task<BacktestResults?> FindExistingResultAsync(BacktestConfiguration configuration);
    
    /// <summary>
    /// Create or get existing configuration, handling deduplication
    /// </summary>
    Task<BacktestConfiguration> CreateOrGetConfigurationAsync(BacktestConfiguration configuration);
    
    /// <summary>
    /// Reproduce an existing backtest to verify results
    /// </summary>
    Task<BacktestResults> ReproduceBacktestAsync(Guid originalResultId);
    
    /// <summary>
    /// Compare two backtest results for differences
    /// </summary>
    Task<BacktestComparison> CompareResultsAsync(Guid result1Id, Guid result2Id);
    
    /// <summary>
    /// Get all configurations for a user/strategy/symbol combination
    /// </summary>
    Task<List<BacktestConfiguration>> GetConfigurationsAsync(Guid userId, Guid? strategyId = null, Guid? symbolId = null);
    
    /// <summary>
    /// Get backtest results with optional filtering
    /// </summary>
    Task<List<BacktestResults>> GetResultsAsync(Guid userId, Guid? configurationId = null, BacktestStatus? status = null);
}

public class BacktestComparison
{
    public BacktestResults Result1 { get; set; } = null!;
    public BacktestResults Result2 { get; set; } = null!;
    public Dictionary<string, object> Differences { get; set; } = new();
    public bool AreIdentical { get; set; }
    public double SimilarityScore { get; set; } // 0.0 to 1.0
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
}