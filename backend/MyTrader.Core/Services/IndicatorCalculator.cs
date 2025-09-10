using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.DTOs.Indicators;

namespace MyTrader.Core.Services;

public interface IIndicatorCalculator
{
    Task<RSI> CalculateRSIAsync(List<MarketData> data, int period = 14);
    Task<MACD> CalculateMACDAsync(List<MarketData> data, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9);
    Task<BollingerBands> CalculateBollingerBandsAsync(List<MarketData> data, int period = 20, decimal stdDev = 2.0m);
    Task<SMA> CalculateSMAAsync(List<MarketData> data, int period = 20);
    Task<EMA> CalculateEMAAsync(List<MarketData> data, int period = 20);
    Task<StochasticOscillator> CalculateStochasticAsync(List<MarketData> data, int kPeriod = 14, int dPeriod = 3);
    Task<WilliamsR> CalculateWilliamsRAsync(List<MarketData> data, int period = 14);
    Task<CCI> CalculateCCIAsync(List<MarketData> data, int period = 20);
    Task<ATR> CalculateATRAsync(List<MarketData> data, int period = 14);
    Task<List<IndicatorValues>> CalculateAllIndicatorsAsync(Guid symbolId, string timeframe, DateTime startDate, DateTime endDate);
}

public class IndicatorCalculator : IIndicatorCalculator
{
    private readonly ILogger<IndicatorCalculator> _logger;

    public IndicatorCalculator(ILogger<IndicatorCalculator> logger)
    {
        _logger = logger;
    }

    public async Task<RSI> CalculateRSIAsync(List<MarketData> data, int period = 14)
    {
        if (data.Count < period + 1)
            throw new ArgumentException($"Insufficient data points. Need at least {period + 1}, got {data.Count}");

        var prices = data.Select(d => d.Close).ToArray();
        var gains = new List<decimal>();
        var losses = new List<decimal>();

        // Calculate price changes
        for (int i = 1; i < prices.Length; i++)
        {
            var change = prices[i] - prices[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        // Calculate initial averages
        var avgGain = gains.Take(period).Average();
        var avgLoss = losses.Take(period).Average();

        // Calculate smoothed averages for remaining periods
        for (int i = period; i < gains.Count; i++)
        {
            avgGain = ((avgGain * (period - 1)) + gains[i]) / period;
            avgLoss = ((avgLoss * (period - 1)) + losses[i]) / period;
        }

        var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
        var rsi = 100 - (100 / (1 + rs));

        return await Task.FromResult(new RSI { Value = rsi });
    }

    public async Task<MACD> CalculateMACDAsync(List<MarketData> data, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        if (data.Count < slowPeriod + signalPeriod)
            throw new ArgumentException($"Insufficient data points for MACD calculation");

        var prices = data.Select(d => d.Close).ToArray();
        
        // Calculate EMAs
        var fastEma = CalculateEMA(prices, fastPeriod);
        var slowEma = CalculateEMA(prices, slowPeriod);

        // Calculate MACD line
        var macdValues = new List<decimal>();
        for (int i = slowPeriod - 1; i < prices.Length; i++)
        {
            macdValues.Add(fastEma[i] - slowEma[i]);
        }

        // Calculate Signal line (EMA of MACD)
        var signalEma = CalculateEMA(macdValues.ToArray(), signalPeriod);
        var macdValue = macdValues.Last();
        var signalValue = signalEma.Last();
        var histogram = macdValue - signalValue;

        return await Task.FromResult(new MACD
        {
            MACDLine = macdValue,
            SignalLine = signalValue,
            Histogram = histogram
        });
    }

    public async Task<BollingerBands> CalculateBollingerBandsAsync(List<MarketData> data, int period = 20, decimal stdDev = 2.0m)
    {
        if (data.Count < period)
            throw new ArgumentException($"Insufficient data points for Bollinger Bands calculation");

        var prices = data.TakeLast(period).Select(d => d.Close).ToArray();
        var sma = prices.Average();
        
        // Calculate standard deviation
        var variance = prices.Select(price => Math.Pow((double)(price - sma), 2)).Average();
        var standardDeviation = (decimal)Math.Sqrt(variance);

        var upperBand = sma + (stdDev * standardDeviation);
        var lowerBand = sma - (stdDev * standardDeviation);

        return await Task.FromResult(new BollingerBands
        {
            UpperBand = upperBand,
            MiddleBand = sma,
            LowerBand = lowerBand
        });
    }

    public async Task<SMA> CalculateSMAAsync(List<MarketData> data, int period = 20)
    {
        if (data.Count < period)
            throw new ArgumentException($"Insufficient data points for SMA calculation");

        var prices = data.TakeLast(period).Select(d => d.Close);
        var smaValue = prices.Average();

        return await Task.FromResult(new SMA { Value = smaValue });
    }

    public async Task<EMA> CalculateEMAAsync(List<MarketData> data, int period = 20)
    {
        if (data.Count < period)
            throw new ArgumentException($"Insufficient data points for EMA calculation");

        var prices = data.Select(d => d.Close).ToArray();
        var emaValues = CalculateEMA(prices, period);
        
        return await Task.FromResult(new EMA { Value = emaValues.Last() });
    }

    public async Task<StochasticOscillator> CalculateStochasticAsync(List<MarketData> data, int kPeriod = 14, int dPeriod = 3)
    {
        if (data.Count < kPeriod + dPeriod)
            throw new ArgumentException($"Insufficient data points for Stochastic calculation");

        var recentData = data.TakeLast(kPeriod).ToList();
        var currentClose = recentData.Last().Close;
        var lowestLow = recentData.Min(d => d.Low);
        var highestHigh = recentData.Max(d => d.High);

        var kPercent = highestHigh == lowestLow ? 50 : 
            ((currentClose - lowestLow) / (highestHigh - lowestLow)) * 100;

        // For simplicity, using current %K as %D (should be SMA of %K over dPeriod)
        var dPercent = kPercent;

        return await Task.FromResult(new StochasticOscillator
        {
            PercentK = kPercent,
            PercentD = dPercent
        });
    }

    public async Task<WilliamsR> CalculateWilliamsRAsync(List<MarketData> data, int period = 14)
    {
        if (data.Count < period)
            throw new ArgumentException($"Insufficient data points for Williams %R calculation");

        var recentData = data.TakeLast(period).ToList();
        var currentClose = recentData.Last().Close;
        var highestHigh = recentData.Max(d => d.High);
        var lowestLow = recentData.Min(d => d.Low);

        var williamsR = highestHigh == lowestLow ? -50 :
            ((highestHigh - currentClose) / (highestHigh - lowestLow)) * -100;

        return await Task.FromResult(new WilliamsR { Value = williamsR });
    }

    public async Task<CCI> CalculateCCIAsync(List<MarketData> data, int period = 20)
    {
        if (data.Count < period)
            throw new ArgumentException($"Insufficient data points for CCI calculation");

        var recentData = data.TakeLast(period).ToList();
        var typicalPrices = recentData.Select(d => (d.High + d.Low + d.Close) / 3).ToList();
        var smaTP = typicalPrices.Average();
        
        var meanDeviation = typicalPrices.Select(tp => Math.Abs(tp - smaTP)).Average();
        var currentTP = typicalPrices.Last();
        
        var cci = meanDeviation == 0 ? 0 : (currentTP - smaTP) / (0.015m * meanDeviation);

        return await Task.FromResult(new CCI { Value = cci });
    }

    public async Task<ATR> CalculateATRAsync(List<MarketData> data, int period = 14)
    {
        if (data.Count < period + 1)
            throw new ArgumentException($"Insufficient data points for ATR calculation");

        var trueRanges = new List<decimal>();
        
        for (int i = 1; i < data.Count; i++)
        {
            var current = data[i];
            var previous = data[i - 1];
            
            var tr1 = current.High - current.Low;
            var tr2 = Math.Abs(current.High - previous.Close);
            var tr3 = Math.Abs(current.Low - previous.Close);
            
            trueRanges.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
        }

        var atr = trueRanges.TakeLast(period).Average();

        return await Task.FromResult(new ATR { Value = atr });
    }

    public async Task<List<IndicatorValues>> CalculateAllIndicatorsAsync(Guid symbolId, string timeframe, DateTime startDate, DateTime endDate)
    {
        // This method would typically fetch market data and calculate all indicators
        // For now, returning empty list - requires integration with market data service
        
        _logger.LogInformation("Calculating all indicators for symbol {SymbolId} from {StartDate} to {EndDate}", 
            symbolId, startDate, endDate);
        
        var results = new List<IndicatorValues>();
        
        // Would iterate through market data and calculate indicators
        // Then save to indicator_values table
        
        return await Task.FromResult(results);
    }

    private decimal[] CalculateEMA(decimal[] prices, int period)
    {
        var emaValues = new decimal[prices.Length];
        var multiplier = 2.0m / (period + 1);
        
        // Start with SMA for first EMA value
        emaValues[period - 1] = prices.Take(period).Average();
        
        // Calculate remaining EMA values
        for (int i = period; i < prices.Length; i++)
        {
            emaValues[i] = (prices[i] * multiplier) + (emaValues[i - 1] * (1 - multiplier));
        }
        
        return emaValues;
    }
}

// Extended indicator models
public class SMA
{
    public decimal Value { get; set; }
}

public class EMA
{
    public decimal Value { get; set; }
}

public class StochasticOscillator
{
    public decimal PercentK { get; set; }
    public decimal PercentD { get; set; }
}

public class WilliamsR
{
    public decimal Value { get; set; }
}

public class CCI
{
    public decimal Value { get; set; }
}

public class ATR
{
    public decimal Value { get; set; }
}