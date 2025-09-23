using MyTrader.Core.Models;
using MyTrader.Core.DTOs.Backtesting;

namespace MyTrader.Core.Interfaces;

/// <summary>
/// Comprehensive backtesting engine interface supporting multiple strategy types,
/// advanced risk management, and parallel execution
/// </summary>
public interface IBacktestingEngine
{
    /// <summary>
    /// Run a single backtest with specified strategy and parameters
    /// </summary>
    Task<BacktestResults> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run parameter optimization using grid search or genetic algorithms
    /// </summary>
    Task<OptimizationResults> RunOptimizationAsync(OptimizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run walk-forward analysis for strategy validation
    /// </summary>
    Task<WalkForwardResults> RunWalkForwardAnalysisAsync(WalkForwardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run Monte Carlo simulation for robustness testing
    /// </summary>
    Task<MonteCarloResults> RunMonteCarloSimulationAsync(MonteCarloRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run parallel backtests for multiple symbols
    /// </summary>
    Task<ParallelBacktestResults> RunParallelBacktestsAsync(ParallelBacktestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed backtest results with trade analysis
    /// </summary>
    Task<DetailedBacktestResults> GetDetailedResultsAsync(Guid backtestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare multiple strategies performance
    /// </summary>
    Task<StrategyComparisonResults> CompareStrategiesAsync(StrategyComparisonRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate strategy configuration and parameters
    /// </summary>
    Task<ValidationResult> ValidateStrategyAsync(StrategyValidationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for strategy execution logic
/// </summary>
public interface IStrategyExecutor
{
    /// <summary>
    /// Strategy type identifier
    /// </summary>
    StrategyType StrategyType { get; }

    /// <summary>
    /// Generate trading signals based on market data and strategy parameters
    /// </summary>
    Task<IEnumerable<TradingSignal>> GenerateSignalsAsync(
        IEnumerable<HistoricalMarketData> marketData,
        StrategyParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate strategy parameters
    /// </summary>
    ValidationResult ValidateParameters(StrategyParameters parameters);

    /// <summary>
    /// Get default parameter values for this strategy
    /// </summary>
    StrategyParameters GetDefaultParameters();
}

/// <summary>
/// Interface for risk management and position sizing
/// </summary>
public interface IRiskManager
{
    /// <summary>
    /// Calculate position size based on risk parameters
    /// </summary>
    decimal CalculatePositionSize(
        decimal availableCapital,
        decimal entryPrice,
        decimal stopLossPrice,
        RiskParameters riskParams);

    /// <summary>
    /// Check if trade meets risk criteria
    /// </summary>
    bool ValidateTrade(
        TradeSignal signal,
        PortfolioState portfolio,
        RiskParameters riskParams);

    /// <summary>
    /// Calculate stop loss and take profit levels
    /// </summary>
    (decimal? stopLoss, decimal? takeProfit) CalculateExitLevels(
        TradeSignal signal,
        RiskParameters riskParams);

    /// <summary>
    /// Monitor portfolio for risk violations
    /// </summary>
    RiskAssessment AssessPortfolioRisk(
        PortfolioState portfolio,
        IEnumerable<Position> positions,
        RiskParameters riskParams);
}

/// <summary>
/// Interface for performance metrics calculation
/// </summary>
public interface IPerformanceCalculator
{
    /// <summary>
    /// Calculate comprehensive performance metrics
    /// </summary>
    PerformanceMetrics CalculateMetrics(
        IEnumerable<TradeExecution> trades,
        decimal initialCapital,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Calculate risk-adjusted metrics
    /// </summary>
    RiskAdjustedMetrics CalculateRiskAdjustedMetrics(
        IEnumerable<decimal> returns,
        decimal riskFreeRate = 0.02m);

    /// <summary>
    /// Calculate drawdown analysis
    /// </summary>
    DrawdownAnalysis CalculateDrawdownMetrics(
        IEnumerable<decimal> portfolioValues);

    /// <summary>
    /// Calculate rolling performance metrics
    /// </summary>
    RollingMetrics CalculateRollingMetrics(
        IEnumerable<TradeExecution> trades,
        TimeSpan windowSize);
}