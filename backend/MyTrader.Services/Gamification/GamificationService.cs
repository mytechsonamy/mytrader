using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyTrader.Services.Gamification;

public class GamificationService : IGamificationService
{
    private readonly TradingDbContext _context;
    private readonly ILogger<GamificationService> _logger;

    public GamificationService(TradingDbContext context, ILogger<GamificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserAchievement>> GetUserAchievementsAsync(Guid userId)
    {
        return await _context.UserAchievements
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.EarnedAt)
            .ToListAsync();
    }

    public async Task<UserAchievement> AwardAchievementAsync(Guid userId, string achievementType, string name, string description, int points, string icon = "🏆")
    {
        // Check if user already has this achievement
        var existing = await _context.UserAchievements
            .FirstOrDefaultAsync(a => a.UserId == userId && a.AchievementType == achievementType);

        if (existing != null)
        {
            _logger.LogDebug("User {UserId} already has achievement {AchievementType}", userId, achievementType);
            return existing;
        }

        var achievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementType = achievementType,
            AchievementName = name,
            Description = description,
            Icon = icon,
            Points = points,
            EarnedAt = DateTimeOffset.UtcNow
        };

        _context.UserAchievements.Add(achievement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Awarded achievement {AchievementType} to user {UserId} for {Points} points", 
            achievementType, userId, points);

        return achievement;
    }

    public async Task CheckAndAwardPerformanceAchievementsAsync(Guid userId, StrategyPerformance performance)
    {
        var achievements = new List<Task<UserAchievement>>();

        // First Strategy Achievement
        var strategyCount = await _context.StrategyPerformances.CountAsync(p => p.UserId == userId);
        if (strategyCount == 1)
        {
            achievements.Add(AwardAchievementAsync(userId, "FIRST_STRATEGY", "First Strategy", "Created your first trading strategy", 100, "🎯"));
        }

        // Performance-based achievements
        if (performance.TotalReturn > 10)
        {
            achievements.Add(AwardAchievementAsync(userId, "PROFITABLE_TRADER", "Profitable Trader", "Achieved >10% return", 250, "💰"));
        }

        if (performance.TotalReturn > 25)
        {
            achievements.Add(AwardAchievementAsync(userId, "HIGH_PERFORMER", "High Performer", "Achieved >25% return", 500, "🚀"));
        }

        if (performance.WinRate > 70)
        {
            achievements.Add(AwardAchievementAsync(userId, "WIN_STREAK", "Win Streak Master", "Achieved >70% win rate", 300, "🎯"));
        }

        if (performance.SharpeRatio > 1.5m)
        {
            achievements.Add(AwardAchievementAsync(userId, "RISK_ADJUSTED", "Risk-Adjusted Returns", "Achieved Sharpe ratio >1.5", 400, "📊"));
        }

        if (performance.MaxDrawdown < 5)
        {
            achievements.Add(AwardAchievementAsync(userId, "LOW_RISK", "Low Risk Master", "Kept drawdown <5%", 350, "🛡️"));
        }

        // Strategy count milestones
        if (strategyCount >= 5)
        {
            achievements.Add(AwardAchievementAsync(userId, "STRATEGY_CREATOR", "Strategy Creator", "Created 5+ strategies", 200, "⚙️"));
        }

        if (strategyCount >= 10)
        {
            achievements.Add(AwardAchievementAsync(userId, "STRATEGY_MASTER", "Strategy Master", "Created 10+ strategies", 500, "🧠"));
        }

        // Execute all achievement awards
        if (achievements.Any())
        {
            await Task.WhenAll(achievements);
        }
    }

    public async Task<StrategyPerformance> RecordStrategyPerformanceAsync(Guid strategyId, Guid userId, string symbol, decimal totalReturn, decimal winRate, decimal maxDrawdown, decimal sharpeRatio, int totalTrades, int profitableTrades, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var performance = new StrategyPerformance
        {
            Id = Guid.NewGuid(),
            StrategyId = strategyId,
            UserId = userId,
            Symbol = symbol,
            TotalReturn = totalReturn,
            WinRate = winRate,
            MaxDrawdown = maxDrawdown,
            SharpeRatio = sharpeRatio,
            TotalTrades = totalTrades,
            ProfitableTrades = profitableTrades,
            StartDate = startDate,
            EndDate = endDate,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.StrategyPerformances.Add(performance);
        await _context.SaveChangesAsync();

        // Check for achievements
        await CheckAndAwardPerformanceAchievementsAsync(userId, performance);

        return performance;
    }

    public async Task<List<StrategyPerformance>> GetUserPerformanceHistoryAsync(Guid userId)
    {
        return await _context.StrategyPerformances
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<StrategyPerformance?> GetBestPerformanceAsync(Guid userId, string metric = "TotalReturn")
    {
        var query = _context.StrategyPerformances.Where(p => p.UserId == userId);

        return metric.ToUpper() switch
        {
            "TOTALRETURN" => await query.OrderByDescending(p => p.TotalReturn).FirstOrDefaultAsync(),
            "WINRATE" => await query.OrderByDescending(p => p.WinRate).FirstOrDefaultAsync(),
            "SHARPERATIO" => await query.OrderByDescending(p => p.SharpeRatio).FirstOrDefaultAsync(),
            _ => await query.OrderByDescending(p => p.TotalReturn).FirstOrDefaultAsync()
        };
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(string metric = "TotalReturn", int limit = 10)
    {
        // First, get the performance data
        var performanceQuery = from p in _context.StrategyPerformances
                              join u in _context.Users on p.UserId equals u.Id
                              group p by new { p.UserId, u.FirstName, u.LastName } into g
                              select new
                              {
                                  UserId = g.Key.UserId,
                                  UserName = $"{g.Key.FirstName} {g.Key.LastName}",
                                  BestReturn = g.Max(x => x.TotalReturn),
                                  BestWinRate = g.Max(x => x.WinRate),
                                  BestSharpe = g.Max(x => x.SharpeRatio)
                              };

        var performanceResults = metric.ToUpper() switch
        {
            "WINRATE" => await performanceQuery.OrderByDescending(x => x.BestWinRate).Take(limit).ToListAsync(),
            "SHARPERATIO" => await performanceQuery.OrderByDescending(x => x.BestSharpe).Take(limit).ToListAsync(),
            _ => await performanceQuery.OrderByDescending(x => x.BestReturn).Take(limit).ToListAsync()
        };

        // Then, get achievements for these users separately
        var userIds = performanceResults.Select(r => r.UserId).ToList();
        var achievementStats = await _context.UserAchievements
            .Where(a => userIds.Contains(a.UserId))
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalAchievements = g.Count(),
                TotalPoints = g.Sum(a => a.Points)
            })
            .ToListAsync();

        // Combine the results
        return performanceResults.Select((r, index) =>
        {
            var achievements = achievementStats.FirstOrDefault(a => a.UserId == r.UserId);
            return new LeaderboardEntry(
                r.UserId,
                r.UserName,
                metric.ToUpper() switch
                {
                    "WINRATE" => r.BestWinRate,
                    "SHARPERATIO" => r.BestSharpe,
                    _ => r.BestReturn
                },
                index + 1,
                metric,
                achievements?.TotalAchievements ?? 0,
                achievements?.TotalPoints ?? 0
            );
        }).ToList();
    }

    public async Task<UserStats> GetUserStatsAsync(Guid userId)
    {
        var strategies = await _context.UserStrategies.Where(s => s.UserId == userId).ToListAsync();
        var performances = await _context.StrategyPerformances.Where(p => p.UserId == userId).ToListAsync();
        var achievements = await _context.UserAchievements.Where(a => a.UserId == userId).ToListAsync();
        
        var rank = await GetUserRankAsync(userId);

        return new UserStats(
            userId,
            strategies.Count,
            strategies.Count(s => s.IsActive),
            performances.Any() ? performances.Max(p => p.TotalReturn) : 0,
            performances.Any() ? performances.Average(p => p.TotalReturn) : 0,
            performances.Any() ? performances.Max(p => p.WinRate) : 0,
            performances.Sum(p => p.TotalTrades),
            achievements.Count,
            achievements.Sum(a => a.Points),
            rank,
            performances.Any() ? performances.Max(p => p.UpdatedAt) : DateTimeOffset.UtcNow
        );
    }

    public async Task<int> GetUserRankAsync(Guid userId, string metric = "TotalReturn")
    {
        var userBestPerformance = await GetBestPerformanceAsync(userId, metric);
        if (userBestPerformance == null) return 0;

        var userValue = metric.ToUpper() switch
        {
            "WINRATE" => userBestPerformance.WinRate,
            "SHARPERATIO" => userBestPerformance.SharpeRatio,
            _ => userBestPerformance.TotalReturn
        };

        IQueryable<decimal> valuesQuery;
        switch (metric.ToUpper())
        {
            case "WINRATE":
                valuesQuery = from p in _context.StrategyPerformances
                             group p by p.UserId into g
                             select g.Max(x => x.WinRate);
                break;
            case "SHARPERATIO":
                valuesQuery = from p in _context.StrategyPerformances
                             group p by p.UserId into g
                             select g.Max(x => x.SharpeRatio);
                break;
            default:
                valuesQuery = from p in _context.StrategyPerformances
                             group p by p.UserId into g
                             select g.Max(x => x.TotalReturn);
                break;
        }

        var betterPerformersCount = await valuesQuery.CountAsync(value => value > userValue);

        return betterPerformersCount + 1;
    }
}