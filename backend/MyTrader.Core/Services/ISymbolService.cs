using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Models;
using MyTrader.Core.Data;

namespace MyTrader.Core.Services;

public interface ISymbolService
{
    Task<List<Symbol>> GetAllSymbolsAsync();
    Task<Symbol?> GetSymbolByIdAsync(Guid id);
    Task<Symbol?> GetSymbolByTickerAsync(string ticker);
}

public class SymbolService : ISymbolService
{
    private readonly ITradingDbContext _context;
    
    public SymbolService(ITradingDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Symbol>> GetAllSymbolsAsync()
    {
        return await _context.Symbols.ToListAsync();
    }
    
    public async Task<Symbol?> GetSymbolByIdAsync(Guid id)
    {
        return await _context.Symbols.FirstOrDefaultAsync(s => s.Id == id);
    }
    
    public async Task<Symbol?> GetSymbolByTickerAsync(string ticker)
    {
        return await _context.Symbols.FirstOrDefaultAsync(s => s.Ticker == ticker);
    }
}