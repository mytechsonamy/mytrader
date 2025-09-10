using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class UserDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string DeviceType { get; set; } = string.Empty; // mobile, web, desktop
    
    [Required]
    [MaxLength(255)]
    public string DeviceToken { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? DeviceName { get; set; }
    
    [MaxLength(50)]
    public string? Platform { get; set; } // iOS, Android, Web
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(50)]
    public string? AppVersion { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}