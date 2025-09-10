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
    public Guid SymbolId { get; set; }
    
    [Required]
    public Guid ConfigurationId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Status { get; set; } = "Running";
    
    [Required]
    [MaxLength(10)]
    public string Timeframe { get; set; } = string.Empty;
    
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercentage { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercentage { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal StartingCapital { get; set; }
    public decimal EndingCapital { get; set; }
    public string? DetailedResults { get; set; }
    public string? StrategyConfig { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User? User { get; set; }
    public Strategy? Strategy { get; set; }
    public Symbol? Symbol { get; set; }
    public ICollection<TradeHistory> TradeHistory { get; set; } = new List<TradeHistory>();
    public ICollection<BacktestResults> Reproductions { get; set; } = new List<BacktestResults>();
}