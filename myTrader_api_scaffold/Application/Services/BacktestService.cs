using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;
using MyTrader.Domain.Entities;
using MyTrader.Infrastructure;
using MyTrader.Realtime;

namespace MyTrader.Application.Services;

public class BacktestService : IBacktestService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<TradingHub> _hub;

    public BacktestService(AppDbContext db, IHubContext<TradingHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private static string Sha256(string s)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
    }

    public async Task<(Guid id, string status)> StartAsync(Guid userId, Guid strategyId, BacktestRequest request)
    {
        var cfg = new {
            request.Symbol, request.Timeframe, request.DateRangeStart, request.DateRangeEnd, StrategyId = strategyId
        };
        var snapshot = JsonSerializer.SerializeToDocument(cfg);
        var configHash = Sha256(JsonSerializer.Serialize(cfg));

        var bt = new Backtest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StrategyId = strategyId,
            Symbol = request.Symbol,
            Timeframe = request.Timeframe,
            DateRangeStart = request.DateRangeStart,
            DateRangeEnd = request.DateRangeEnd,
            ConfigSnapshot = snapshot,
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow,
            CodeRef = "unknown",
            EngineVersion = "1.0",
            IndicatorVersions = JsonDocument.Parse("{}"),
            ConfigHash = configHash
        };

        _db.Backtests.Add(bt);
        await _db.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            bt.TotalReturn = 0.1234m;
            bt.SharpeRatio = 1.25m;
            bt.MaxDrawdown = -0.08m;
            bt.TotalTrades = 42;
            bt.Status = "finished";
            bt.FinishedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            await _hub.Clients.Group($"user:{userId}").SendAsync("BacktestStatusUpdated", new {
                backtestId = bt.Id, status = bt.Status, finishedAt = bt.FinishedAt
            });
            await _hub.Clients.Group($"user:{userId}").SendAsync("BacktestMetricsUpdated", new {
                backtestId = bt.Id, totalReturn = bt.TotalReturn, sharpe = bt.SharpeRatio, maxDD = bt.MaxDrawdown, totalTrades = bt.TotalTrades
            });
        });

        return (bt.Id, bt.Status);
    }

    public async Task<BacktestResult?> GetResultAsync(Guid userId, Guid backtestId)
    {
        var bt = await _db.Backtests.FirstOrDefaultAsync(x => x.Id == backtestId && x.UserId == userId);
        if (bt == null) return null;

        return new BacktestResult(
            bt.Id, bt.Status, bt.StartedAt, bt.FinishedAt, bt.TotalReturn, bt.SharpeRatio, bt.MaxDrawdown, bt.TotalTrades
        );
    }
}
