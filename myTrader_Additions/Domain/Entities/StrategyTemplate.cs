using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MyTrader.Domain.Entities;

[Table("strategy_templates")]
public class StrategyTemplate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [Column("version")]
    [MaxLength(20)]
    public string Version { get; set; } = "1.0";

    [Column("parameters")]
    public JsonDocument Parameters { get; set; } = JsonDocument.Parse("{}");

    [Column("param_schema")]
    public JsonDocument? ParamSchema { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
