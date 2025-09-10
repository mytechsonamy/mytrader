using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MyTrader.Core.Models;

[Table("user_strategies")]
public class UserStrategy
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("template_id")]
    public Guid? TemplateId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [Column("parameters")]
    public JsonDocument Parameters { get; set; } = JsonDocument.Parse("{}");

    [Column("is_active")]
    public bool IsActive { get; set; } = false;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("template_version")]
    [MaxLength(20)]
    public string? TemplateVersion { get; set; }
}
