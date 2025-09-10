using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MyTrader.Application.Interfaces;
using MyTrader.Infrastructure;

namespace MyTrader.Application.Services;

public class SymbolService : ISymbolService
{
    private readonly AppDbContext _db;
    public SymbolService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<SymbolDto>> GetTrackedAsync(string venue = "BINANCE", CancellationToken ct = default)
    {
        return await _db.Symbols
            .Where(s => EF.Property<bool>(s, "IsTracked") && s.Venue == venue)
            .Select(s => new SymbolDto(s.Id, s.Ticker, EF.Property<string>(s, "Display"), s.Venue, s.BaseCcy!, s.QuoteCcy!, EF.Property<bool>(s, "IsTracked")))
            .ToListAsync(ct);
    }

    public async Task<SymbolDto> GetOrCreateAsync(string ticker, string venue = "BINANCE", string? baseCcy = null, string? quoteCcy = null, CancellationToken ct = default)
    {
        var ex = await _db.Symbols.FirstOrDefaultAsync(s => s.Ticker == ticker && s.Venue == venue, ct);
        if (ex != null) return new SymbolDto(ex.Id, ex.Ticker, EF.Property<string>(ex, "Display"), ex.Venue, ex.BaseCcy!, ex.QuoteCcy!, EF.Property<bool>(ex, "IsTracked"));

        var (b, q) = InferCcy(ticker, baseCcy, quoteCcy);
        var entity = new Domain.Entities.Symbol
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Venue = venue,
            AssetClass = "CRYPTO",
            BaseCcy = b,
            QuoteCcy = q
        };
        _db.Entry(entity).Property("IsTracked").CurrentValue = true;
        _db.Entry(entity).Property("Display").CurrentValue = b ?? ticker;
        _db.Symbols.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new SymbolDto(entity.Id, entity.Ticker, b ?? entity.Ticker, entity.Venue, entity.BaseCcy!, entity.QuoteCcy!, true);
    }

    public async Task<bool> SetTrackedAsync(Guid symbolId, bool tracked, CancellationToken ct = default)
    {
        var s = await _db.Symbols.FirstOrDefaultAsync(x => x.Id == symbolId, ct);
        if (s == null) return false;
        _db.Entry(s).Property("IsTracked").CurrentValue = tracked;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SymbolDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _db.Symbols.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s == null) return null;
        return new SymbolDto(s.Id, s.Ticker, EF.Property<string>(s, "Display"), s.Venue, s.BaseCcy!, s.QuoteCcy!, EF.Property<bool>(s, "IsTracked"));
    }

    private (string? b, string? q) InferCcy(string ticker, string? b, string? q)
    {
        if (!string.IsNullOrWhiteSpace(b) && !string.IsNullOrWhiteSpace(q)) return (b, q);
        // naive inference for USDT/USDC/BUSD/TRY
        foreach (var quote in new[] { "USDT", "USDC", "BUSD", "TRY", "USD", "EUR" })
        {
            if (ticker.EndsWith(quote, StringComparison.OrdinalIgnoreCase))
                return (ticker[..^quote.Length], quote);
        }
        return (b, q);
    }
}
