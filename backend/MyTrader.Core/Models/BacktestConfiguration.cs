using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class BacktestConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public decimal InitialBalance { get; set; } = 10000m;
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public string? StrategyParameters { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum BacktestStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}