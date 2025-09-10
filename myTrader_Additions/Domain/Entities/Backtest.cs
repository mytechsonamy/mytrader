using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MyTrader.Domain.Entities;

[Table("backtests")]
public class Backtest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("strategy_id")]
    public Guid StrategyId { get; set; }

    [Column("symbol")]
    [MaxLength(20)]
    public string Symbol { get; set; } = default!;

    [Column("timeframe")]
    [MaxLength(10)]
    public string Timeframe { get; set; } = default!;

    [Column("date_range_start")]
    public DateTimeOffset DateRangeStart { get; set; }

    [Column("date_range_end")]
    public DateTimeOffset DateRangeEnd { get; set; }

    [Column("config_snapshot")]
    public JsonDocument ConfigSnapshot { get; set; } = JsonDocument.Parse("{}");

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "running";

    [Column("started_at")]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("finished_at")]
    public DateTimeOffset? FinishedAt { get; set; }

    [Column("total_return"), Precision(38,18)]
    public decimal? TotalReturn { get; set; }

    [Column("sharpe_ratio"), Precision(38,18)]
    public decimal? SharpeRatio { get; set; }

    [Column("max_drawdown"), Precision(38,18)]
    public decimal? MaxDrawdown { get; set; }

    [Column("total_trades")]
    public int? TotalTrades { get; set; }

    [Column("code_ref")]
    [MaxLength(64)]
    public string CodeRef { get; set; } = "unknown";

    [Column("engine_version")]
    [MaxLength(20)]
    public string EngineVersion { get; set; } = "1.0";

    [Column("indicator_versions")]
    public JsonDocument IndicatorVersions { get; set; } = JsonDocument.Parse("{}");

    [Column("config_hash")]
    [MaxLength(64)]
    public string ConfigHash { get; set; } = default!;
}
