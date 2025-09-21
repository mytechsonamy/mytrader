using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyTrader.Core.Models;

[Table("portfolio_positions")]
public class PortfolioPosition
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("portfolio_id")]
    [Required]
    public Guid PortfolioId { get; set; }

    [Column("symbol_id")]
    [Required]
    public Guid SymbolId { get; set; }

    [Column("quantity")]
    [Precision(18, 8)]
    public decimal Quantity { get; set; } = 0m;

    [Column("average_price")]
    [Precision(18, 8)]
    public decimal AveragePrice { get; set; } = 0m;

    [Column("current_price")]
    [Precision(18, 8)]
    public decimal CurrentPrice { get; set; } = 0m;

    [Column("market_value")]
    [Precision(18, 8)]
    public decimal MarketValue { get; set; } = 0m;

    [Column("unrealized_pnl")]
    [Precision(18, 8)]
    public decimal UnrealizedPnL { get; set; } = 0m;

    [Column("unrealized_pnl_percent")]
    [Precision(10, 4)]
    public decimal UnrealizedPnLPercent { get; set; } = 0m;

    [Column("realized_pnl")]
    [Precision(18, 8)]
    public decimal RealizedPnL { get; set; } = 0m;

    [Column("cost_basis")]
    [Precision(18, 8)]
    public decimal CostBasis { get; set; } = 0m;

    [Column("first_purchased_at")]
    public DateTime? FirstPurchasedAt { get; set; }

    [Column("last_traded_at")]
    public DateTime? LastTradedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public UserPortfolio Portfolio { get; set; } = null!;
    public Symbol Symbol { get; set; } = null!;
}