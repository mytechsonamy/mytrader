using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Gamification;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/competition")]
[Tags("Gamification & Achievements")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;
    private readonly ILogger<GamificationController> _logger;

    public GamificationController(IGamificationService gamificationService, ILogger<GamificationController> logger)
    {
        _gamificationService = gamificationService;
        _logger = logger;
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
    [AllowAnonymous] // Allow public access for testing - will be updated later with proper auth
    public async Task<ActionResult> GetCompetitionStats()
    {
        try
        {
            // For authenticated users, return user-specific stats
            if (User.Identity?.IsAuthenticated == true)
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
                        last_activity = stats.LastActivityAt,
                        totalParticipants = 100, // This should be fetched from a service
                        topScore = 2500.75m,
                        currentPeriod = "weekly"
                    }
                });
            }
            else
            {
                // For unauthenticated users, return general competition stats
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalParticipants = 100,
                        averageReturn = 8.5,
                        topScore = 2500.75,
                        currentPeriod = "weekly",
                        total_strategies = 0,
                        active_strategies = 0,
                        best_return = 0.0,
                        average_return = 0.0,
                        best_win_rate = 0.0,
                        total_trades = 0,
                        total_achievements = 0,
                        total_points = 0,
                        global_rank = 0,
                        last_activity = (DateTimeOffset?)null
                    }
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to load competition stats", error = ex.Message });
        }
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous] // Allow public access for leaderboard
    public async Task<ActionResult> GetLeaderboard([FromQuery] string metric = "TotalReturn", [FromQuery] int limit = 10)
    {
        try
        {
            var leaderboard = await _gamificationService.GetLeaderboardAsync(metric, limit);

            // CRITICAL: Always ensure entries is an array, never null or empty object
            var entries = new List<object>();

            if (leaderboard != null && leaderboard.Any())
            {
                entries = leaderboard.Select(e => (object)new
                {
                    user_id = e.UserId,
                    user_name = e.UserName ?? "Anonymous",
                    value = e.Value,
                    rank = e.Rank,
                    total_achievements = e.TotalAchievements,
                    total_points = e.TotalPoints
                }).ToList();
            }

            // If no real leaderboard data, provide mock data for testing
            if (entries.Count == 0)
            {
                entries = new List<object>
                {
                    new { user_id = Guid.NewGuid(), user_name = "TraderPro", value = 15.8m, rank = 1, total_achievements = 12, total_points = 2500 },
                    new { user_id = Guid.NewGuid(), user_name = "MarketMaster", value = 12.3m, rank = 2, total_achievements = 9, total_points = 2100 },
                    new { user_id = Guid.NewGuid(), user_name = "StockWizard", value = 9.7m, rank = 3, total_achievements = 7, total_points = 1800 },
                    new { user_id = Guid.NewGuid(), user_name = "CryptoKing", value = 8.2m, rank = 4, total_achievements = 6, total_points = 1650 },
                    new { user_id = Guid.NewGuid(), user_name = "InvestorAce", value = 6.9m, rank = 5, total_achievements = 5, total_points = 1450 }
                };
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    metric = metric,
                    entries = entries, // GUARANTEED to be an array
                    total_count = entries.Count,
                    timestamp = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard for metric: {Metric}", metric);

            // Even in error case, return empty array to prevent frontend crashes
            return Ok(new
            {
                success = false,
                message = "Failed to load leaderboard",
                data = new
                {
                    metric = metric,
                    entries = new List<object>(), // Empty array, not null
                    total_count = 0,
                    timestamp = DateTime.UtcNow
                },
                error = ex.Message
            });
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

    [HttpGet("my-ranking")]
    [AllowAnonymous] // Allow public access for testing - will be updated later with proper auth
    public async Task<ActionResult> GetMyRanking([FromQuery] string period = "weekly")
    {
        try
        {
            // For authenticated users, get actual ranking
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserId();
                var rank = await _gamificationService.GetUserRankAsync(userId, "TotalReturn");
                var stats = await _gamificationService.GetUserStatsAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        rank = rank,
                        totalParticipants = 100, // This could be fetched from a service method
                        score = stats.BestReturn,
                        period = period,
                        user_id = userId,
                        total_points = stats.TotalPoints,
                        global_rank = stats.GlobalRank
                    }
                });
            }
            else
            {
                // For unauthenticated users, return mock data for testing
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        rank = 1,
                        totalParticipants = 100,
                        score = 1250.50,
                        period = period,
                        user_id = (Guid?)null,
                        total_points = 0,
                        global_rank = 0
                    }
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get user ranking", error = ex.Message });
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