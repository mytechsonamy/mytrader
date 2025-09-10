using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTrader.Api.Services;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/backfill")]
public class BackfillController : ControllerBase
{
    private readonly ILogger<BackfillController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public BackfillController(ILogger<BackfillController> logger, IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Backfill([FromBody] BackfillRequest req, CancellationToken ct)
    {
        if (req.Symbols == null || req.Symbols.Length == 0)
            return BadRequest("symbols required");

        var intervals = (req.Intervals is { Length: > 0 } ? req.Intervals : new[] { "5m", "15m", "1h", "4h", "1d", "1w" })!;
        var start = req.StartUtc ?? DateTime.UtcNow.AddDays(-7);
        var end = req.EndUtc ?? DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        var binance = new BinanceKlinesClient(_httpClientFactory.CreateClient());

        var total = 0;
        foreach (var symbol in req.Symbols)
        {
            foreach (var interval in intervals)
            {
                // Resolve SymbolId for ticker (create if missing)
                var venue = "BINANCE";
                var symbolRow = await db.Symbols.FirstOrDefaultAsync(s => s.Ticker == symbol && s.Venue == venue, ct);
                if (symbolRow == null)
                {
                    symbolRow = new Symbol { Id = Guid.NewGuid(), Ticker = symbol, Venue = venue, AssetClass = "CRYPTO", IsActive = true, IsTracked = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    db.Symbols.Add(symbolRow);
                    await db.SaveChangesAsync(ct);
                }

                var count = 0;
                await foreach (var k in binance.GetKlinesAsync(symbol, interval, start, end, 1000, ct))
                {
                    var id = Guid.NewGuid();
                    await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO candles (""Id"",""SymbolId"",""Timeframe"",""Timestamp"",""Open"",""High"",""Low"",""Close"",""Volume"",""IsFinalized"",""CreatedAt"")
VALUES ({id},{symbolRow.Id},{interval},{k.OpenTime},{k.Open},{k.High},{k.Low},{k.Close},{k.Volume}, TRUE, NOW())
ON CONFLICT (""SymbolId"",""Timeframe"",""Timestamp"") DO UPDATE SET
  ""Open"" = EXCLUDED.""Open"",
  ""High"" = GREATEST(candles.""High"", EXCLUDED.""High""),
  ""Low""  = LEAST(candles.""Low"", EXCLUDED.""Low""),
  ""Close""= EXCLUDED.""Close"",
  ""Volume""= EXCLUDED.""Volume"",
  ""IsFinalized""= TRUE;", ct);

                    count++;
                }
                total += count;
                _logger.LogInformation("Backfilled {Count} klines for {Symbol} {Interval}", count, symbol, interval);
            }
        }

        return Ok(new { message = "backfill completed", rows = total });
    }
}

public class BackfillRequest
{
    public string[] Symbols { get; set; } = Array.Empty<string>();
    public string[]? Intervals { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
}
