using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyTrader.Services.Market;

namespace MyTrader.Api.Jobs;

public class OnSymbolTrackedJob
{
    private readonly ISymbolService _symbolService;
    private readonly ILogger<OnSymbolTrackedJob> _logger;

    public OnSymbolTrackedJob(ISymbolService symbolService, ILogger<OnSymbolTrackedJob> logger)
    {
        _symbolService = symbolService;
        _logger = logger;
    }

    public async Task RunAsync(Guid symbolId)
    {
        _logger.LogInformation("Running OnSymbolTracked job for symbol {SymbolId}", symbolId);

        try
        {
            // Get symbol details
            var symbol = await _symbolService.GetByIdAsync(symbolId);
            if (symbol == null)
            {
                _logger.LogWarning("Symbol {SymbolId} not found", symbolId);
                return;
            }

            _logger.LogInformation("Symbol {Ticker} tracked, triggering backfill and backtest", symbol.Ticker);

            // TODO: Implement backfill job
            // await _backfillService.BackfillAsync(symbolId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            // TODO: Enqueue backtests for active strategies
            // await _backtestService.EnqueueBacktestsForSymbolAsync(symbolId);

            _logger.LogInformation("OnSymbolTracked job completed for symbol {Ticker}", symbol.Ticker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnSymbolTracked job for symbol {SymbolId}", symbolId);
            throw;
        }
    }
}