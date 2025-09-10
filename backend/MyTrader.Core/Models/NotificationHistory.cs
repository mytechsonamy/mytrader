using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("notification_history")]
public class NotificationHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // signal, trade_execution, backtest_result, account_update, marketing
    
    [Required]
    [MaxLength(50)]
    public string Channel { get; set; } = string.Empty; // email, push, sms, telegram
    
    [Required]
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string Body { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // pending, sent, delivered, failed, read
    
    public string? ErrorMessage { get; set; }
    
    // Reference to the entity that triggered the notification
    public Guid? SourceEntityId { get; set; }
    [MaxLength(100)]
    public string? SourceEntityType { get; set; } // Signal, TradeHistory, BacktestResults, etc.
    
    // Additional metadata
    public string? Metadata { get; set; } // JSON for channel-specific data
    public string? Data { get; set; } // Legacy compatibility - same as Metadata
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? FailedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public UserDevice? Device { get; set; }
}