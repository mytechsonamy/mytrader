using System;
using System.Collections.Generic;

namespace MyTrader.Core.DTOs.Analytics;

public class PerformanceAnalytics
{
    public Guid UserId { get; set; }
    public string Period { get; set; } = default!; // "1D", "7D", "30D", "90D", "1Y", "ALL"
    
    // Overall Performance Metrics
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercentage { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercentage { get; set; }
    
    // Trading Statistics
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageTradeSize { get; set; }
    
    // Time-based Performance
    public List<PeriodPerformance> DailyReturns { get; set; } = new();
    public List<PeriodPerformance> WeeklyReturns { get; set; } = new();
    public List<PeriodPerformance> MonthlyReturns { get; set; } = new();
    
    // Strategy Performance
    public List<StrategyAnalytics> StrategyBreakdown { get; set; } = new();
    
    // Symbol Performance
    public List<SymbolAnalytics> SymbolBreakdown { get; set; } = new();
    
    // Risk Metrics
    public RiskMetrics RiskAnalysis { get; set; } = new();
    
    public DateTimeOffset AnalysisDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
}

public class PeriodPerformance
{
    public DateTimeOffset Date { get; set; }
    public decimal Return { get; set; }
    public decimal ReturnPercentage { get; set; }
    public decimal CumulativeReturn { get; set; }
    public decimal CumulativeReturnPercentage { get; set; }
    public int TradeCount { get; set; }
    public decimal Volume { get; set; }
}

public class StrategyAnalytics
{
    public Guid StrategyId { get; set; }
    public string StrategyName { get; set; } = default!;
    public decimal Return { get; set; }
    public decimal ReturnPercentage { get; set; }
    public decimal WinRate { get; set; }
    public int TradeCount { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset LastUsed { get; set; }
}

public class SymbolAnalytics
{
    public string Symbol { get; set; } = default!;
    public decimal Return { get; set; }
    public decimal ReturnPercentage { get; set; }
    public decimal WinRate { get; set; }
    public int TradeCount { get; set; }
    public decimal Volume { get; set; }
    public decimal AverageTradeSize { get; set; }
    public decimal ProfitFactor { get; set; }
}

public class RiskMetrics
{
    public decimal ValueAtRisk { get; set; } // 95% VaR
    public decimal ConditionalValueAtRisk { get; set; } // Expected Shortfall
    public decimal Beta { get; set; } // Market beta
    public decimal Alpha { get; set; } // Alpha generation
    public decimal Volatility { get; set; } // Annualized volatility
    public decimal DownsideDeviation { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal CalmarRatio { get; set; }
    public decimal InformationRatio { get; set; }
}