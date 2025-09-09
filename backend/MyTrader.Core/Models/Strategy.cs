using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class Strategy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = false;
    public bool IsCustom { get; set; } = true; // Custom vs Template strategy
    public bool IsDefault { get; set; } = false;
    
    // Strategy type classification
    public StrategyType StrategyType { get; set; } = StrategyType.TrendFollowing;
    
    // Risk settings
    public decimal InitialCapital { get; set; } = 10000m;
    public decimal MaxPositionSize { get; set; } = 0.1m; // 10% of portfolio
    
    // Indicator Configuration Reference
    public Guid? IndicatorConfigId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Strategy parameters as JSON string
    public string Parameters { get; set; } = "{}";
    
    // Strategy rules and conditions as JSON
    public string EntryRules { get; set; } = "{}";
    public string ExitRules { get; set; } = "{}";
    
    // Performance tracking
    public decimal? TotalReturn { get; set; }
    public decimal? WinRate { get; set; }
    public int? TotalTrades { get; set; }
    public DateTime? LastBacktestDate { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public IndicatorConfig? IndicatorConfig { get; set; }
    public ICollection<Signal> Signals { get; set; } = new List<Signal>();
    public ICollection<BacktestResults> BacktestResults { get; set; } = new List<BacktestResults>();
    public ICollection<TradeHistory> TradeHistory { get; set; } = new List<TradeHistory>();
}

public enum StrategyType
{
    TrendFollowing = 0,
    MeanReversion = 1,
    Momentum = 2,
    VolatilityBreakout = 3,
    Scalping = 4,
    Conservative = 5,
    Aggressive = 6,
    Balanced = 7,
    Custom = 8
}