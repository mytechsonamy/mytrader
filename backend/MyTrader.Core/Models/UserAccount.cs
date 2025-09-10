using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; } = string.Empty; // live, demo, paper
    
    [Required]
    [MaxLength(100)]
    public string BrokerName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;
    
    public string? ApiKeyEncrypted { get; set; }
    public string? ApiSecretEncrypted { get; set; }
    
    public decimal? Balance { get; set; }
    public string? Currency { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Position> Positions { get; set; } = new List<Position>();
}