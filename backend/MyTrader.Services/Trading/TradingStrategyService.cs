using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.Models.Indicators;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Services.Trading;

public class TradingStrategyService : ITradingStrategyService
{
    private readonly TradingDbContext _context;
    private readonly IIndicatorService _indicatorService;
    private readonly ILogger<TradingStrategyService> _logger;

    public TradingStrategyService(
        TradingDbContext context, 
        IIndicatorService indicatorService,
        ILogger<TradingStrategyService> logger)
    {
        _context = context;
        _indicatorService = indicatorService;
        _logger = logger;
    }

    public async Task<SignalType> AnalyzeSignalAsync(string symbol, List<MarketData> marketData, StrategyParameters parameters)
    {
        try
        {
            if (marketData.Count < 30) // Need minimum data for analysis
            {
                return SignalType.NEUTRAL;
            }

            var closePrices = marketData.Select(m => m.Close).ToList();
            var currentPrice = closePrices.Last();

            // Calculate indicators
            var bb = _indicatorService.CalculateBollingerBands(closePrices, parameters.BollingerBands);
            var rsi = _indicatorService.CalculateRSI(closePrices, parameters.RSI);
            var macd = _indicatorService.CalculateMACD(closePrices, parameters.MACD);

            // Determine Bollinger Band position
            string bbPosition;
            if (currentPrice <= bb.Lower)
                bbPosition = "lower";
            else if (currentPrice >= bb.Upper)
                bbPosition = "upper";
            else
                bbPosition = "middle";

            // Signal logic (simplified BB + MACD + RSI strategy)
            var signal = GenerateSignal(currentPrice, bb, rsi, macd, bbPosition);

            // Save the signal to database
            var signalEntity = new Signal
            {
                StrategyId = Guid.Empty, // For now, using empty GUID - in real app would come from strategy
                Symbol = symbol,
                SignalType = signal.ToString(),
                Price = currentPrice,
                Rsi = rsi.Value,
                Macd = macd.Value,
                BollingerBandUpper = bb.Upper,
                BollingerBandLower = bb.Lower,
                BollingerPosition = bbPosition,
                Timestamp = DateTime.UtcNow
            };

            await SaveSignalAsync(signalEntity);
            
            return signal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing signal for {Symbol}", symbol);
            return SignalType.NEUTRAL;
        }
    }

    public async Task<List<Signal>> GetSignalsAsync(string symbol, int limit = 100)
    {
        return await _context.Signals
            .Where(s => s.Symbol == symbol)
            .OrderByDescending(s => s.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task SaveSignalAsync(Signal signal)
    {
        try
        {
            await _context.Signals.AddAsync(signal);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving signal for {Symbol}", signal.Symbol);
            throw;
        }
    }

    private SignalType GenerateSignal(decimal currentPrice, BollingerBands bb, RSI rsi, MACD macd, string bbPosition)
    {
        var buySignals = 0;
        var sellSignals = 0;

        // Bollinger Bands logic
        if (bbPosition == "lower")
            buySignals++;
        else if (bbPosition == "upper")
            sellSignals++;

        // RSI logic
        if (rsi.Value < 30)
            buySignals++;
        else if (rsi.Value > 70)
            sellSignals++;

        // MACD logic (simplified)
        if (macd.Value > macd.Signal && macd.Histogram > 0)
            buySignals++;
        else if (macd.Value < macd.Signal && macd.Histogram < 0)
            sellSignals++;

        // Decision logic
        if (buySignals >= 2 && sellSignals == 0)
            return SignalType.BUY;
        else if (sellSignals >= 2 && buySignals == 0)
            return SignalType.SELL;
        else
            return SignalType.NEUTRAL;
    }
}