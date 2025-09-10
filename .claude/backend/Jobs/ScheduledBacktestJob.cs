using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTrader.Application.Interfaces;
using MyTrader.Infrastructure;

namespace MyTrader.Jobs;

public class ScheduledBacktestJob
{
    private readonly AppDbContext _db;
    public ScheduledBacktestJob(AppDbContext db) { _db = db; }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var strategies = await _db.UserStrategies.ToListAsync(ct);
        var symbols = await _db.Symbols.Where(s => EF.Property<bool>(s, "IsTracked")).ToListAsync(ct);

        foreach (var st in strategies)
        {
            foreach (var s in symbols)
            {
                // enqueue backtest - integrate with your queue/worker
            }
        }
    }
}
