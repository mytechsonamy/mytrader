using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Market;

namespace MyTrader.Api.Jobs;

public class ScheduledBacktestJob
{
    private readonly TradingDbContext _context;
    private readonly ISymbolService _symbolService;
    private readonly ILogger<ScheduledBacktestJob> _logger;

    public ScheduledBacktestJob(
        TradingDbContext context, 
        ISymbolService symbolService,
        ILogger<ScheduledBacktestJob> logger)
    {
        _context = context;
        _symbolService = symbolService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running scheduled backtest job");

        try
        {
            // Get all active strategies
            var strategies = await _context.Strategies
                .Where(s => s.IsActive)
                .ToListAsync(ct);

            // Get all tracked symbols
            var trackedSymbols = await _symbolService.GetTrackedAsync("BINANCE", ct);

            _logger.LogInformation("Found {StrategyCount} strategies and {SymbolCount} tracked symbols", 
                strategies.Count, trackedSymbols.Count);

            foreach (var strategy in strategies)
            {
                foreach (var symbol in trackedSymbols)
                {
                    _logger.LogDebug("Enqueuing backtest for strategy {StrategyId} and symbol {Symbol}", 
                        strategy.Id, symbol.Ticker);

                    // TODO: Integrate with background job queue (Hangfire, etc.)
                    // await _backgroundJobClient.Enqueue<BacktestJob>(x => x.RunAsync(strategy.Id, symbol.Id));
                }
            }

            _logger.LogInformation("Scheduled backtest job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled backtest job");
            throw;
        }
    }
}