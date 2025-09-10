using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Strategy;

public class CreateUserStrategyRequest
{
    /// <summary>
    /// Template to base this strategy on (null for fully custom strategy)
    /// </summary>
    public Guid? TemplateId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public object Parameters { get; set; } = new();
    
    /// <summary>
    /// Custom entry rules (override template if provided)
    /// </summary>
    public object? CustomEntryRules { get; set; }
    
    /// <summary>
    /// Custom exit rules (override template if provided)
    /// </summary>
    public object? CustomExitRules { get; set; }
    
    /// <summary>
    /// Custom risk management (override template if provided)
    /// </summary>
    public object? CustomRiskManagement { get; set; }
    
    [MaxLength(1000)]
    public string? TargetSymbols { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Timeframe { get; set; } = "1h";
    
    public bool IsActive { get; set; } = false;
    
    [Range(100, 1000000)]
    public decimal InitialCapital { get; set; } = 10000m;
    
    [Range(1, 100)]
    public decimal MaxPositionSizePercent { get; set; } = 10m;
    
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    [MaxLength(500)]
    public string? Tags { get; set; }
}

public class UpdateUserStrategyRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public object? Parameters { get; set; }
    
    public object? CustomEntryRules { get; set; }
    
    public object? CustomExitRules { get; set; }
    
    public object? CustomRiskManagement { get; set; }
    
    [MaxLength(1000)]
    public string? TargetSymbols { get; set; }
    
    [MaxLength(10)]
    public string? Timeframe { get; set; }
    
    public bool? IsActive { get; set; }
    
    public bool? IsFavorite { get; set; }
    
    [Range(100, 1000000)]
    public decimal? InitialCapital { get; set; }
    
    [Range(1, 100)]
    public decimal? MaxPositionSizePercent { get; set; }
    
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    [MaxLength(500)]
    public string? Tags { get; set; }
}

public class UserStrategyQuery
{
    public Guid? TemplateId { get; set; }
    public string? Timeframe { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFavorite { get; set; }
    public bool? IsCustom { get; set; }
    public string? Search { get; set; }
    public string? Tags { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "name";
    public string? SortOrder { get; set; } = "asc";
}