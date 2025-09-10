using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTrader.Application.Interfaces;

public record SymbolDto(Guid Id, string Ticker, string Display, string Venue, string BaseCcy, string QuoteCcy, bool IsTracked);

public interface ISymbolService
{
    Task<IReadOnlyList<SymbolDto>> GetTrackedAsync(string venue = "BINANCE", CancellationToken ct = default);
    Task<SymbolDto> GetOrCreateAsync(string ticker, string venue = "BINANCE", string? baseCcy = null, string? quoteCcy = null, CancellationToken ct = default);
    Task<bool> SetTrackedAsync(Guid symbolId, bool tracked, CancellationToken ct = default);
    Task<SymbolDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
