using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("user_achievements")]
public class UserAchievement
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("achievement_type")]
    [MaxLength(50)]
    public string AchievementType { get; set; } = default!;

    [Column("achievement_name")]
    [MaxLength(100)]
    public string AchievementName { get; set; } = default!;

    [Column("description")]
    [MaxLength(250)]
    public string Description { get; set; } = default!;

    [Column("icon")]
    [MaxLength(10)]
    public string Icon { get; set; } = "🏆";

    [Column("points")]
    public int Points { get; set; }

    [Column("earned_at")]
    public DateTimeOffset EarnedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("data")]
    [MaxLength(500)]
    public string? Data { get; set; } // JSON data for achievement context

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = default!;
}