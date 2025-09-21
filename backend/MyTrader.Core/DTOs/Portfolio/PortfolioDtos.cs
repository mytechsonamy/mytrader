using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Portfolio;

public class PortfolioSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public decimal InitialCapital { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<PortfolioPositionDto> Positions { get; set; } = new();
}

public class PortfolioPositionDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string SymbolName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public decimal CostBasis { get; set; }
    public decimal Weight { get; set; } // Portfolio allocation percentage
    public DateTime? LastTradedAt { get; set; }
}

public class PortfolioPerformanceDto
{
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public decimal DailyReturn { get; set; }
    public decimal DailyReturnPercent { get; set; }
    public decimal WeeklyReturn { get; set; }
    public decimal WeeklyReturnPercent { get; set; }
    public decimal MonthlyReturn { get; set; }
    public decimal MonthlyReturnPercent { get; set; }
    public decimal YearlyReturn { get; set; }
    public decimal YearlyReturnPercent { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class PortfolioAllocationDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class CreatePortfolioDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(12)]
    public string BaseCurrency { get; set; } = "USD";
    
    [Range(0.01, double.MaxValue)]
    public decimal InitialCapital { get; set; } = 100000m;
}

public class UpdatePortfolioDto
{
    [MaxLength(100)]
    public string? Name { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(12)]
    public string? BaseCurrency { get; set; }
}