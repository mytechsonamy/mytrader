using MyTrader.Core.Models;

// Note: BacktestResults class already exists in MyTrader.Core.Models
// We use that existing model instead of creating a duplicate

namespace MyTrader.Core.DTOs.Backtesting;

/// <summary>
/// Request for running a single backtest
/// </summary>
public class BacktestRequest
{
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; } = 10000m;
    public StrategyParameters Parameters { get; set; } = new();
    public RiskParameters RiskParameters { get; set; } = new();
}

// BacktestResults class already exists in MyTrader.Core.Models - using that instead

/// <summary>
/// Request for parameter optimization
/// </summary>
public class OptimizationRequest
{
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; } = 10000m;
    public Dictionary<string, object> ParameterRanges { get; set; } = new();
    public string OptimizationCriteria { get; set; } = "TotalReturn";
}

/// <summary>
/// Results from parameter optimization
/// </summary>
public class OptimizationResults
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public OptimizationRequest Request { get; set; } = new();
    public List<BacktestResults> Results { get; set; } = new();
    public BacktestResults BestResult { get; set; } = new();
    public Dictionary<string, object> OptimalParameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request for walk-forward analysis
/// </summary>
public class WalkForwardRequest
{
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; } = 10000m;
    public TimeSpan WindowSize { get; set; }
    public TimeSpan StepSize { get; set; }
    public StrategyParameters Parameters { get; set; } = new();
}

/// <summary>
/// Results from walk-forward analysis
/// </summary>
public class WalkForwardResults
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public WalkForwardRequest Request { get; set; } = new();
    public List<BacktestResults> WindowResults { get; set; } = new();
    public PerformanceMetrics AggregateMetrics { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request for Monte Carlo simulation
/// </summary>
public class MonteCarloRequest
{
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; } = 10000m;
    public int Simulations { get; set; } = 1000;
    public StrategyParameters Parameters { get; set; } = new();
}

/// <summary>
/// Results from Monte Carlo simulation
/// </summary>
public class MonteCarloResults
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MonteCarloRequest Request { get; set; } = new();
    public List<BacktestResults> SimulationResults { get; set; } = new();
    public decimal MeanReturn { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal WorstCase { get; set; }
    public decimal BestCase { get; set; }
    public decimal ValueAtRisk95 { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request for parallel backtesting
/// </summary>
public class ParallelBacktestRequest
{
    public List<BacktestRequest> Requests { get; set; } = new();
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
}

/// <summary>
/// Results from parallel backtesting
/// </summary>
public class ParallelBacktestResults
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<BacktestResults> Results { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detailed backtest results with additional analysis
/// </summary>
public class DetailedBacktestResults : BacktestResults
{
    public List<TradeAnalysis> TradeAnalyses { get; set; } = new();
    public DrawdownAnalysis DrawdownAnalysis { get; set; } = new();
    public RiskAdjustedMetrics RiskAdjustedMetrics { get; set; } = new();
    public List<PerformancePeriod> PerformancePeriods { get; set; } = new();
}

/// <summary>
/// Request for strategy comparison
/// </summary>
public class StrategyComparisonRequest
{
    public List<Guid> StrategyIds { get; set; } = new();
    public string Symbol { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; } = 10000m;
}

/// <summary>
/// Results from strategy comparison
/// </summary>
public class StrategyComparisonResults
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public StrategyComparisonRequest Request { get; set; } = new();
    public List<BacktestResults> StrategyResults { get; set; } = new();
    public ComparisonMetrics ComparisonMetrics { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request for strategy validation
/// </summary>
public class StrategyValidationRequest
{
    public Guid StrategyId { get; set; }
    public StrategyParameters Parameters { get; set; } = new();
}

/// <summary>
/// Result of strategy validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}