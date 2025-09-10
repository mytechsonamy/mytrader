using MyTrader.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTrader.Services.Gamification;

public interface IGamificationService
{
    // Achievement Management
    Task<List<UserAchievement>> GetUserAchievementsAsync(Guid userId);
    Task<UserAchievement> AwardAchievementAsync(Guid userId, string achievementType, string name, string description, int points, string icon = "🏆");
    Task CheckAndAwardPerformanceAchievementsAsync(Guid userId, StrategyPerformance performance);
    
    // Performance Tracking
    Task<StrategyPerformance> RecordStrategyPerformanceAsync(Guid strategyId, Guid userId, string symbol, decimal totalReturn, decimal winRate, decimal maxDrawdown, decimal sharpeRatio, int totalTrades, int profitableTrades, DateTimeOffset startDate, DateTimeOffset endDate);
    Task<List<StrategyPerformance>> GetUserPerformanceHistoryAsync(Guid userId);
    Task<StrategyPerformance?> GetBestPerformanceAsync(Guid userId, string metric = "TotalReturn");
    
    // Leaderboard & Stats
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(string metric = "TotalReturn", int limit = 10);
    Task<UserStats> GetUserStatsAsync(Guid userId);
    Task<int> GetUserRankAsync(Guid userId, string metric = "TotalReturn");
}

public record LeaderboardEntry(
    Guid UserId,
    string UserName,
    decimal Value,
    int Rank,
    string Metric,
    int TotalAchievements,
    int TotalPoints
);

public record UserStats(
    Guid UserId,
    int TotalStrategies,
    int ActiveStrategies,
    decimal BestReturn,
    decimal AverageReturn,
    decimal BestWinRate,
    int TotalTrades,
    int TotalAchievements,
    int TotalPoints,
    int GlobalRank,
    DateTimeOffset LastActivityAt
);