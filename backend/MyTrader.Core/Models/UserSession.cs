using System;
using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string SessionToken { get; set; } = default!;

    [MaxLength(255)]
    public string JwtId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string RefreshTokenHash { get; set; } = string.Empty;

    public Guid TokenFamilyId { get; set; }
    public Guid? RotatedFrom { get; set; }

    [MaxLength(1000)]
    public string? UserAgent { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}
