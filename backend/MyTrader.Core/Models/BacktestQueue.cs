using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTrader.Core.Models;

[Table("backtest_queue")]
public class BacktestQueue
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid StrategyId { get; set; }
    
    [Required] 
    public Guid SymbolId { get; set; }
    
    public Guid? ConfigurationId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string TriggerType { get; set; } = default!; // "Manual", "Scheduled", "NewSymbol", "Performance"
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Queued"; // "Queued", "Running", "Completed", "Failed", "Cancelled"
    
    [Required]
    public int Priority { get; set; } = 50; // 0-100, higher = more priority
    
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    
    [Column(TypeName = "jsonb")]
    public string? Parameters { get; set; } // JSON parameters for the backtest
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; } // Additional metadata (trigger context, etc.)
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ScheduledFor { get; set; } // For scheduled backtests
    
    public TimeSpan? EstimatedDuration { get; set; }
    public TimeSpan? ActualDuration { get; set; }
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    public Guid? ResultId { get; set; } // Link to BacktestResults when completed
    
    // Navigation properties
    public virtual User User { get; set; } = default!;
    public virtual Strategy Strategy { get; set; } = default!;
    public virtual Symbol Symbol { get; set; } = default!;
    public virtual BacktestConfiguration? Configuration { get; set; }
    public virtual BacktestResults? Result { get; set; }
}

public enum QueuePriority
{
    Low = 10,
    Normal = 50,
    High = 75,
    Critical = 100
}

public enum QueueTriggerType
{
    Manual,
    Scheduled, 
    NewSymbol,
    PerformanceDegradation,
    MarketEvent,
    Optimization
}

public enum QueueStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
    Retrying
}