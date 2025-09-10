using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("price_alerts")]
public class PriceAlert
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("symbol")]
    [MaxLength(20)]
    public string Symbol { get; set; } = default!;

    [Column("alert_type")]
    [MaxLength(20)]
    public string AlertType { get; set; } = default!; // PRICE_ABOVE, PRICE_BELOW, PRICE_CHANGE

    [Column("target_price", TypeName = "decimal(18,8)")]
    public decimal TargetPrice { get; set; }

    [Column("percentage_change", TypeName = "decimal(5,2)")]
    public decimal? PercentageChange { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_triggered")]
    public bool IsTriggered { get; set; } = false;

    [Column("message")]
    [MaxLength(500)]
    public string? Message { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("triggered_at")]
    public DateTimeOffset? TriggeredAt { get; set; }

    [Column("triggered_price", TypeName = "decimal(18,8)")]
    public decimal? TriggeredPrice { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = default!;
}