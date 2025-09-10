using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs.Analytics;
using MyTrader.Infrastructure.Data;
using System.Text.Json;

namespace MyTrader.Services.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly TradingDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(TradingDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PerformanceAnalytics> GetPerformanceAnalyticsAsync(Guid userId, string period = "30D")
    {
        var (startDate, endDate) = GetPeriodDates(period);
        
        // Get strategy performances for the period
        var performances = await _context.StrategyPerformances
            .Where(p => p.UserId == userId && p.UpdatedAt >= startDate && p.UpdatedAt <= endDate)
            .ToListAsync();

        if (!performances.Any())
        {
            return new PerformanceAnalytics
            {
                UserId = userId,
                Period = period,
                PeriodStart = startDate,
                PeriodEnd = endDate
            };
        }

        // Calculate overall metrics
        var totalReturn = performances.Sum(p => p.TotalReturn);
        var averageWinRate = performances.Average(p => p.WinRate);
        var totalTrades = performances.Sum(p => p.TotalTrades);
        var winningTrades = performances.Sum(p => p.ProfitableTrades);
        var maxDrawdown = performances.Max(p => p.MaxDrawdown);
        var averageSharpe = performances.Average(p => p.SharpeRatio);

        // Generate time-based performance data
        var dailyReturns = await GenerateDailyReturns(userId, startDate, endDate);
        var strategyBreakdown = await GetStrategyBreakdown(userId, startDate, endDate);
        var symbolBreakdown = await GetSymbolBreakdownInternal(userId, startDate, endDate);
        var riskMetrics = await CalculateRiskMetrics(userId, startDate, endDate);

        return new PerformanceAnalytics
        {
            UserId = userId,
            Period = period,
            TotalReturn = totalReturn,
            TotalReturnPercentage = totalReturn, // Assuming returns are already in percentage
            WinRate = averageWinRate,
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = totalTrades - winningTrades,
            SharpeRatio = averageSharpe,
            MaxDrawdown = maxDrawdown,
            DailyReturns = dailyReturns,
            StrategyBreakdown = strategyBreakdown,
            SymbolBreakdown = symbolBreakdown,
            RiskAnalysis = riskMetrics,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };
    }

    public async Task<EquityCurveData> GetEquityCurveAsync(Guid userId, string period = "30D")
    {
        var (startDate, endDate) = GetPeriodDates(period);
        var dailyReturns = await GenerateDailyReturns(userId, startDate, endDate);

        var equityCurve = new EquityCurveData
        {
            ChartType = "equity_curve",
            Title = "Equity Curve",
            Period = period,
            StartingBalance = 10000, // Default starting balance
            Configuration = new ChartConfiguration
            {
                XAxisLabel = "Date",
                YAxisLabel = "Portfolio Value ($)",
                Currency = "USD"
            }
        };

        // Calculate cumulative equity
        decimal runningBalance = equityCurve.StartingBalance;
        var equityPoints = new List<ChartDataPoint>();
        var drawdownPeriods = new List<DrawdownPeriod>();
        
        decimal peakBalance = runningBalance;
        DateTimeOffset? drawdownStart = null;

        foreach (var day in dailyReturns.OrderBy(d => d.Date))
        {
            runningBalance += day.Return;
            
            equityPoints.Add(new ChartDataPoint
            {
                Timestamp = day.Date,
                Value = runningBalance,
                Volume = day.Volume
            });

            // Track drawdown periods
            if (runningBalance > peakBalance)
            {
                // End current drawdown if exists
                if (drawdownStart.HasValue)
                {
                    drawdownPeriods.Add(new DrawdownPeriod
                    {
                        StartDate = drawdownStart.Value,
                        EndDate = day.Date,
                        MaxDrawdown = peakBalance - runningBalance,
                        MaxDrawdownPercentage = ((peakBalance - runningBalance) / peakBalance) * 100,
                        DurationDays = (int)(day.Date - drawdownStart.Value).TotalDays
                    });
                    drawdownStart = null;
                }
                peakBalance = runningBalance;
            }
            else if (runningBalance < peakBalance && !drawdownStart.HasValue)
            {
                drawdownStart = day.Date;
            }
        }

        equityCurve.Series.Add(new ChartSeries
        {
            Name = "Portfolio Value",
            Type = "line",
            Color = "#007bff",
            Data = equityPoints
        });

        equityCurve.EndingBalance = runningBalance;
        equityCurve.PeakBalance = peakBalance;
        equityCurve.LowestBalance = equityPoints.Min(p => p.Value);
        equityCurve.DrawdownPeriods = drawdownPeriods;

        return equityCurve;
    }

    public async Task<ChartDataResponse> GetPnLChartAsync(Guid userId, string period = "30D", string interval = "1D")
    {
        var (startDate, endDate) = GetPeriodDates(period);
        var dailyReturns = await GenerateDailyReturns(userId, startDate, endDate);

        var chartData = new ChartDataResponse
        {
            ChartType = "pnl_chart",
            Title = "Profit & Loss",
            Period = period,
            Configuration = new ChartConfiguration
            {
                XAxisLabel = "Date",
                YAxisLabel = "P&L ($)",
                Currency = "USD"
            }
        };

        var pnlPoints = dailyReturns.Select(d => new ChartDataPoint
        {
            Timestamp = d.Date,
            Value = d.Return,
            Volume = d.Volume,
            Metadata = new Dictionary<string, object>
            {
                ["trade_count"] = d.TradeCount,
                ["cumulative_return"] = d.CumulativeReturn
            }
        }).ToList();

        // Add positive and negative series for better visualization
        chartData.Series.Add(new ChartSeries
        {
            Name = "Profit",
            Type = "column",
            Color = "#28a745",
            Data = pnlPoints.Where(p => p.Value >= 0).ToList()
        });

        chartData.Series.Add(new ChartSeries
        {
            Name = "Loss",
            Type = "column", 
            Color = "#dc3545",
            Data = pnlPoints.Where(p => p.Value < 0).ToList()
        });

        return chartData;
    }

    public async Task<PnLDistributionData> GetPnLDistributionAsync(Guid userId, string period = "30D")
    {
        var (startDate, endDate) = GetPeriodDates(period);
        var performances = await _context.StrategyPerformances
            .Where(p => p.UserId == userId && p.UpdatedAt >= startDate && p.UpdatedAt <= endDate)
            .Select(p => p.TotalReturn)
            .ToListAsync();

        if (!performances.Any())
        {
            return new PnLDistributionData
            {
                ChartType = "pnl_distribution",
                Title = "P&L Distribution",
                Period = period
            };
        }

        // Create histogram buckets
        var minPnL = performances.Min();
        var maxPnL = performances.Max();
        var bucketCount = Math.Min(20, performances.Count);
        var bucketSize = (maxPnL - minPnL) / bucketCount;

        var distribution = new List<PnLBucket>();
        for (int i = 0; i < bucketCount; i++)
        {
            var rangeStart = minPnL + (i * bucketSize);
            var rangeEnd = rangeStart + bucketSize;
            var count = performances.Count(p => p >= rangeStart && p < rangeEnd);
            
            distribution.Add(new PnLBucket
            {
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                Count = count,
                Frequency = (decimal)count / performances.Count
            });
        }

        // Calculate statistics
        var mean = performances.Average();
        var median = performances.OrderBy(p => p).Skip(performances.Count / 2).First();
        var variance = performances.Sum(p => (double)Math.Pow((double)(p - mean), 2)) / performances.Count;
        var stdDev = (decimal)Math.Sqrt(variance);

        return new PnLDistributionData
        {
            ChartType = "pnl_distribution",
            Title = "P&L Distribution",
            Period = period,
            Distribution = distribution,
            MeanPnL = mean,
            MedianPnL = median,
            StandardDeviation = stdDev,
            Series = new List<ChartSeries>
            {
                new ChartSeries
                {
                    Name = "Distribution",
                    Type = "column",
                    Data = distribution.Select(b => new ChartDataPoint
                    {
                        Value = b.Count,
                        Label = $"${b.RangeStart:F0} - ${b.RangeEnd:F0}"
                    }).ToList()
                }
            }
        };
    }

    public async Task<HeatmapData> GetStrategyHeatmapAsync(Guid userId)
    {
        var strategies = await _context.UserStrategies
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var performances = await _context.StrategyPerformances
            .Where(p => p.UserId == userId)
            .ToListAsync();

        var symbols = performances.Select(p => p.Symbol).Distinct().ToList();

        var heatmapData = new HeatmapData
        {
            ChartType = "strategy_heatmap",
            Title = "Strategy Performance Heatmap",
            Period = "ALL"
        };

        var heatmapPoints = new List<HeatmapDataPoint>();
        var values = new List<decimal>();

        foreach (var strategy in strategies)
        {
            foreach (var symbol in symbols)
            {
                var performance = performances
                    .Where(p => p.StrategyId == strategy.Id && p.Symbol == symbol)
                    .Sum(p => p.TotalReturn);

                heatmapPoints.Add(new HeatmapDataPoint
                {
                    XCategory = symbol,
                    YCategory = strategy.Name,
                    Value = performance,
                    DisplayValue = $"{performance:F2}%"
                });

                values.Add(performance);
            }
        }

        // Generate color scale
        if (values.Any())
        {
            var minValue = values.Min();
            var maxValue = values.Max();

            foreach (var point in heatmapPoints)
            {
                var normalizedValue = (point.Value - minValue) / (maxValue - minValue);
                point.Color = GetHeatmapColor(normalizedValue);
            }

            heatmapData.Scale = new HeatmapScale
            {
                MinValue = minValue,
                MaxValue = maxValue,
                ColorScale = new List<string> { "#ff4444", "#ffaa00", "#44ff44" }
            };
        }

        heatmapData.HeatmapPoints = heatmapPoints;
        return heatmapData;
    }

    // Helper methods
    private (DateTimeOffset startDate, DateTimeOffset endDate) GetPeriodDates(string period)
    {
        var endDate = DateTimeOffset.UtcNow;
        var startDate = period.ToUpper() switch
        {
            "1D" => endDate.AddDays(-1),
            "7D" => endDate.AddDays(-7),
            "30D" => endDate.AddDays(-30),
            "90D" => endDate.AddDays(-90),
            "1Y" => endDate.AddYears(-1),
            "ALL" => DateTimeOffset.MinValue,
            _ => endDate.AddDays(-30)
        };
        return (startDate, endDate);
    }

    private async Task<List<PeriodPerformance>> GenerateDailyReturns(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        // For now, generate mock daily returns - in production this would calculate from actual trade data
        var dailyReturns = new List<PeriodPerformance>();
        var random = new Random();
        var currentDate = startDate.Date;
        decimal cumulativeReturn = 0;

        while (currentDate <= endDate.Date)
        {
            var dailyReturn = (decimal)(random.NextDouble() - 0.5) * 200; // -100 to +100
            cumulativeReturn += dailyReturn;

            dailyReturns.Add(new PeriodPerformance
            {
                Date = new DateTimeOffset(currentDate),
                Return = dailyReturn,
                ReturnPercentage = dailyReturn,
                CumulativeReturn = cumulativeReturn,
                CumulativeReturnPercentage = cumulativeReturn,
                TradeCount = random.Next(0, 10),
                Volume = (decimal)(random.NextDouble() * 100000)
            });

            currentDate = currentDate.AddDays(1);
        }

        return dailyReturns;
    }

    private async Task<List<StrategyAnalytics>> GetStrategyBreakdown(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var strategies = await _context.UserStrategies
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var performances = await _context.StrategyPerformances
            .Where(p => p.UserId == userId && p.UpdatedAt >= startDate && p.UpdatedAt <= endDate)
            .ToListAsync();

        return strategies.Select(s =>
        {
            var strategyPerformances = performances.Where(p => p.StrategyId == s.Id).ToList();
            return new StrategyAnalytics
            {
                StrategyId = s.Id,
                StrategyName = s.Name,
                Return = strategyPerformances.Sum(p => p.TotalReturn),
                ReturnPercentage = strategyPerformances.Sum(p => p.TotalReturn),
                WinRate = strategyPerformances.Any() ? strategyPerformances.Average(p => p.WinRate) : 0,
                TradeCount = strategyPerformances.Sum(p => p.TotalTrades),
                SharpeRatio = strategyPerformances.Any() ? strategyPerformances.Average(p => p.SharpeRatio) : 0,
                MaxDrawdown = strategyPerformances.Any() ? strategyPerformances.Max(p => p.MaxDrawdown) : 0,
                IsActive = s.IsActive,
                LastUsed = s.CreatedAt
            };
        }).ToList();
    }

    private async Task<List<SymbolAnalytics>> GetSymbolBreakdownInternal(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var performances = await _context.StrategyPerformances
            .Where(p => p.UserId == userId && p.UpdatedAt >= startDate && p.UpdatedAt <= endDate)
            .GroupBy(p => p.Symbol)
            .ToListAsync();

        return performances.Select(g => new SymbolAnalytics
        {
            Symbol = g.Key,
            Return = g.Sum(p => p.TotalReturn),
            ReturnPercentage = g.Sum(p => p.TotalReturn),
            WinRate = g.Average(p => p.WinRate),
            TradeCount = g.Sum(p => p.TotalTrades),
            Volume = g.Sum(p => p.TotalTrades) * 1000, // Mock volume calculation
            AverageTradeSize = g.Any() ? g.Sum(p => p.TotalTrades) / g.Count() : 0,
            ProfitFactor = g.Where(p => p.TotalReturn > 0).Sum(p => p.TotalReturn) / 
                          Math.Max(Math.Abs(g.Where(p => p.TotalReturn < 0).Sum(p => p.TotalReturn)), 0.01m)
        }).ToList();
    }

    private async Task<RiskMetrics> CalculateRiskMetrics(Guid userId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var performances = await _context.StrategyPerformances
            .Where(p => p.UserId == userId && p.UpdatedAt >= startDate && p.UpdatedAt <= endDate)
            .Select(p => p.TotalReturn)
            .ToListAsync();

        if (!performances.Any())
        {
            return new RiskMetrics();
        }

        var mean = performances.Average();
        var variance = performances.Sum(p => (double)Math.Pow((double)(p - mean), 2)) / performances.Count;
        var volatility = (decimal)Math.Sqrt(variance * 252); // Annualized

        // Calculate VaR (95% confidence)
        var sortedReturns = performances.OrderBy(p => p).ToList();
        var varIndex = (int)(performances.Count * 0.05);
        var valueAtRisk = sortedReturns[Math.Max(0, varIndex - 1)];

        return new RiskMetrics
        {
            ValueAtRisk = Math.Abs(valueAtRisk),
            Volatility = volatility,
            SortinoRatio = volatility > 0 ? mean / volatility : 0,
            DownsideDeviation = performances.Min(),
            Beta = 1.0m, // Mock beta - would need market data for real calculation
            Alpha = mean * 252 // Annualized excess return
        };
    }

    private string GetHeatmapColor(decimal normalizedValue)
    {
        if (normalizedValue <= 0.33m) return "#ff4444"; // Red for poor performance
        if (normalizedValue <= 0.66m) return "#ffaa00"; // Orange for medium performance
        return "#44ff44"; // Green for good performance
    }

    // Placeholder implementations for remaining methods
    public Task<PerformanceAnalytics> GetStrategyPerformanceAsync(Guid userId, Guid strategyId, string period = "30D") => throw new NotImplementedException();
    public Task<List<StrategyAnalytics>> GetTopPerformingStrategiesAsync(Guid userId, int limit = 10) => throw new NotImplementedException();
    public Task<List<SymbolAnalytics>> GetSymbolPerformanceAsync(Guid userId, string period = "30D") => throw new NotImplementedException();
    public Task<ChartDataResponse> GetWinRateChartAsync(Guid userId, string period = "30D") => throw new NotImplementedException();
    public Task<ChartDataResponse> GetVolumeAnalysisAsync(Guid userId, string period = "30D") => throw new NotImplementedException();
    public Task<ChartDataResponse> GetDrawdownAnalysisAsync(Guid userId, string period = "30D") => throw new NotImplementedException();
    public Task<RiskMetrics> GetRiskAnalyticsAsync(Guid userId, string period = "30D") => throw new NotImplementedException();
    public Task<ChartDataResponse> GetVolatilityAnalysisAsync(Guid userId, string period = "30D") => throw new NotImplementedException();
    public Task<ChartDataResponse> GetCorrelationAnalysisAsync(Guid userId, List<string> symbols) => throw new NotImplementedException();
    public Task<ChartDataResponse> GetBenchmarkComparisonAsync(Guid userId, string benchmark = "SPY", string period = "30D") => throw new NotImplementedException();
    public Task<List<PerformanceAnalytics>> GetPeerComparisonAsync(Guid userId, int peerCount = 10) => throw new NotImplementedException();
    public Task<Dictionary<string, decimal>> GetRealTimeMetricsAsync(Guid userId) => throw new NotImplementedException();
    public Task<List<ChartDataPoint>> GetRecentTradeActivityAsync(Guid userId, int hours = 24) => throw new NotImplementedException();
    public Task<byte[]> ExportAnalyticsReportAsync(Guid userId, string period = "30D", string format = "PDF") => throw new NotImplementedException();
    public Task<string> GenerateAnalyticsSummaryAsync(Guid userId, string period = "30D") => throw new NotImplementedException();
}