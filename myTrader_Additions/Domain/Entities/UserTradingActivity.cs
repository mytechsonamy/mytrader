using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Domain.Entities;

[Table("user_trading_activity")]
public class UserTradingActivity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("symbol_id")]
    public Guid SymbolId { get; set; }

    [Column("source")]
    [MaxLength(10)]
    public string Source { get; set; } = "backtest"; // backtest|paper|live

    [Column("side")]
    [MaxLength(10)]
    public string Side { get; set; } = "BUY";

    [Column("quantity"), Precision(38,18)]
    public decimal Quantity { get; set; }

    [Column("price"), Precision(38,18)]
    public decimal Price { get; set; }

    [Column("ts")]
    public DateTimeOffset Timestamp { get; set; }
}
