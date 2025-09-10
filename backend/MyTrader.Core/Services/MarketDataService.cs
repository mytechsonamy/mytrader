using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.Data;

namespace MyTrader.Core.Services;

public interface IMarketDataService
{
    Task<List<MarketData>> GetHistoricalDataAsync(Guid symbolId, DateTime startDate, DateTime endDate, string timeframe);
    Task<List<MarketData>> GetHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, string timeframe);
    Task<MarketData?> GetLatestDataAsync(Guid symbolId, string timeframe);
    Task<List<MarketData>> GetRecentDataAsync(Guid symbolId, string timeframe, int count = 100);
    Task<bool> HasSufficientDataAsync(Guid symbolId, string timeframe, DateTime startDate, int minRequired = 100);
}

public class MarketDataService : IMarketDataService
{
    private readonly ITradingDbContext _context;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(ITradingDbContext context, ILogger<MarketDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MarketData>> GetHistoricalDataAsync(Guid symbolId, DateTime startDate, DateTime endDate, string timeframe)
    {
        _logger.LogInformation("Fetching historical data for symbol {SymbolId} from {StartDate} to {EndDate} with timeframe {Timeframe}", 
            symbolId, startDate, endDate, timeframe);

        // First get symbol ticker
        var symbol = await _context.Symbols.FindAsync(symbolId);
        if (symbol == null)
        {
            throw new ArgumentException($"Symbol with ID {symbolId} not found");
        }

        return await GetHistoricalDataAsync(symbol.Ticker, startDate, endDate, timeframe);
    }

    public async Task<List<MarketData>> GetHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, string timeframe)
    {
        var data = await _context.MarketData
            .Where(md => md.Symbol == symbol && 
                        md.Timeframe == timeframe &&
                        md.Timestamp >= startDate &&
                        md.Timestamp <= endDate)
            .OrderBy(md => md.Timestamp)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} data points for symbol {Symbol}", data.Count, symbol);
        return data;
    }

    public async Task<MarketData?> GetLatestDataAsync(Guid symbolId, string timeframe)
    {
        var symbol = await _context.Symbols.FindAsync(symbolId);
        if (symbol == null) return null;

        return await _context.MarketData
            .Where(md => md.Symbol == symbol.Ticker && md.Timeframe == timeframe)
            .OrderByDescending(md => md.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MarketData>> GetRecentDataAsync(Guid symbolId, string timeframe, int count = 100)
    {
        var symbol = await _context.Symbols.FindAsync(symbolId);
        if (symbol == null) return new List<MarketData>();

        return await _context.MarketData
            .Where(md => md.Symbol == symbol.Ticker && md.Timeframe == timeframe)
            .OrderByDescending(md => md.Timestamp)
            .Take(count)
            .OrderBy(md => md.Timestamp)
            .ToListAsync();
    }

    public async Task<bool> HasSufficientDataAsync(Guid symbolId, string timeframe, DateTime startDate, int minRequired = 100)
    {
        var symbol = await _context.Symbols.FindAsync(symbolId);
        if (symbol == null) return false;

        var count = await _context.MarketData
            .Where(md => md.Symbol == symbol.Ticker && 
                        md.Timeframe == timeframe &&
                        md.Timestamp >= startDate)
            .CountAsync();

        return count >= minRequired;
    }
}