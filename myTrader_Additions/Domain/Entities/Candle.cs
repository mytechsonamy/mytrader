using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Domain.Entities;

[Table("candles")]
public class Candle
{
    [Column("symbol_id")]
    public Guid SymbolId { get; set; }

    [Column("timeframe")]
    [MaxLength(10)]
    public string Timeframe { get; set; } = default!;

    [Column("ts")]
    public DateTimeOffset Timestamp { get; set; }

    [Column("open"), Precision(38,18)]
    public decimal Open { get; set; }

    [Column("high"), Precision(38,18)]
    public decimal High { get; set; }

    [Column("low"), Precision(38,18)]
    public decimal Low { get; set; }

    [Column("close"), Precision(38,18)]
    public decimal Close { get; set; }

    [Column("volume"), Precision(38,18)]
    public decimal Volume { get; set; }
}
