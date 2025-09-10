using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyTrader.Application.Interfaces;
using MyTrader.Contracts;
using MyTrader.Infrastructure;

namespace MyTrader.Application.Services;

public class MarketDataService : IMarketDataService
{
    private readonly AppDbContext _db;

    public MarketDataService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CandleDto>> GetCandlesAsync(string symbol, string timeframe, DateTimeOffset from, DateTimeOffset to)
    {
        var sym = await _db.Symbols.FirstOrDefaultAsync(s => s.Ticker == symbol);
        if (sym == null) return Array.Empty<CandleDto>();

        var rows = await _db.Candles
            .Where(c => c.SymbolId == sym.Id && c.Timeframe == timeframe && c.Timestamp >= from && c.Timestamp <= to)
            .OrderBy(c => c.Timestamp)
            .Select(c => new CandleDto(c.Timestamp, c.Open, c.High, c.Low, c.Close, c.Volume))
            .ToListAsync();
        return rows;
    }
}
