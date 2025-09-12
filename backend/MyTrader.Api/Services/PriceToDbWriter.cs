using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MyTrader.Infrastructure.Data;
using MyTrader.Core.Models;
using MyTrader.Services.Market;
using System.Collections.Concurrent;

namespace MyTrader.Api.Services;

public class PriceToDbWriter : IHostedService
{
    private readonly ILogger<PriceToDbWriter> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string[] _timeframes = new[] { "5m", "15m", "1h", "4h", "1d", "1w" };
    // Symbols will be fetched dynamically from database
    private readonly Random _random = new Random();
    #pragma warning disable CS0649 // Field is never assigned - intentionally disabled
    private readonly Timer? _timer;
    #pragma warning restore CS0649
    
    // Her asset için son DB yazım zamanını takip et
    private readonly ConcurrentDictionary<string, DateTime> _lastWriteTime = new();
    private readonly TimeSpan _writeInterval = TimeSpan.FromMinutes(1); // 1 dakikada bir yazım

    public PriceToDbWriter(
        ILogger<PriceToDbWriter> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PriceToDbWriter DISABLED - no database writes will be performed to prevent memory issues");
        // Timer disabled to stop excessive DB writes
        // _timer = new Timer(OnPrice, null, TimeSpan.Zero, TimeSpan.FromSeconds(10)); // Her 10 saniyede veri simüle et
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _logger.LogInformation("PriceToDbWriter stopped");
        return Task.CompletedTask;
    }

    private void OnPrice(object? state)
    {
        // COMPLETELY DISABLED - No database writes to prevent memory issues
        _logger.LogDebug("OnPrice called but DISABLED to prevent excessive DB writes");
    }

    /// <summary>
    /// Gerçek zamanlı price data'sı için kullanılacak metot
    /// COMPLETELY DISABLED to prevent memory issues
    /// </summary>
    public async Task ProcessPriceUpdateAsync(string symbol, decimal price, decimal volume)
    {
        // COMPLETELY DISABLED - No database writes to prevent memory issues
        _logger.LogDebug("ProcessPriceUpdateAsync called for {Symbol} but DISABLED to prevent excessive DB writes", symbol);
        await Task.CompletedTask;
    }

    private static DateTime AlignToTimeframe(DateTime ts, string timeframe)
    {
        ts = ts.ToUniversalTime();
        return timeframe switch
        {
            "1m" => new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, DateTimeKind.Utc),
            "5m" => new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, (ts.Minute / 5) * 5, 0, DateTimeKind.Utc),
            "15m" => new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, (ts.Minute / 15) * 15, 0, DateTimeKind.Utc),
            "1h" => new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, 0, 0, DateTimeKind.Utc),
            "4h" => new DateTime(ts.Year, ts.Month, ts.Day, (ts.Hour / 4) * 4, 0, 0, DateTimeKind.Utc),
            "1d" => new DateTime(ts.Year, ts.Month, ts.Day, 0, 0, 0, DateTimeKind.Utc),
            "1w" =>
                // ISO week: start Monday 00:00 UTC
                new DateTime(ts.Year, ts.Month, ts.Day, 0, 0, 0, DateTimeKind.Utc)
                    .AddDays(-(int)(ts.DayOfWeek == DayOfWeek.Sunday ? 6 : (ts.DayOfWeek - DayOfWeek.Monday))),
            _ => ts
        };
    }

    private static async Task UpsertMarketDataAsync(TradingDbContext db, string symbol, string timeframe, DateTime ts,
        decimal open, decimal high, decimal low, decimal close, decimal volume)
    {
        // Align timestamp to the timeframe bucket to enforce once-per-interval writes
        ts = AlignToTimeframe(ts, timeframe);

        var existing = await db.MarketData.FirstOrDefaultAsync(m => m.Symbol == symbol && m.Timeframe == timeframe && m.Timestamp == ts);
        if (existing != null)
        {
            existing.High = Math.Max(existing.High, high);
            existing.Low = existing.Low == 0 ? low : Math.Min(existing.Low, low);
            existing.Close = close;
            existing.Volume = volume;
        }
        else
        {
            await db.MarketData.AddAsync(new MarketData
            {
                Symbol = symbol,
                Timeframe = timeframe,
                Timestamp = ts,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            });
        }
    }

}
