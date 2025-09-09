using MyTrader.Core.Models;
using MyTrader.Core.Models.Indicators;

namespace MyTrader.Services.Trading;

public interface ITradingStrategyService
{
    Task<SignalType> AnalyzeSignalAsync(string symbol, List<MarketData> marketData, StrategyParameters parameters);
    Task<List<Signal>> GetSignalsAsync(string symbol, int limit = 100);
    Task SaveSignalAsync(Signal signal);
}

public enum SignalType
{
    BUY,
    SELL,
    NEUTRAL
}

public class StrategyParameters
{
    public BollingerBandSettings BollingerBands { get; set; } = new();
    public RSISettings RSI { get; set; } = new();
    public MACDSettings MACD { get; set; } = new();
}