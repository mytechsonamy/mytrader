using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTrader.Application.Interfaces;
using MyTrader.Infrastructure;

namespace MyTrader.Jobs;

public class BackfillJob
{
    private readonly AppDbContext _db;
    private readonly IHistoricalDataProvider _hist;
    public BackfillJob(AppDbContext db, IHistoricalDataProvider hist) { _db = db; _hist = hist; }

    public async Task RunAsync(Guid symbolId, string timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var sym = await _db.Symbols.FirstAsync(s => s.Id == symbolId, ct);
        var klines = await _hist.GetAsync(sym.Ticker, timeframe, from, to, ct);

        foreach (var k in klines)
        {
            await _db.Database.ExecuteSqlRawAsync(@"
INSERT INTO candles (symbol_id, timeframe, ts, open, high, low, close, volume)
VALUES ({0},{1},{2},{3},{4},{5},{6},{7})
ON CONFLICT(symbol_id, timeframe, ts) DO UPDATE SET
open=EXCLUDED.open, high=EXCLUDED.high, low=EXCLUDED.low, close=EXCLUDED.close, volume=EXCLUDED.volume
            ", sym.Id, timeframe, k.Ts, k.Open, k.High, k.Low, k.Close, k.Volume);
        }
    }
}
