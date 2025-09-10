using MyTrader.Core.Models;
using MyTrader.Core.Models.Indicators;

namespace MyTrader.Services.Trading;

public interface IIndicatorService
{
    // Basic indicators
    BollingerBands CalculateBollingerBands(List<decimal> prices, BollingerBandSettings settings);
    RSI CalculateRSI(List<decimal> prices, RSISettings settings);
    MACD CalculateMACD(List<decimal> prices, MACDSettings settings);
    
    // Advanced indicators
    decimal CalculateEMA(List<decimal> prices, int period);
    decimal CalculateSMA(List<decimal> prices, int period);
    StochasticOscillator CalculateStochastic(List<decimal> highs, List<decimal> lows, List<decimal> closes, StochasticSettings settings);
    ATR CalculateATR(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period);
    Williams CalculateWilliamsR(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period);
    CCI CalculateCCI(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period);
    MFI CalculateMFI(List<decimal> highs, List<decimal> lows, List<decimal> closes, List<decimal> volumes, int period);
    
    // Volume indicators
    decimal CalculateVWAP(List<Candle> candles);
    decimal CalculateOBV(List<decimal> closes, List<decimal> volumes);
    
    // Trend indicators
    ADX CalculateADX(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period);
    Ichimoku CalculateIchimoku(List<decimal> highs, List<decimal> lows, List<decimal> closes, IchimokuSettings settings);
    
    // Support/Resistance
    SupportResistance CalculateSupportResistance(List<decimal> highs, List<decimal> lows, int lookbackPeriod);
    
    // Batch calculation for performance
    Task<IndicatorValues> CalculateAllIndicatorsAsync(Guid symbolId, string timeframe, List<Candle> candles, IndicatorSettings settings);
    
    // Real-time updates
    Task<IndicatorValues> UpdateIndicatorsAsync(IndicatorValues previous, Candle newCandle, IndicatorSettings settings);
}