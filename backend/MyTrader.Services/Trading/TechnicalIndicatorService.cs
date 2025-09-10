using Microsoft.Extensions.Logging;
using MyTrader.Services.Market;
using System.Collections.Concurrent;

namespace MyTrader.Services.Trading;

public interface ITechnicalIndicatorService
{
    Task<TechnicalIndicatorValues> CalculateIndicatorsAsync(string symbol, decimal price);
    Task<TechnicalIndicatorValues> GetLatestIndicatorsAsync(string symbol);
}

public class TechnicalIndicatorValues
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    
    // RSI (Relative Strength Index)
    public decimal? RSI { get; set; }
    
    // MACD
    public decimal? MACD { get; set; }
    public decimal? MACDSignal { get; set; }
    public decimal? MACDHistogram { get; set; }
    
    // Bollinger Bands
    public decimal? BBUpper { get; set; }
    public decimal? BBMiddle { get; set; }
    public decimal? BBLower { get; set; }
    
    // Moving Averages
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? EMA12 { get; set; }
    public decimal? EMA26 { get; set; }
    
    // Trading Signals
    public string? Signal { get; set; }
    public string? Trend { get; set; }
}

public class TechnicalIndicatorService : ITechnicalIndicatorService
{
    private readonly ILogger<TechnicalIndicatorService> _logger;
    
    // Price history for each symbol (last 200 prices for calculation)
    private readonly ConcurrentDictionary<string, Queue<PriceData>> _priceHistory = new();
    private readonly ConcurrentDictionary<string, TechnicalIndicatorValues> _latestIndicators = new();
    
    private const int MaxHistorySize = 200;

    public TechnicalIndicatorService(ILogger<TechnicalIndicatorService> logger)
    {
        _logger = logger;
    }

    public async Task<TechnicalIndicatorValues> CalculateIndicatorsAsync(string symbol, decimal price)
    {
        try
        {
            // Add price to history
            var priceData = new PriceData { Price = price, Timestamp = DateTime.UtcNow };
            AddPriceToHistory(symbol, priceData);
            
            var history = _priceHistory[symbol].ToArray();
            var indicators = new TechnicalIndicatorValues
            {
                Symbol = symbol,
                Price = price,
                Timestamp = DateTime.UtcNow
            };

            if (history.Length >= 14) // Minimum for RSI
            {
                indicators.RSI = CalculateRSI(history, 14);
            }

            if (history.Length >= 26) // Minimum for MACD
            {
                var (macd, signal, histogram) = CalculateMACD(history);
                indicators.MACD = macd;
                indicators.MACDSignal = signal;
                indicators.MACDHistogram = histogram;
            }

            if (history.Length >= 20) // Minimum for Bollinger Bands and SMA20
            {
                indicators.SMA20 = CalculateSMA(history, 20);
                var (upper, middle, lower) = CalculateBollingerBands(history, 20, 2);
                indicators.BBUpper = upper;
                indicators.BBMiddle = middle;
                indicators.BBLower = lower;
            }

            if (history.Length >= 50)
            {
                indicators.SMA50 = CalculateSMA(history, 50);
            }

            if (history.Length >= 12)
            {
                indicators.EMA12 = CalculateEMA(history, 12);
            }

            if (history.Length >= 26)
            {
                indicators.EMA26 = CalculateEMA(history, 26);
            }

            // Generate trading signals
            indicators.Signal = GenerateSignal(indicators);
            indicators.Trend = DetermineTrend(indicators);

            // Cache latest indicators
            _latestIndicators[symbol] = indicators;

            _logger.LogDebug("Calculated indicators for {Symbol}: RSI={RSI}, MACD={MACD}, Signal={Signal}", 
                symbol, indicators.RSI, indicators.MACD, indicators.Signal);

            return indicators;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating indicators for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<TechnicalIndicatorValues> GetLatestIndicatorsAsync(string symbol)
    {
        _latestIndicators.TryGetValue(symbol, out var indicators);
        return indicators ?? new TechnicalIndicatorValues { Symbol = symbol };
    }

    private void AddPriceToHistory(string symbol, PriceData priceData)
    {
        if (!_priceHistory.ContainsKey(symbol))
        {
            _priceHistory[symbol] = new Queue<PriceData>();
        }

        var queue = _priceHistory[symbol];
        queue.Enqueue(priceData);

        // Keep only the last MaxHistorySize prices
        while (queue.Count > MaxHistorySize)
        {
            queue.Dequeue();
        }
    }

    private decimal CalculateRSI(PriceData[] prices, int period)
    {
        if (prices.Length < period + 1) return 50m; // Neutral RSI

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < prices.Length; i++)
        {
            var change = prices[i].Price - prices[i - 1].Price;
            if (change > 0)
            {
                gains.Add(change);
                losses.Add(0);
            }
            else
            {
                gains.Add(0);
                losses.Add(Math.Abs(change));
            }
        }

        if (gains.Count < period) return 50m;

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0) return 100m;

        var rs = avgGain / avgLoss;
        var rsi = 100 - (100 / (1 + rs));

        return Math.Round(rsi, 2);
    }

    private (decimal macd, decimal signal, decimal histogram) CalculateMACD(PriceData[] prices)
    {
        var ema12 = CalculateEMA(prices, 12);
        var ema26 = CalculateEMA(prices, 26);
        var macd = ema12 - ema26;

        // For signal line, we'd need MACD history. Simplified version:
        var signal = macd * 0.9m; // Approximate signal line
        var histogram = macd - signal;

        return (Math.Round(macd, 4), Math.Round(signal, 4), Math.Round(histogram, 4));
    }

    private decimal CalculateEMA(PriceData[] prices, int period)
    {
        if (prices.Length < period) return prices.Last().Price;

        var multiplier = 2m / (period + 1);
        var ema = prices.Take(period).Average(p => p.Price);

        for (int i = period; i < prices.Length; i++)
        {
            ema = (prices[i].Price * multiplier) + (ema * (1 - multiplier));
        }

        return Math.Round(ema, 2);
    }

    private decimal CalculateSMA(PriceData[] prices, int period)
    {
        if (prices.Length < period) return prices.Average(p => p.Price);

        var sma = prices.TakeLast(period).Average(p => p.Price);
        return Math.Round(sma, 2);
    }

    private (decimal upper, decimal middle, decimal lower) CalculateBollingerBands(PriceData[] prices, int period, decimal stdDevMultiplier)
    {
        var sma = CalculateSMA(prices, period);
        var recentPrices = prices.TakeLast(period).Select(p => p.Price).ToArray();
        
        var variance = recentPrices.Sum(price => Math.Pow((double)(price - sma), 2)) / period;
        var stdDev = (decimal)Math.Sqrt(variance);

        var upper = sma + (stdDev * stdDevMultiplier);
        var lower = sma - (stdDev * stdDevMultiplier);

        return (Math.Round(upper, 2), Math.Round(sma, 2), Math.Round(lower, 2));
    }

    private string GenerateSignal(TechnicalIndicatorValues indicators)
    {
        var signals = new List<string>();

        // RSI signals
        if (indicators.RSI.HasValue)
        {
            if (indicators.RSI < 30) signals.Add("RSI_OVERSOLD");
            else if (indicators.RSI > 70) signals.Add("RSI_OVERBOUGHT");
        }

        // MACD signals
        if (indicators.MACD.HasValue && indicators.MACDSignal.HasValue)
        {
            if (indicators.MACD > indicators.MACDSignal) signals.Add("MACD_BULLISH");
            else signals.Add("MACD_BEARISH");
        }

        // Bollinger Bands signals
        if (indicators.BBUpper.HasValue && indicators.BBLower.HasValue)
        {
            if (indicators.Price >= indicators.BBUpper) signals.Add("BB_OVERBOUGHT");
            else if (indicators.Price <= indicators.BBLower) signals.Add("BB_OVERSOLD");
        }

        // Moving Average signals
        if (indicators.SMA20.HasValue && indicators.SMA50.HasValue)
        {
            if (indicators.SMA20 > indicators.SMA50) signals.Add("MA_BULLISH");
            else signals.Add("MA_BEARISH");
        }

        // Combine signals for overall recommendation
        var bullishSignals = signals.Count(s => s.Contains("BULLISH") || s.Contains("OVERSOLD"));
        var bearishSignals = signals.Count(s => s.Contains("BEARISH") || s.Contains("OVERBOUGHT"));

        if (bullishSignals > bearishSignals) return "BUY";
        else if (bearishSignals > bullishSignals) return "SELL";
        else return "HOLD";
    }

    private string DetermineTrend(TechnicalIndicatorValues indicators)
    {
        if (indicators.SMA20.HasValue && indicators.SMA50.HasValue)
        {
            if (indicators.SMA20 > indicators.SMA50 && indicators.Price > indicators.SMA20)
                return "UPTREND";
            else if (indicators.SMA20 < indicators.SMA50 && indicators.Price < indicators.SMA20)
                return "DOWNTREND";
        }

        return "SIDEWAYS";
    }
}

public class PriceData
{
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}