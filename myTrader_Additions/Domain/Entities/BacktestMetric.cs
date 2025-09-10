using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MyTrader.Domain.Entities;

[Table("backtest_metrics")]
public class BacktestMetric
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("backtest_id")]
    public Guid BacktestId { get; set; }

    [Column("metric")]
    [MaxLength(64)]
    public string Metric { get; set; } = default!;

    [Column("value"), Precision(38,18)]
    public decimal Value { get; set; }
}
