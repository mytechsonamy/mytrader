using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class PasswordReset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty; // 6-digit verification code
    
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1); // 1 hour expiry
    
    public bool IsUsed { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}