using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.DTOs.Signals;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Market;

namespace MyTrader.Services.Signals;

public class SignalService : ISignalService
{
    private readonly TradingDbContext _context;
    private readonly ISymbolService _symbolService;
    private readonly ILogger<SignalService> _logger;

    public SignalService(TradingDbContext context, ISymbolService symbolService, ILogger<SignalService> logger)
    {
        _context = context;
        _symbolService = symbolService;
        _logger = logger;
    }

    public async Task<SignalsListResponse> GetSignalsAsync(int limit = 50, int cursor = 0)
    {
        try
        {
            // Clamp inputs
            limit = Math.Max(1, Math.Min(200, limit));
            cursor = Math.Max(0, cursor);

            var totalCount = await _context.Signals.CountAsync();
            
            var signals = await _context.Signals
                .Include(s => s.Symbol)
                .OrderByDescending(s => s.Timestamp)
                .Skip(cursor)
                .Take(limit)
                .Select(s => new SignalResponse
                {
                    Id = s.Id,
                    Symbol = s.Symbol.Ticker,
                    Signal = s.SignalType,
                    Price = s.Price,
                    Rsi = s.Rsi,
                    Macd = s.Macd,
                    Timestamp = s.Timestamp
                })
                .ToListAsync();

            var nextCursor = cursor + signals.Count < totalCount ? cursor + signals.Count : (int?)null;

            return new SignalsListResponse
            {
                Items = signals,
                NextCursor = nextCursor,
                Total = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signals");
            throw;
        }
    }

    public async Task<MarketDataResponse> GetCurrentMarketDataAsync()
    {
        try
        {
            // For demo purposes, we'll generate mock current market data
            // In a real implementation, this would fetch from a live data source
            var random = new Random();
            var price = 45000m + (decimal)(random.NextDouble() * 1000 - 500); // BTC price around 45k
            
            var rsi = 30 + (decimal)(random.NextDouble() * 40); // RSI between 30-70
            var macd = (decimal)(random.NextDouble() * 200 - 100); // MACD between -100 to 100
            
            var bbLower = price * 0.95m; // 5% below current price
            var bbUpper = price * 1.05m; // 5% above current price
            
            var bbPosition = price < bbLower ? "lower" : price > bbUpper ? "upper" : "middle";
            
            var currentSignal = rsi < 30 ? "BUY" : rsi > 70 ? "SELL" : "NEUTRAL";

            return new MarketDataResponse
            {
                Price = Math.Round(price, 2),
                CurrentSignal = currentSignal,
                BbPosition = bbPosition,
                Indicators = new MarketIndicatorValues
                {
                    Rsi = Math.Round(rsi, 1),
                    Macd = Math.Round(macd, 4),
                    BbLower = Math.Round(bbLower, 2),
                    BbUpper = Math.Round(bbUpper, 2)
                },
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current market data");
            throw;
        }
    }
}