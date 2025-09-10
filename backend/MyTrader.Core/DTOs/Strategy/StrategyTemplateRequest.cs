using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Strategy;

public class CreateStrategyTemplateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Version { get; set; } = "1.0";
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public object Parameters { get; set; } = new();
    
    public object? ParameterSchema { get; set; }
    
    [Required]
    public object EntryRules { get; set; } = new();
    
    [Required]
    public object ExitRules { get; set; } = new();
    
    [Required]
    public object RiskManagement { get; set; } = new();
    
    [MaxLength(50)]
    public string Category { get; set; } = "custom";
    
    [MaxLength(200)]
    public string SupportedAssetClasses { get; set; } = "CRYPTO";
    
    [MaxLength(200)]
    public string SupportedTimeframes { get; set; } = "15m,1h,4h,1d";
    
    public bool IsPublic { get; set; } = false;
    
    public decimal? MinRecommendedCapital { get; set; }
    
    [Range(1, 5)]
    public int? VolatilityLevel { get; set; }
    
    [Range(0, 100)]
    public decimal? ExpectedWinRate { get; set; }
}

public class UpdateStrategyTemplateRequest
{
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public object? Parameters { get; set; }
    
    public object? ParameterSchema { get; set; }
    
    public object? EntryRules { get; set; }
    
    public object? ExitRules { get; set; }
    
    public object? RiskManagement { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    [MaxLength(200)]
    public string? SupportedAssetClasses { get; set; }
    
    [MaxLength(200)]
    public string? SupportedTimeframes { get; set; }
    
    public bool? IsPublic { get; set; }
    
    public decimal? MinRecommendedCapital { get; set; }
    
    [Range(1, 5)]
    public int? VolatilityLevel { get; set; }
    
    [Range(0, 100)]
    public decimal? ExpectedWinRate { get; set; }
}

public class StrategyTemplateQuery
{
    public string? Category { get; set; }
    public string? AssetClass { get; set; }
    public string? Timeframe { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsPublic { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "name";
    public string? SortOrder { get; set; } = "asc";
}