using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Market;

namespace MyTrader.Api.Jobs;

// Temporary structure for candlestick data until we have proper data provider interfaces
public record CandleData(DateTimeOffset Ts, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

public class BackfillJob
{
    private readonly TradingDbContext _context;
    private readonly ISymbolService _symbolService;
    private readonly ILogger<BackfillJob> _logger;

    public BackfillJob(
        TradingDbContext context, 
        ISymbolService symbolService,
        ILogger<BackfillJob> logger)
    {
        _context = context;
        _symbolService = symbolService;
        _logger = logger;
    }

    public async Task RunAsync(Guid symbolId, string timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        _logger.LogInformation("Running backfill job for symbol {SymbolId}, timeframe {Timeframe}, from {From} to {To}", 
            symbolId, timeframe, from, to);

        try
        {
            // Get symbol details
            var symbol = await _symbolService.GetByIdAsync(symbolId, ct);
            if (symbol == null)
            {
                _logger.LogWarning("Symbol with ID {SymbolId} not found", symbolId);
                return;
            }

            // TODO: Replace with actual historical data provider
            // var klines = await _historicalDataProvider.GetAsync(symbol.Ticker, timeframe, from, to, ct);

            // For now, generate mock data
            var klines = GenerateMockCandleData(from, to, timeframe);

            _logger.LogInformation("Retrieved {Count} candles for {Ticker}", klines.Count(), symbol.Ticker);

            foreach (var candle in klines)
            {
                // UPSERT candle data using PostgreSQL ON CONFLICT
                await _context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO candles (symbol_id, timeframe, ""Timestamp"", open, high, low, close, volume)
                    VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})
                    ON CONFLICT(symbol_id, timeframe, ""Timestamp"") DO UPDATE SET
                    open = EXCLUDED.open, 
                    high = EXCLUDED.high, 
                    low = EXCLUDED.low, 
                    close = EXCLUDED.close, 
                    volume = EXCLUDED.volume
                ", symbolId, timeframe, candle.Ts, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
            }

            _logger.LogInformation("Backfill completed for {Ticker}, inserted/updated {Count} candles", 
                symbol.Ticker, klines.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in backfill job for symbol {SymbolId}", symbolId);
            throw;
        }
    }

    private IEnumerable<CandleData> GenerateMockCandleData(DateTimeOffset from, DateTimeOffset to, string timeframe)
    {
        var candles = new List<CandleData>();
        var random = new Random();
        var current = from;
        var basePrice = 45000m; // Mock BTC price
        var currentPrice = basePrice;

        // Determine time increment based on timeframe
        var increment = timeframe switch
        {
            "1m" => TimeSpan.FromMinutes(1),
            "5m" => TimeSpan.FromMinutes(5),
            "15m" => TimeSpan.FromMinutes(15),
            "1h" => TimeSpan.FromHours(1),
            "4h" => TimeSpan.FromHours(4),
            "1d" => TimeSpan.FromDays(1),
            _ => TimeSpan.FromHours(1)
        };

        while (current <= to)
        {
            var open = currentPrice;
            var changePercent = (decimal)(random.NextDouble() * 0.04 - 0.02); // -2% to +2%
            var close = open * (1 + changePercent);
            
            var high = Math.Max(open, close) * (1 + (decimal)(random.NextDouble() * 0.01));
            var low = Math.Min(open, close) * (1 - (decimal)(random.NextDouble() * 0.01));
            var volume = (decimal)(random.NextDouble() * 1000 + 100);

            candles.Add(new CandleData(
                current,
                Math.Round(open, 2),
                Math.Round(high, 2),
                Math.Round(low, 2),
                Math.Round(close, 2),
                Math.Round(volume, 2)
            ));

            currentPrice = close;
            current = current.Add(increment);
        }

        return candles;
    }
}