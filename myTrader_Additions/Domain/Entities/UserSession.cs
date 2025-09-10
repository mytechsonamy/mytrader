using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Domain.Entities;

[Table("user_sessions")]
public class UserSession
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("jwt_id")]
    [MaxLength(255)]
    public string JwtId { get; set; } = default!;

    [Column("refresh_token_hash")]
    [MaxLength(255)]
    public string RefreshTokenHash { get; set; } = default!;

    [Column("token_family_id")]
    public Guid TokenFamilyId { get; set; }

    [Column("rotated_from")]
    public Guid? RotatedFrom { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Column("last_used_at")]
    public DateTimeOffset? LastUsedAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }
}
