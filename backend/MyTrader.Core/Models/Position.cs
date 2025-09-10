using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserAccountId { get; set; }
    public Guid SymbolId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Side { get; set; } = string.Empty; // long, short
    
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnl { get; set; }
    public decimal RealizedPnl { get; set; }
    
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Legacy compatibility property
    public Guid UserId => UserAccount?.UserId ?? Guid.Empty;
    
    // Navigation properties
    public UserAccount UserAccount { get; set; } = null!;
    public Symbol Symbol { get; set; } = null!;
}