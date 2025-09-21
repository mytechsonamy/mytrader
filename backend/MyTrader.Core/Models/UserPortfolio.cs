using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyTrader.Core.Models;

[Table("user_portfolios")]
public class UserPortfolio
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    [Required]
    public Guid UserId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    [Required]
    public string Name { get; set; } = "Default Portfolio";

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("base_currency")]
    [MaxLength(12)]
    [Required]
    public string BaseCurrency { get; set; } = "USD";

    [Column("initial_capital")]
    [Precision(18, 8)]
    public decimal InitialCapital { get; set; } = 100000m;

    [Column("current_value")]
    [Precision(18, 8)]
    public decimal CurrentValue { get; set; } = 100000m;

    [Column("cash_balance")]
    [Precision(18, 8)]
    public decimal CashBalance { get; set; } = 100000m;

    [Column("total_pnl")]
    [Precision(18, 8)]
    public decimal TotalPnL { get; set; } = 0m;

    [Column("daily_pnl")]
    [Precision(18, 8)]
    public decimal DailyPnL { get; set; } = 0m;

    [Column("total_return_percent")]
    [Precision(10, 4)]
    public decimal TotalReturnPercent { get; set; } = 0m;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_default")]
    public bool IsDefault { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<PortfolioPosition> Positions { get; set; } = new List<PortfolioPosition>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}