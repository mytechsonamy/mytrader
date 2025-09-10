using MyTrader.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTrader.Services.Market;

public record SymbolDto(Guid Id, string Ticker, string Display, string Venue, string BaseCcy, string QuoteCcy, bool IsTracked);

public interface ISymbolService
{
    // New agent pack methods
    Task<IReadOnlyList<SymbolDto>> GetTrackedAsync(string venue = "BINANCE", CancellationToken ct = default);
    Task<SymbolDto> GetOrCreateAsync(string ticker, string venue = "BINANCE", string? baseCcy = null, string? quoteCcy = null, CancellationToken ct = default);
    Task<bool> SetTrackedAsync(Guid symbolId, bool tracked, CancellationToken ct = default);
    Task<SymbolDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Legacy methods for backward compatibility
    Task<Symbol> GetOrCreateSymbolAsync(string ticker, string venue = "BINANCE");
    Task<Symbol?> GetSymbolAsync(Guid symbolId);
    Task<Symbol?> GetSymbolAsync(string ticker, string venue = "BINANCE");
    Task<List<Symbol>> GetActiveSymbolsAsync();
    Task<List<Symbol>> GetSymbolsByAssetClassAsync(string assetClass);
    Task UpdateSymbolMetadataAsync(Guid symbolId, object metadata);
    Task<List<Symbol>> BulkUpsertSymbolsAsync(List<(string ticker, string venue, string assetClass, string? baseCurrency, string? quoteCurrency)> symbols);
}