using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyTrader.Services.Market;

public class SymbolService : ISymbolService
{
    private readonly TradingDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SymbolService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(4);

    public SymbolService(TradingDbContext context, IMemoryCache cache, ILogger<SymbolService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // New agent pack methods
    public async Task<IReadOnlyList<SymbolDto>> GetTrackedAsync(string venue = "BINANCE", CancellationToken ct = default)
    {
        return await _context.Symbols
            .Where(s => s.IsTracked && s.Venue == venue)
            .Select(s => new SymbolDto(
                s.Id, 
                s.Ticker, 
                s.Display ?? s.BaseCurrency ?? s.Ticker,
                s.Venue, 
                s.BaseCurrency ?? "", 
                s.QuoteCurrency ?? "", 
                s.IsTracked
            ))
            .ToListAsync(ct);
    }

    public async Task<SymbolDto> GetOrCreateAsync(string ticker, string venue = "BINANCE", string? baseCcy = null, string? quoteCcy = null, CancellationToken ct = default)
    {
        var existing = await _context.Symbols.FirstOrDefaultAsync(s => s.Ticker == ticker && s.Venue == venue, ct);
        if (existing != null) 
        {
            return new SymbolDto(
                existing.Id, 
                existing.Ticker, 
                existing.Display ?? existing.BaseCurrency ?? existing.Ticker,
                existing.Venue, 
                existing.BaseCurrency ?? "", 
                existing.QuoteCurrency ?? "", 
                existing.IsTracked
            );
        }

        var (b, q) = InferCcy(ticker, baseCcy, quoteCcy);
        var entity = new Symbol
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Venue = venue,
            AssetClass = "CRYPTO",
            BaseCurrency = b,
            QuoteCurrency = q,
            FullName = b ?? ticker,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        entity.IsTracked = true;
        entity.Display = b ?? ticker;
        _context.Symbols.Add(entity);
        await _context.SaveChangesAsync(ct);
        
        return new SymbolDto(entity.Id, entity.Ticker, b ?? entity.Ticker, entity.Venue, entity.BaseCurrency!, entity.QuoteCurrency!, true);
    }

    public async Task<bool> SetTrackedAsync(Guid symbolId, bool tracked, CancellationToken ct = default)
    {
        var symbol = await _context.Symbols.FirstOrDefaultAsync(x => x.Id == symbolId, ct);
        if (symbol == null) return false;
        
        _context.Entry(symbol).Property("IsTracked").CurrentValue = tracked;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SymbolDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var symbol = await _context.Symbols.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (symbol == null) return null;
        
        return new SymbolDto(
            symbol.Id, 
            symbol.Ticker, 
            symbol.Display ?? symbol.BaseCurrency ?? symbol.Ticker,
            symbol.Venue, 
            symbol.BaseCurrency ?? "", 
            symbol.QuoteCurrency ?? "", 
            symbol.IsTracked
        );
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

    public async Task<Symbol> GetOrCreateSymbolAsync(string ticker, string venue = "BINANCE")
    {
        var cacheKey = $"symbol_{ticker}_{venue}";
        
        if (_cache.TryGetValue(cacheKey, out Symbol? cachedSymbol) && cachedSymbol != null)
        {
            return cachedSymbol;
        }

        var symbol = await _context.Symbols
            .FirstOrDefaultAsync(s => s.Ticker == ticker && s.Venue == venue);

        if (symbol == null)
        {
            // Create new symbol with sensible defaults
            symbol = new Symbol
            {
                Ticker = ticker,
                Venue = venue,
                AssetClass = DetermineAssetClass(ticker, venue),
                BaseCurrency = ExtractBaseCurrency(ticker),
                QuoteCurrency = ExtractQuoteCurrency(ticker),
                FullName = GenerateFullName(ticker),
                IsActive = true,
                IsTracked = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Symbols.Add(symbol);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new symbol: {Ticker} on {Venue}", ticker, venue);
        }

        _cache.Set(cacheKey, symbol, _cacheExpiration);
        return symbol;
    }

    public async Task<Symbol?> GetSymbolAsync(Guid symbolId)
    {
        var cacheKey = $"symbol_id_{symbolId}";
        
        if (_cache.TryGetValue(cacheKey, out Symbol? cachedSymbol))
        {
            return cachedSymbol;
        }

        var symbol = await _context.Symbols.FindAsync(symbolId);
        
        if (symbol != null)
        {
            _cache.Set(cacheKey, symbol, _cacheExpiration);
        }

        return symbol;
    }

    public async Task<Symbol?> GetSymbolAsync(string ticker, string venue = "BINANCE")
    {
        var cacheKey = $"symbol_{ticker}_{venue}";
        
        if (_cache.TryGetValue(cacheKey, out Symbol? cachedSymbol))
        {
            return cachedSymbol;
        }

        var symbol = await _context.Symbols
            .FirstOrDefaultAsync(s => s.Ticker == ticker && s.Venue == venue);

        if (symbol != null)
        {
            _cache.Set(cacheKey, symbol, _cacheExpiration);
        }

        return symbol;
    }

    public async Task<List<Symbol>> GetActiveSymbolsAsync()
    {
        var cacheKey = "active_symbols";
        
        if (_cache.TryGetValue(cacheKey, out List<Symbol>? cachedSymbols) && cachedSymbols != null)
        {
            return cachedSymbols;
        }

        var symbols = await _context.Symbols
            .Where(s => s.IsActive && s.IsTracked)
            .OrderBy(s => s.Ticker)
            .ToListAsync();

        _cache.Set(cacheKey, symbols, TimeSpan.FromMinutes(30));
        return symbols;
    }

    public async Task<List<Symbol>> GetSymbolsByAssetClassAsync(string assetClass)
    {
        var cacheKey = $"symbols_asset_{assetClass}";
        
        if (_cache.TryGetValue(cacheKey, out List<Symbol>? cachedSymbols) && cachedSymbols != null)
        {
            return cachedSymbols;
        }

        var symbols = await _context.Symbols
            .Where(s => s.AssetClass == assetClass && s.IsActive)
            .OrderBy(s => s.Ticker)
            .ToListAsync();

        _cache.Set(cacheKey, symbols, TimeSpan.FromHours(1));
        return symbols;
    }

    public async Task UpdateSymbolMetadataAsync(Guid symbolId, object metadata)
    {
        var symbol = await _context.Symbols.FindAsync(symbolId);
        if (symbol != null)
        {
            symbol.Metadata = JsonSerializer.Serialize(metadata);
            symbol.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            // Invalidate cache
            _cache.Remove($"symbol_id_{symbolId}");
            _cache.Remove($"symbol_{symbol.Ticker}_{symbol.Venue}");
        }
    }

    public async Task<List<Symbol>> BulkUpsertSymbolsAsync(List<(string ticker, string venue, string assetClass, string? baseCurrency, string? quoteCurrency)> symbols)
    {
        var result = new List<Symbol>();
        
        // Get existing symbols
        var tickers = symbols.Select(s => s.ticker).ToList();
        var existingSymbols = await _context.Symbols
            .Where(s => tickers.Contains(s.Ticker))
            .ToListAsync();

        foreach (var (ticker, venue, assetClass, baseCurrency, quoteCurrency) in symbols)
        {
            var existing = existingSymbols.FirstOrDefault(s => s.Ticker == ticker && s.Venue == venue);
            
            if (existing != null)
            {
                // Update existing
                existing.AssetClass = assetClass;
                existing.BaseCurrency = baseCurrency;
                existing.QuoteCurrency = quoteCurrency;
                existing.UpdatedAt = DateTime.UtcNow;
                result.Add(existing);
            }
            else
            {
                // Create new
                var newSymbol = new Symbol
                {
                    Ticker = ticker,
                    Venue = venue,
                    AssetClass = assetClass,
                    BaseCurrency = baseCurrency,
                    QuoteCurrency = quoteCurrency,
                    FullName = GenerateFullName(ticker),
                    IsActive = true,
                    IsTracked = true
                };
                
                _context.Symbols.Add(newSymbol);
                result.Add(newSymbol);
            }
        }

        await _context.SaveChangesAsync();
        
        // Clear relevant caches
        _cache.Remove("active_symbols");
        
        return result;
    }

    // Helper methods
    private string DetermineAssetClass(string ticker, string venue)
    {
        if (venue.Contains("BINANCE") || venue.Contains("COINBASE") || venue.Contains("CRYPTO"))
            return "CRYPTO";
        
        if (venue.Contains("FOREX") || venue.Contains("FX"))
            return "FX";
        
        if (venue.Contains("STOCK") || venue == "YF")
            return "EQUITY";
            
        // Default for crypto pairs
        if (ticker.Contains("USDT") || ticker.Contains("BTC") || ticker.Contains("ETH"))
            return "CRYPTO";
            
        return "UNKNOWN";
    }

    private string? ExtractBaseCurrency(string ticker)
    {
        // Common crypto pair patterns
        if (ticker.EndsWith("USDT"))
            return ticker[..^4];
        if (ticker.EndsWith("BTC"))
            return ticker[..^3];
        if (ticker.EndsWith("ETH"))
            return ticker[..^3];
        if (ticker.EndsWith("USD"))
            return ticker[..^3];
            
        return null;
    }

    private string? ExtractQuoteCurrency(string ticker)
    {
        if (ticker.EndsWith("USDT"))
            return "USDT";
        if (ticker.EndsWith("BTC"))
            return "BTC";
        if (ticker.EndsWith("ETH"))
            return "ETH";
        if (ticker.EndsWith("USD"))
            return "USD";
            
        return null;
    }

    private string GenerateFullName(string ticker)
    {
        var baseCurrency = ExtractBaseCurrency(ticker);
        var quoteCurrency = ExtractQuoteCurrency(ticker);
        
        if (!string.IsNullOrEmpty(baseCurrency) && !string.IsNullOrEmpty(quoteCurrency))
        {
            return $"{baseCurrency}/{quoteCurrency}";
        }
        
        return ticker;
    }
}