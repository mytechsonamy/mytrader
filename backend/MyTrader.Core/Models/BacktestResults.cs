using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class BacktestResults
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid StrategyId { get; set; }
    
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    // Performance Metrics
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercentage { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercentage { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal CalmarRatio { get; set; }
    public decimal Volatility { get; set; }
    
    // Trade Statistics
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal ExpectedValue { get; set; }
    
    // Portfolio Metrics
    public decimal StartingCapital { get; set; }
    public decimal EndingCapital { get; set; }
    public decimal PeakCapital { get; set; }
    public decimal LowestCapital { get; set; }
    
    // Time-based Metrics
    public int TradingDays { get; set; }
    public decimal AverageHoldingPeriod { get; set; } // in hours
    public decimal MaxHoldingPeriod { get; set; }
    public decimal MinHoldingPeriod { get; set; }
    
    // Risk Metrics
    public decimal BetaToMarket { get; set; }
    public decimal AlphaToMarket { get; set; }
    public decimal TrackingError { get; set; }
    public decimal InformationRatio { get; set; }
    
    // Execution Quality
    public decimal AverageSlippage { get; set; }
    public decimal TotalFees { get; set; }
    public decimal FeeImpactPercentage { get; set; }
    
    public BacktestStatus Status { get; set; } = BacktestStatus.Pending;
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Detailed results as JSON (trade log, equity curve, etc.)
    public string? DetailedResults { get; set; }
    
    // Strategy configuration used for this backtest
    public string StrategyConfig { get; set; } = "{}";
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Strategy Strategy { get; set; } = null!;
    public ICollection<TradeHistory> TradeHistory { get; set; } = new List<TradeHistory>();
}

public enum BacktestStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}