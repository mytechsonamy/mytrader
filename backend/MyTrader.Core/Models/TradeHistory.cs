using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class TradeHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    public Guid? StrategyId { get; set; }
    public Guid? BacktestResultsId { get; set; }
    
    [Required]
    public Guid SymbolId { get; set; }
    
    [Required]
    public TradeType TradeType { get; set; } // BUY, SELL
    
    [Required]
    public TradeSource TradeSource { get; set; } // Live, Backtest, Paper
    
    // Entry Details
    public DateTime EntryTime { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal EntryValue { get; set; }
    public decimal? EntryFee { get; set; }
    
    // Exit Details
    public DateTime? ExitTime { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal? ExitValue { get; set; }
    public decimal? ExitFee { get; set; }
    
    // P&L Calculation
    public decimal? RealizedPnl { get; set; }
    public decimal? RealizedPnlPercentage { get; set; }
    public decimal? UnrealizedPnl { get; set; }
    public decimal? UnrealizedPnlPercentage { get; set; }
    
    // Trade Metrics
    public TimeSpan? HoldingPeriod { get; set; }
    public decimal? MaxProfit { get; set; }
    public decimal? MaxLoss { get; set; }
    public decimal? MaxProfitPercentage { get; set; }
    public decimal? MaxLossPercentage { get; set; }
    
    // Signal Context
    public decimal? EntryRsi { get; set; }
    public decimal? EntryMacd { get; set; }
    public decimal? EntryBollingerPosition { get; set; }
    public decimal? ExitRsi { get; set; }
    public decimal? ExitMacd { get; set; }
    public decimal? ExitBollingerPosition { get; set; }
    
    // Risk Management
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public bool? WasStopLossHit { get; set; }
    public bool? WasTakeProfitHit { get; set; }
    
    public TradeStatus Status { get; set; } = TradeStatus.Open;
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Additional trade context as JSON
    public string? TradeContext { get; set; } = "{}";
    
    // Legacy compatibility properties (computed from primary properties)
    public decimal ProfitLoss => RealizedPnl ?? UnrealizedPnl ?? 0m;
    public decimal Price => ExitPrice ?? EntryPrice;
    public DateTime ExecutedAt => ExitTime ?? EntryTime;
    public decimal Commission => (EntryFee ?? 0m) + (ExitFee ?? 0m);
    public string Type => TradeType.ToString().ToUpper();
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Symbol Symbol { get; set; } = null!;
    public Strategy? Strategy { get; set; }
    public BacktestResults? BacktestResults { get; set; }
}

public enum TradeType
{
    Buy = 0,
    Sell = 1
}

public enum TradeSource
{
    Live = 0,
    Backtest = 1,
    PaperTrading = 2
}

public enum TradeStatus
{
    Open = 0,
    Closed = 1,
    Cancelled = 2,
    PartiallyFilled = 3
}