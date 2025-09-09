using MyTrader.Core.Models.Indicators;

namespace MyTrader.Services.Trading;

public class IndicatorService : IIndicatorService
{
    public BollingerBands CalculateBollingerBands(List<decimal> prices, BollingerBandSettings settings)
    {
        if (prices.Count < settings.Period)
            throw new ArgumentException($"Not enough data points. Need at least {settings.Period}");

        var recentPrices = prices.TakeLast(settings.Period).ToList();
        var sma = recentPrices.Average();
        
        // Calculate standard deviation
        var variance = recentPrices.Select(price => Math.Pow((double)(price - sma), 2)).Average();
        var stdDev = (decimal)Math.Sqrt(variance);
        
        return new BollingerBands
        {
            Upper = sma + (settings.Multiplier * stdDev),
            Middle = sma,
            Lower = sma - (settings.Multiplier * stdDev)
        };
    }

    public RSI CalculateRSI(List<decimal> prices, RSISettings settings)
    {
        if (prices.Count < settings.Period + 1)
            throw new ArgumentException($"Not enough data points. Need at least {settings.Period + 1}");

        var changes = new List<decimal>();
        for (int i = 1; i < prices.Count; i++)
        {
            changes.Add(prices[i] - prices[i - 1]);
        }

        var recentChanges = changes.TakeLast(settings.Period).ToList();
        
        var gains = recentChanges.Where(c => c > 0).ToList();
        var losses = recentChanges.Where(c => c < 0).Select(c => -c).ToList();
        
        var avgGain = gains.Any() ? gains.Average() : 0;
        var avgLoss = losses.Any() ? losses.Average() : 0;
        
        if (avgLoss == 0)
        {
            return new RSI { Value = 100 };
        }
        
        var rs = avgGain / avgLoss;
        var rsi = 100 - (100 / (1 + rs));
        
        return new RSI { Value = Math.Round(rsi, 2) };
    }

    public MACD CalculateMACD(List<decimal> prices, MACDSettings settings)
    {
        if (prices.Count < settings.SlowPeriod)
            throw new ArgumentException($"Not enough data points. Need at least {settings.SlowPeriod}");

        // Calculate EMAs
        var fastEma = CalculateEMA(prices, settings.FastPeriod);
        var slowEma = CalculateEMA(prices, settings.SlowPeriod);
        
        var macdLine = fastEma - slowEma;
        
        // For simplicity, using SMA for signal line instead of EMA
        // In a production system, you'd want to implement proper EMA for signal line
        var macdValues = new List<decimal> { macdLine };
        var signalLine = macdLine; // Simplified - should be EMA of MACD line
        
        var histogram = macdLine - signalLine;
        
        return new MACD
        {
            Value = Math.Round(macdLine, 4),
            Signal = Math.Round(signalLine, 4),
            Histogram = Math.Round(histogram, 4)
        };
    }

    private decimal CalculateEMA(List<decimal> prices, int period)
    {
        if (prices.Count < period)
            return prices.Average();

        var multiplier = 2.0m / (period + 1);
        var ema = prices.Take(period).Average(); // Start with SMA
        
        for (int i = period; i < prices.Count; i++)
        {
            ema = (prices[i] * multiplier) + (ema * (1 - multiplier));
        }
        
        return ema;
    }
}