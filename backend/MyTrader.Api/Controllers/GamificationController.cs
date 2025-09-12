using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Gamification;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/gamification")]
[Tags("Gamification & Achievements")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;

    public GamificationController(IGamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("achievements")]
    public async Task<ActionResult> GetUserAchievements()
    {
        try
        {
            var userId = GetUserId();
            var achievements = await _gamificationService.GetUserAchievementsAsync(userId);

            return Ok(new
            {
                success = true,
                data = achievements.Select(a => new
                {
                    id = a.Id,
                    type = a.AchievementType,
                    name = a.AchievementName,
                    description = a.Description,
                    icon = a.Icon,
                    points = a.Points,
                    earned_at = a.EarnedAt
                })
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to load achievements" });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetUserStats()
    {
        try
        {
            var userId = GetUserId();
            var stats = await _gamificationService.GetUserStatsAsync(userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    total_strategies = stats.TotalStrategies,
                    active_strategies = stats.ActiveStrategies,
                    best_return = stats.BestReturn,
                    average_return = stats.AverageReturn,
                    best_win_rate = stats.BestWinRate,
                    total_trades = stats.TotalTrades,
                    total_achievements = stats.TotalAchievements,
                    total_points = stats.TotalPoints,
                    global_rank = stats.GlobalRank,
                    last_activity = stats.LastActivityAt
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to load user stats" });
        }
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult> GetLeaderboard([FromQuery] string metric = "TotalReturn", [FromQuery] int limit = 10)
    {
        try
        {
            var leaderboard = await _gamificationService.GetLeaderboardAsync(metric, limit);

            return Ok(new
            {
                success = true,
                data = new
                {
                    metric = metric,
                    entries = leaderboard.Select(e => new
                    {
                        user_id = e.UserId,
                        user_name = e.UserName,
                        value = e.Value,
                        rank = e.Rank,
                        total_achievements = e.TotalAchievements,
                        total_points = e.TotalPoints
                    })
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to load leaderboard" });
        }
    }

    [HttpGet("performance-history")]
    public async Task<ActionResult> GetPerformanceHistory()
    {
        try
        {
            var userId = GetUserId();
            var history = await _gamificationService.GetUserPerformanceHistoryAsync(userId);

            return Ok(new
            {
                success = true,
                data = history.Select(p => new
                {
                    id = p.Id,
                    strategy_id = p.StrategyId,
                    symbol = p.Symbol,
                    total_return = p.TotalReturn,
                    win_rate = p.WinRate,
                    max_drawdown = p.MaxDrawdown,
                    sharpe_ratio = p.SharpeRatio,
                    total_trades = p.TotalTrades,
                    profitable_trades = p.ProfitableTrades,
                    start_date = p.StartDate,
                    end_date = p.EndDate,
                    updated_at = p.UpdatedAt
                })
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to load performance history" });
        }
    }

    [HttpPost("record-performance")]
    public async Task<ActionResult> RecordPerformance([FromBody] RecordPerformanceRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            var performance = await _gamificationService.RecordStrategyPerformanceAsync(
                request.StrategyId,
                userId,
                request.Symbol,
                request.TotalReturn,
                request.WinRate,
                request.MaxDrawdown,
                request.SharpeRatio,
                request.TotalTrades,
                request.ProfitableTrades,
                request.StartDate,
                request.EndDate
            );

            return Ok(new
            {
                success = true,
                message = "Performance recorded successfully",
                data = new
                {
                    id = performance.Id,
                    total_return = performance.TotalReturn,
                    win_rate = performance.WinRate
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to record performance" });
        }
    }

    [HttpGet("rank")]
    public async Task<ActionResult> GetUserRank([FromQuery] string metric = "TotalReturn")
    {
        try
        {
            var userId = GetUserId();
            var rank = await _gamificationService.GetUserRankAsync(userId, metric);

            return Ok(new
            {
                success = true,
                data = new
                {
                    user_id = userId,
                    rank = rank,
                    metric = metric
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Failed to get user rank" });
        }
    }
}

public class RecordPerformanceRequest
{
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal TotalReturn { get; set; }
    public decimal WinRate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public int TotalTrades { get; set; }
    public int ProfitableTrades { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
}