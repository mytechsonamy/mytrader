using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("strategy_performance")]
public class StrategyPerformance
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("strategy_id")]
    public Guid StrategyId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("symbol")]
    [MaxLength(20)]
    public string Symbol { get; set; } = default!;

    [Column("total_return", TypeName = "decimal(18,4)")]
    public decimal TotalReturn { get; set; }

    [Column("win_rate", TypeName = "decimal(5,2)")]
    public decimal WinRate { get; set; }

    [Column("max_drawdown", TypeName = "decimal(5,2)")]
    public decimal MaxDrawdown { get; set; }

    [Column("sharpe_ratio", TypeName = "decimal(5,2)")]
    public decimal SharpeRatio { get; set; }

    [Column("total_trades")]
    public int TotalTrades { get; set; }

    [Column("profitable_trades")]
    public int ProfitableTrades { get; set; }

    [Column("start_date")]
    public DateTimeOffset StartDate { get; set; }

    [Column("end_date")]
    public DateTimeOffset EndDate { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey("StrategyId")]
    public UserStrategy Strategy { get; set; } = default!;

    [ForeignKey("UserId")]
    public User User { get; set; } = default!;
}