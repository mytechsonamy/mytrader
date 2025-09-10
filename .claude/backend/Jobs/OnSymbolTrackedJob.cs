using System;
using System.Threading.Tasks;

namespace MyTrader.Jobs;

public class OnSymbolTrackedJob
{
    public Task RunAsync(Guid symbolId)
    {
        // Backfill and then enqueue backtests for active strategies
        return Task.CompletedTask;
    }
}
