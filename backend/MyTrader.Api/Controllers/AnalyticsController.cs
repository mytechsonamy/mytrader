using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Analytics;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Tags("Analytics & Charts")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("performance")]
    public async Task<ActionResult> GetPerformanceAnalytics([FromQuery] string period = "30D")
    {
        try
        {
            var userId = GetUserId();
            var analytics = await _analyticsService.GetPerformanceAnalyticsAsync(userId, period);

            return Ok(new
            {
                success = true,
                data = new
                {
                    user_id = analytics.UserId,
                    period = analytics.Period,
                    total_return = analytics.TotalReturn,
                    total_return_percentage = analytics.TotalReturnPercentage,
                    win_rate = analytics.WinRate,
                    total_trades = analytics.TotalTrades,
                    winning_trades = analytics.WinningTrades,
                    losing_trades = analytics.LosingTrades,
                    sharpe_ratio = analytics.SharpeRatio,
                    max_drawdown = analytics.MaxDrawdown,
                    daily_returns = analytics.DailyReturns.Select(d => new
                    {
                        date = d.Date,
                        return_value = d.Return,
                        return_percentage = d.ReturnPercentage,
                        cumulative_return = d.CumulativeReturn,
                        trade_count = d.TradeCount,
                        volume = d.Volume
                    }),
                    strategy_breakdown = analytics.StrategyBreakdown.Select(s => new
                    {
                        strategy_id = s.StrategyId,
                        strategy_name = s.StrategyName,
                        return_value = s.Return,
                        win_rate = s.WinRate,
                        trade_count = s.TradeCount,
                        sharpe_ratio = s.SharpeRatio,
                        is_active = s.IsActive
                    }),
                    symbol_breakdown = analytics.SymbolBreakdown.Select(s => new
                    {
                        symbol = s.Symbol,
                        return_value = s.Return,
                        win_rate = s.WinRate,
                        trade_count = s.TradeCount,
                        volume = s.Volume,
                        profit_factor = s.ProfitFactor
                    }),
                    risk_analysis = new
                    {
                        value_at_risk = analytics.RiskAnalysis.ValueAtRisk,
                        volatility = analytics.RiskAnalysis.Volatility,
                        sharpe_ratio = analytics.RiskAnalysis.SharpeRatio,
                        max_drawdown = analytics.RiskAnalysis.MaxDrawdown,
                        beta = analytics.RiskAnalysis.Beta,
                        alpha = analytics.RiskAnalysis.Alpha
                    },
                    period_start = analytics.PeriodStart,
                    period_end = analytics.PeriodEnd,
                    analysis_date = analytics.AnalysisDate
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get performance analytics" });
        }
    }

    [HttpGet("charts/equity-curve")]
    public async Task<ActionResult> GetEquityCurve([FromQuery] string period = "30D")
    {
        try
        {
            var userId = GetUserId();
            var equityCurve = await _analyticsService.GetEquityCurveAsync(userId, period);

            return Ok(new
            {
                success = true,
                data = new
                {
                    chart_type = equityCurve.ChartType,
                    title = equityCurve.Title,
                    period = equityCurve.Period,
                    starting_balance = equityCurve.StartingBalance,
                    ending_balance = equityCurve.EndingBalance,
                    peak_balance = equityCurve.PeakBalance,
                    lowest_balance = equityCurve.LowestBalance,
                    configuration = new
                    {
                        x_axis_type = equityCurve.Configuration.XAxisType,
                        y_axis_type = equityCurve.Configuration.YAxisType,
                        x_axis_label = equityCurve.Configuration.XAxisLabel,
                        y_axis_label = equityCurve.Configuration.YAxisLabel,
                        currency = equityCurve.Configuration.Currency,
                        decimal_places = equityCurve.Configuration.DecimalPlaces
                    },
                    series = equityCurve.Series.Select(s => new
                    {
                        name = s.Name,
                        type = s.Type,
                        color = s.Color,
                        data = s.Data.Select(d => new
                        {
                            timestamp = d.Timestamp,
                            value = d.Value,
                            volume = d.Volume
                        })
                    }),
                    drawdown_periods = equityCurve.DrawdownPeriods.Select(d => new
                    {
                        start_date = d.StartDate,
                        end_date = d.EndDate,
                        max_drawdown = d.MaxDrawdown,
                        max_drawdown_percentage = d.MaxDrawdownPercentage,
                        duration_days = d.DurationDays
                    }),
                    generated_at = equityCurve.GeneratedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get equity curve" });
        }
    }

    [HttpGet("charts/pnl")]
    public async Task<ActionResult> GetPnLChart([FromQuery] string period = "30D", [FromQuery] string interval = "1D")
    {
        try
        {
            var userId = GetUserId();
            var pnlChart = await _analyticsService.GetPnLChartAsync(userId, period, interval);

            return Ok(new
            {
                success = true,
                data = new
                {
                    chart_type = pnlChart.ChartType,
                    title = pnlChart.Title,
                    period = pnlChart.Period,
                    configuration = new
                    {
                        x_axis_label = pnlChart.Configuration.XAxisLabel,
                        y_axis_label = pnlChart.Configuration.YAxisLabel,
                        currency = pnlChart.Configuration.Currency
                    },
                    series = pnlChart.Series.Select(s => new
                    {
                        name = s.Name,
                        type = s.Type,
                        color = s.Color,
                        data = s.Data.Select(d => new
                        {
                            timestamp = d.Timestamp,
                            value = d.Value,
                            volume = d.Volume,
                            metadata = d.Metadata
                        })
                    }),
                    generated_at = pnlChart.GeneratedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get P&L chart" });
        }
    }

    [HttpGet("charts/pnl-distribution")]
    public async Task<ActionResult> GetPnLDistribution([FromQuery] string period = "30D")
    {
        try
        {
            var userId = GetUserId();
            var distribution = await _analyticsService.GetPnLDistributionAsync(userId, period);

            return Ok(new
            {
                success = true,
                data = new
                {
                    chart_type = distribution.ChartType,
                    title = distribution.Title,
                    period = distribution.Period,
                    distribution = distribution.Distribution.Select(b => new
                    {
                        range_start = b.RangeStart,
                        range_end = b.RangeEnd,
                        count = b.Count,
                        frequency = b.Frequency,
                        cumulative_frequency = b.CumulativeFrequency
                    }),
                    statistics = new
                    {
                        mean_pnl = distribution.MeanPnL,
                        median_pnl = distribution.MedianPnL,
                        standard_deviation = distribution.StandardDeviation,
                        skewness = distribution.Skewness,
                        kurtosis = distribution.Kurtosis
                    },
                    series = distribution.Series.Select(s => new
                    {
                        name = s.Name,
                        type = s.Type,
                        data = s.Data.Select(d => new
                        {
                            value = d.Value,
                            label = d.Label
                        })
                    }),
                    generated_at = distribution.GeneratedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get P&L distribution" });
        }
    }

    [HttpGet("charts/strategy-heatmap")]
    public async Task<ActionResult> GetStrategyHeatmap()
    {
        try
        {
            var userId = GetUserId();
            var heatmap = await _analyticsService.GetStrategyHeatmapAsync(userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    chart_type = heatmap.ChartType,
                    title = heatmap.Title,
                    period = heatmap.Period,
                    heatmap_points = heatmap.HeatmapPoints.Select(p => new
                    {
                        x_category = p.XCategory,
                        y_category = p.YCategory,
                        value = p.Value,
                        display_value = p.DisplayValue,
                        color = p.Color
                    }),
                    scale = new
                    {
                        min_value = heatmap.Scale.MinValue,
                        max_value = heatmap.Scale.MaxValue,
                        color_scale = heatmap.Scale.ColorScale
                    },
                    generated_at = heatmap.GeneratedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get strategy heatmap" });
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> GetAnalyticsDashboard([FromQuery] string period = "30D")
    {
        try
        {
            var userId = GetUserId();
            
            // Get multiple analytics in parallel for dashboard
            var performanceTask = _analyticsService.GetPerformanceAnalyticsAsync(userId, period);
            var equityCurveTask = _analyticsService.GetEquityCurveAsync(userId, period);
            var pnlChartTask = _analyticsService.GetPnLChartAsync(userId, period);
            var heatmapTask = _analyticsService.GetStrategyHeatmapAsync(userId);

            await Task.WhenAll(performanceTask, equityCurveTask, pnlChartTask, heatmapTask);

            var performance = await performanceTask;
            var equityCurve = await equityCurveTask;
            var pnlChart = await pnlChartTask;
            var heatmap = await heatmapTask;

            return Ok(new
            {
                success = true,
                data = new
                {
                    period = period,
                    performance_summary = new
                    {
                        total_return = performance.TotalReturn,
                        win_rate = performance.WinRate,
                        total_trades = performance.TotalTrades,
                        sharpe_ratio = performance.SharpeRatio,
                        max_drawdown = performance.MaxDrawdown
                    },
                    charts = new
                    {
                        equity_curve = new
                        {
                            starting_balance = equityCurve.StartingBalance,
                            ending_balance = equityCurve.EndingBalance,
                            series = equityCurve.Series.FirstOrDefault()?.Data.TakeLast(30)
                        },
                        pnl_summary = new
                        {
                            positive_days = pnlChart.Series.FirstOrDefault()?.Data.Count(d => d.Value > 0) ?? 0,
                            negative_days = pnlChart.Series.FirstOrDefault()?.Data.Count(d => d.Value < 0) ?? 0,
                            recent_pnl = pnlChart.Series.FirstOrDefault()?.Data.TakeLast(7)
                        },
                        strategy_performance = heatmap.HeatmapPoints.Take(10)
                    },
                    generated_at = DateTimeOffset.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get analytics dashboard" });
        }
    }
}