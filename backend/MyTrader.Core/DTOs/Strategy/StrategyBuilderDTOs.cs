using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Strategy;

// Strategy Template DTOs
public class StrategyTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public decimal EstimatedReturns { get; set; }
    public int RiskLevel { get; set; } // 1-10
    public string TimeHorizon { get; set; } = string.Empty;
    public decimal MinCapital { get; set; }
    public List<string> SupportedAssets { get; set; } = new();
    public List<ParameterDefinition> Parameters { get; set; } = new();
    public ChartPreview PreviewChart { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class StrategyTemplateDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    public StrategyOverview Overview { get; set; } = new();
    public RiskMetrics RiskMetrics { get; set; } = new();
    public List<MobileParameter> Parameters { get; set; } = new();
    public List<BacktestSummary> BacktestResults { get; set; } = new();
    public List<SampleTrade> SampleTrades { get; set; } = new();
    public StrategyRequirements Requirements { get; set; } = new();
}

public class StrategyOverview
{
    public string WhatItDoes { get; set; } = string.Empty;
    public string HowItWorks { get; set; } = string.Empty;
    public List<string> BestUsedWhen { get; set; } = new();
    public List<string> Pros { get; set; } = new();
    public List<string> Cons { get; set; } = new();
}

public class RiskMetrics
{
    public int RiskLevel { get; set; } // 1-10
    public decimal MaxDrawdown { get; set; }
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal WinRate { get; set; }
}

public class StrategyRequirements
{
    public decimal MinCapital { get; set; }
    public string TimeCommitment { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public List<string> SupportedMarkets { get; set; } = new();
    public List<string> RequiredIndicators { get; set; } = new();
}

// Parameter DTOs
public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // number, percentage, boolean, selection
    public object DefaultValue { get; set; } = new();
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string>? Options { get; set; } // For selection type
}

public class MobileParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object DefaultValue { get; set; } = new();
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Grouping for UI
    public string HelpText { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty; // slider, toggle, picker, text
    public ParameterValidation Validation { get; set; } = new();
    public List<string>? Options { get; set; }
}

public class ParameterValidation
{
    public bool Required { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty; // Regex pattern
}

// Chart DTOs
public class ChartPreview
{
    public List<ChartPoint> DataPoints { get; set; } = new();
    public string YAxisLabel { get; set; } = string.Empty;
    public string Color { get; set; } = "#4CAF50";
}

public class ChartPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

// Strategy Creation DTOs
public class CreateStrategyRequest
{
    [Required]
    public Guid TemplateId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
    
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class CreateStrategyResponse
{
    public Guid StrategyId { get; set; }
    public Guid UserStrategyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public StrategySetup EstimatedSetup { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class StrategySetup
{
    public decimal ExpectedReturn { get; set; }
    public int RiskLevel { get; set; }
    public decimal RecommendedCapital { get; set; }
    public int EstimatedTrades { get; set; } // Per month
    public List<string> NextSteps { get; set; } = new();
}

// User Strategy DTOs
public class UserStrategyResponse
{
    public Guid Id { get; set; }
    public Guid StrategyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCategory { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public StrategyPerformance Performance { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StrategyPerformance
{
    public decimal TotalReturn { get; set; }
    public decimal MonthlyReturn { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public string Status { get; set; } = string.Empty;
}

// Wizard DTOs
public class WizardStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // selection, slider, amount, multi-select
    public List<WizardOption> Options { get; set; } = new();
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal? DefaultValue { get; set; }
    public List<string> Labels { get; set; } = new();
    public string Currency { get; set; } = string.Empty;
}

public class WizardOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class WizardResponses
{
    public string Goal { get; set; } = string.Empty;
    public int RiskTolerance { get; set; } // 1-10
    public decimal InvestmentCapital { get; set; }
    public string TimeCommitment { get; set; } = string.Empty;
    public List<string> AssetPreferences { get; set; } = new();
}

public class StrategyRecommendation
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MatchScore { get; set; } // 0-100
    public List<string> Pros { get; set; } = new();
    public List<string> Cons { get; set; } = new();
    public decimal ExpectedReturn { get; set; }
    public int RiskLevel { get; set; }
    public decimal MinCapital { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

// Backtesting DTOs
public class BacktestSummary
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalReturn { get; set; }
    public decimal WinRate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
}

public class SampleTrade
{
    public string Symbol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // BUY/SELL
    public decimal Entry { get; set; }
    public decimal Exit { get; set; }
    public decimal Return { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

// Paper Trading DTOs
public class PaperTradingRequest
{
    [Required]
    [Range(1000, 1000000)]
    public decimal StartingCapital { get; set; }
    
    [Range(1, 365)]
    public int TestDurationDays { get; set; } = 30;
    
    public bool EnableNotifications { get; set; } = true;
}

public class PaperTradingResponse
{
    public Guid PaperTradingId { get; set; }
    public Guid StrategyId { get; set; }
    public decimal StartingCapital { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public string EstimatedDuration { get; set; } = string.Empty;
    public DateTime NextUpdate { get; set; }
}

// Validation DTOs
public class ParameterValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

// Mobile UI Helper DTOs
public class StrategyCard
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RiskLevel { get; set; }
    public decimal ExpectedReturn { get; set; }
    public decimal MinCapital { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string PreviewImage { get; set; } = string.Empty;
    public StrategyStats Stats { get; set; } = new();
}

public class StrategyStats
{
    public int UsersCount { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public decimal SuccessRate { get; set; }
}

// Filter and Search DTOs
public class StrategyFilterRequest
{
    public List<string> Categories { get; set; } = new();
    public List<int> RiskLevels { get; set; } = new();
    public decimal? MinCapital { get; set; }
    public decimal? MaxCapital { get; set; }
    public List<string> Difficulties { get; set; } = new();
    public List<string> AssetTypes { get; set; } = new();
    public string SortBy { get; set; } = "popularity"; // popularity, return, risk, newest
    public string SearchQuery { get; set; } = string.Empty;
}

public class StrategySearchResponse
{
    public List<StrategyCard> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public Dictionary<string, int> CategoryCounts { get; set; } = new();
    public Dictionary<int, int> RiskLevelCounts { get; set; } = new();
    public StrategyFilterRequest AppliedFilters { get; set; } = new();
}

// Dashboard DTOs
public class StrategyDashboard
{
    public List<UserStrategyResponse> ActiveStrategies { get; set; } = new();
    public PortfolioSummary Portfolio { get; set; } = new();
    public List<RecentSignal> RecentSignals { get; set; } = new();
    public List<UpcomingEvent> UpcomingEvents { get; set; } = new();
    public PerformanceChart PerformanceChart { get; set; } = new();
}

public class PortfolioSummary
{
    public decimal TotalValue { get; set; }
    public decimal DayChange { get; set; }
    public decimal DayChangePercent { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int ActivePositions { get; set; }
    public decimal CashBalance { get; set; }
}

public class RecentSignal
{
    public string Symbol { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class UpcomingEvent
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // earnings, economic, strategy
}

public class PerformanceChart
{
    public List<ChartPoint> PortfolioValue { get; set; } = new();
    public List<ChartPoint> Benchmark { get; set; } = new();
    public string Period { get; set; } = string.Empty;
}