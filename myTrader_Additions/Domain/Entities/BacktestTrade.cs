using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Domain.Entities;

[Table("backtest_trades")]
public class BacktestTrade
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("backtest_id")]
    public Guid BacktestId { get; set; }

    [Column("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [Column("symbol")]
    [MaxLength(20)]
    public string Symbol { get; set; } = default!;

    [Column("side")]
    [MaxLength(10)]
    public string Side { get; set; } = default!; // BUY/SELL

    [Column("quantity"), Precision(38,18)]
    public decimal Quantity { get; set; }

    [Column("price"), Precision(38,18)]
    public decimal Price { get; set; }

    [Column("pnl"), Precision(38,18)]
    public decimal? PnL { get; set; }
}
