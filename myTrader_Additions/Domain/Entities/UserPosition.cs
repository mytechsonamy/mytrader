using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Domain.Entities;

[Table("user_positions")]
public class UserPosition
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("portfolio_id")]
    public Guid PortfolioId { get; set; }

    [Column("symbol_id")]
    public Guid SymbolId { get; set; }

    [Column("quantity"), Precision(38,18)]
    public decimal Quantity { get; set; }

    [Column("avg_price"), Precision(38,18)]
    public decimal AvgPrice { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
