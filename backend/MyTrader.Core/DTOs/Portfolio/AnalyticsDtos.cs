namespace MyTrader.Core.DTOs.Portfolio;

public class PortfolioAnalyticsDto
{
    public Guid PortfolioId { get; set; }
    public string PortfolioName { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public PerformanceMetricsDto Performance { get; set; } = new();
    public RiskMetricsDto Risk { get; set; } = new();
    public List<AllocationDto> AssetAllocation { get; set; } = new();
    public List<AllocationDto> SectorAllocation { get; set; } = new();
    public List<PerformanceHistoryDto> PerformanceHistory { get; set; } = new();
}

public class PerformanceMetricsDto
{
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public decimal DailyReturn { get; set; }
    public decimal WeeklyReturn { get; set; }
    public decimal MonthlyReturn { get; set; }
    public decimal YearToDateReturn { get; set; }
    public decimal BestDay { get; set; }
    public decimal WorstDay { get; set; }
    public int WinningDays { get; set; }
    public int LosingDays { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
}

public class RiskMetricsDto
{
    public decimal Volatility { get; set; }
    public decimal AnnualizedVolatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public DateTime MaxDrawdownDate { get; set; }
    public int MaxDrawdownDuration { get; set; }
    public decimal CurrentDrawdown { get; set; }
    public decimal ValueAtRisk { get; set; } // 95% VaR
    public decimal Beta { get; set; }
    public decimal Alpha { get; set; }
    public decimal InformationRatio { get; set; }
}

public class AllocationDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Asset, Sector, etc.
    public decimal Value { get; set; }
    public decimal Percentage { get; set; }
    public decimal Weight { get; set; }
    public string Color { get; set; } = string.Empty; // For chart visualization
}

public class PerformanceHistoryDto
{
    public DateTime Date { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal DailyReturn { get; set; }
    public decimal CumulativeReturn { get; set; }
    public decimal Benchmark { get; set; } // Benchmark comparison (e.g., S&P 500)
    public decimal Drawdown { get; set; }
}

public class PortfolioComparisonDto
{
    public List<PortfolioAnalysisDto> Portfolios { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string BenchmarkSymbol { get; set; } = "SPY"; // Default benchmark
}

public class PortfolioAnalysisDto
{
    public Guid PortfolioId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalReturn { get; set; }
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public List<DailyReturnDto> DailyReturns { get; set; } = new();
}

public class DailyReturnDto
{
    public DateTime Date { get; set; }
    public decimal Return { get; set; }
    public decimal CumulativeReturn { get; set; }
}

public class AnalyticsRequestDto
{
    public Guid PortfolioId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? BenchmarkSymbol { get; set; } = "SPY";
    public bool IncludeRiskMetrics { get; set; } = true;
    public bool IncludeAllocation { get; set; } = true;
    public bool IncludePerformanceHistory { get; set; } = true;
}

public class PortfolioOptimizationDto
{
    public Guid PortfolioId { get; set; }
    public List<OptimalAllocationDto> RecommendedAllocations { get; set; } = new();
    public decimal ExpectedReturn { get; set; }
    public decimal ExpectedRisk { get; set; }
    public decimal SharpeRatio { get; set; }
    public string OptimizationType { get; set; } = string.Empty; // MaxSharpe, MinRisk, etc.
}

public class OptimalAllocationDto
{
    public string Symbol { get; set; } = string.Empty;
    public string SymbolName { get; set; } = string.Empty;
    public decimal CurrentWeight { get; set; }
    public decimal RecommendedWeight { get; set; }
    public decimal WeightChange { get; set; }
    public decimal ExpectedReturn { get; set; }
    public decimal Risk { get; set; }
}

public class ComparePortfoliosRequestDto
{
    public List<Guid> PortfolioIds { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? BenchmarkSymbol { get; set; } = "SPY";
}