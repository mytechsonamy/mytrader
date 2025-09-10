using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.Models.Indicators;
using MyTrader.Infrastructure.Data;
using System.Collections.Concurrent;

namespace MyTrader.Services.Trading;

public class IndicatorService : IIndicatorService
{
    private readonly TradingDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IndicatorService> _logger;
    private readonly ConcurrentDictionary<string, object> _calculationLocks = new();

    public IndicatorService(TradingDbContext context, IMemoryCache cache, ILogger<IndicatorService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }
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

    public decimal CalculateEMA(List<decimal> prices, int period)
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

    public decimal CalculateSMA(List<decimal> prices, int period)
    {
        if (prices.Count < period)
            return prices.Average();
            
        return prices.TakeLast(period).Average();
    }

    public StochasticOscillator CalculateStochastic(List<decimal> highs, List<decimal> lows, List<decimal> closes, StochasticSettings settings)
    {
        if (highs.Count < settings.KPeriod || lows.Count < settings.KPeriod || closes.Count < settings.KPeriod)
            throw new ArgumentException($"Not enough data points. Need at least {settings.KPeriod}");

        var recentHighs = highs.TakeLast(settings.KPeriod).ToList();
        var recentLows = lows.TakeLast(settings.KPeriod).ToList();
        var currentClose = closes.Last();

        var highestHigh = recentHighs.Max();
        var lowestLow = recentLows.Min();
        
        var kPercent = highestHigh == lowestLow ? 50m : (currentClose - lowestLow) / (highestHigh - lowestLow) * 100;
        
        // Calculate %D as SMA of %K (simplified)
        var dPercent = kPercent; // Should be SMA of recent %K values
        
        return new StochasticOscillator
        {
            K = Math.Round(kPercent, 2),
            D = Math.Round(dPercent, 2)
        };
    }

    public ATR CalculateATR(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period)
    {
        if (highs.Count < period + 1 || lows.Count < period + 1 || closes.Count < period + 1)
            throw new ArgumentException($"Not enough data points. Need at least {period + 1}");

        var trueRanges = new List<decimal>();
        
        for (int i = 1; i < highs.Count; i++)
        {
            var tr1 = highs[i] - lows[i];
            var tr2 = Math.Abs(highs[i] - closes[i - 1]);
            var tr3 = Math.Abs(lows[i] - closes[i - 1]);
            
            trueRanges.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
        }
        
        var atr = CalculateEMA(trueRanges, period);
        var currentPrice = closes.Last();
        var atrPercentage = currentPrice > 0 ? (atr / currentPrice) * 100 : 0;
        
        return new ATR
        {
            Value = Math.Round(atr, 4),
            Percentage = Math.Round(atrPercentage, 4)
        };
    }

    public Williams CalculateWilliamsR(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period)
    {
        if (highs.Count < period || lows.Count < period || closes.Count < period)
            throw new ArgumentException($"Not enough data points. Need at least {period}");

        var recentHighs = highs.TakeLast(period).ToList();
        var recentLows = lows.TakeLast(period).ToList();
        var currentClose = closes.Last();

        var highestHigh = recentHighs.Max();
        var lowestLow = recentLows.Min();
        
        var williamsR = highestHigh == lowestLow ? -50m : ((highestHigh - currentClose) / (highestHigh - lowestLow)) * -100;
        
        return new Williams
        {
            Value = Math.Round(williamsR, 2)
        };
    }

    public CCI CalculateCCI(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period)
    {
        if (highs.Count < period || lows.Count < period || closes.Count < period)
            throw new ArgumentException($"Not enough data points. Need at least {period}");

        var typicalPrices = new List<decimal>();
        for (int i = 0; i < highs.Count; i++)
        {
            typicalPrices.Add((highs[i] + lows[i] + closes[i]) / 3);
        }
        
        var recentTypicalPrices = typicalPrices.TakeLast(period).ToList();
        var smaTypical = recentTypicalPrices.Average();
        
        var meanDeviation = recentTypicalPrices.Sum(tp => Math.Abs(tp - smaTypical)) / period;
        var cci = meanDeviation != 0 ? (typicalPrices.Last() - smaTypical) / (0.015m * meanDeviation) : 0;
        
        return new CCI
        {
            Value = Math.Round(cci, 2)
        };
    }

    public MFI CalculateMFI(List<decimal> highs, List<decimal> lows, List<decimal> closes, List<decimal> volumes, int period)
    {
        if (highs.Count < period + 1 || volumes.Count < period + 1)
            throw new ArgumentException($"Not enough data points. Need at least {period + 1}");

        var rawMoneyFlows = new List<decimal>();
        var moneyFlowMultipliers = new List<decimal>();
        
        for (int i = 1; i < highs.Count; i++)
        {
            var typicalPrice = (highs[i] + lows[i] + closes[i]) / 3;
            var prevTypicalPrice = (highs[i-1] + lows[i-1] + closes[i-1]) / 3;
            
            var rawMoneyFlow = typicalPrice * volumes[i];
            rawMoneyFlows.Add(rawMoneyFlow);
            
            moneyFlowMultipliers.Add(typicalPrice > prevTypicalPrice ? 1 : typicalPrice < prevTypicalPrice ? -1 : 0);
        }
        
        var recentFlows = rawMoneyFlows.TakeLast(period).ToList();
        var recentMultipliers = moneyFlowMultipliers.TakeLast(period).ToList();
        
        var positiveFlow = recentFlows.Where((flow, idx) => recentMultipliers[idx] > 0).Sum();
        var negativeFlow = recentFlows.Where((flow, idx) => recentMultipliers[idx] < 0).Sum();
        
        var mfi = negativeFlow == 0 ? 100m : 100m - (100m / (1m + (positiveFlow / negativeFlow)));
        
        return new MFI
        {
            Value = Math.Round(mfi, 2)
        };
    }

    public decimal CalculateVWAP(List<Candle> candles)
    {
        if (!candles.Any())
            return 0;

        decimal totalPriceVolume = 0;
        decimal totalVolume = 0;
        
        foreach (var candle in candles)
        {
            var typicalPrice = (candle.High + candle.Low + candle.Close) / 3;
            totalPriceVolume += typicalPrice * candle.Volume;
            totalVolume += candle.Volume;
        }
        
        return totalVolume > 0 ? totalPriceVolume / totalVolume : 0;
    }

    public decimal CalculateOBV(List<decimal> closes, List<decimal> volumes)
    {
        if (closes.Count < 2 || volumes.Count < 2)
            return 0;

        decimal obv = 0;
        for (int i = 1; i < closes.Count; i++)
        {
            if (closes[i] > closes[i - 1])
                obv += volumes[i];
            else if (closes[i] < closes[i - 1])
                obv -= volumes[i];
        }
        
        return obv;
    }

    public ADX CalculateADX(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period)
    {
        if (highs.Count < period + 1)
            throw new ArgumentException($"Not enough data points. Need at least {period + 1}");

        var plusDMs = new List<decimal>();
        var minusDMs = new List<decimal>();
        var trueRanges = new List<decimal>();
        
        for (int i = 1; i < highs.Count; i++)
        {
            var upMove = highs[i] - highs[i - 1];
            var downMove = lows[i - 1] - lows[i];
            
            plusDMs.Add(upMove > downMove && upMove > 0 ? upMove : 0);
            minusDMs.Add(downMove > upMove && downMove > 0 ? downMove : 0);
            
            var tr1 = highs[i] - lows[i];
            var tr2 = Math.Abs(highs[i] - closes[i - 1]);
            var tr3 = Math.Abs(lows[i] - closes[i - 1]);
            trueRanges.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
        }
        
        var avgTR = CalculateEMA(trueRanges, period);
        var avgPlusDM = CalculateEMA(plusDMs, period);
        var avgMinusDM = CalculateEMA(minusDMs, period);
        
        var plusDI = avgTR != 0 ? (avgPlusDM / avgTR) * 100 : 0;
        var minusDI = avgTR != 0 ? (avgMinusDM / avgTR) * 100 : 0;
        
        var dx = (plusDI + minusDI) != 0 ? Math.Abs(plusDI - minusDI) / (plusDI + minusDI) * 100 : 0;
        var adx = dx; // Simplified - should be smoothed
        
        var direction = plusDI > minusDI ? TrendDirection.Bullish : 
                       minusDI > plusDI ? TrendDirection.Bearish : TrendDirection.Sideways;
        
        return new ADX
        {
            Value = Math.Round(adx, 2),
            PlusDI = Math.Round(plusDI, 2),
            MinusDI = Math.Round(minusDI, 2),
            Direction = direction,
            TrendStrength = Math.Round(adx, 2)
        };
    }

    public Ichimoku CalculateIchimoku(List<decimal> highs, List<decimal> lows, List<decimal> closes, IchimokuSettings settings)
    {
        if (highs.Count < settings.SenkouBPeriod)
            throw new ArgumentException($"Not enough data points. Need at least {settings.SenkouBPeriod}");

        var tenkanHighs = highs.TakeLast(settings.TenkanPeriod).ToList();
        var tenkanLows = lows.TakeLast(settings.TenkanPeriod).ToList();
        var tenkanSen = (tenkanHighs.Max() + tenkanLows.Min()) / 2;
        
        var kijunHighs = highs.TakeLast(settings.KijunPeriod).ToList();
        var kijunLows = lows.TakeLast(settings.KijunPeriod).ToList();
        var kijunSen = (kijunHighs.Max() + kijunLows.Min()) / 2;
        
        var senkouSpanA = (tenkanSen + kijunSen) / 2;
        
        var senkouBHighs = highs.TakeLast(settings.SenkouBPeriod).ToList();
        var senkouBLows = lows.TakeLast(settings.SenkouBPeriod).ToList();
        var senkouSpanB = (senkouBHighs.Max() + senkouBLows.Min()) / 2;
        
        var currentPrice = closes.Last();
        var chikouSpan = currentPrice; // Lagging span is current close displaced back
        
        var cloudStatus = currentPrice > Math.Max(senkouSpanA, senkouSpanB) ? CloudStatus.Bullish :
                         currentPrice < Math.Min(senkouSpanA, senkouSpanB) ? CloudStatus.Bearish :
                         CloudStatus.InCloud;
        
        return new Ichimoku
        {
            TenkanSen = Math.Round(tenkanSen, 4),
            KijunSen = Math.Round(kijunSen, 4),
            SenkouSpanA = Math.Round(senkouSpanA, 4),
            SenkouSpanB = Math.Round(senkouSpanB, 4),
            ChikouSpan = Math.Round(chikouSpan, 4),
            CloudStatus = cloudStatus
        };
    }

    public SupportResistance CalculateSupportResistance(List<decimal> highs, List<decimal> lows, int lookbackPeriod)
    {
        if (highs.Count < lookbackPeriod || lows.Count < lookbackPeriod)
            throw new ArgumentException($"Not enough data points. Need at least {lookbackPeriod}");

        var recentHighs = highs.TakeLast(lookbackPeriod).ToList();
        var recentLows = lows.TakeLast(lookbackPeriod).ToList();
        var currentPrice = (highs.Last() + lows.Last()) / 2;
        
        // Find local maxima and minima
        var resistanceLevels = new List<decimal>();
        var supportLevels = new List<decimal>();
        
        for (int i = 2; i < recentHighs.Count - 2; i++)
        {
            if (recentHighs[i] > recentHighs[i-1] && recentHighs[i] > recentHighs[i+1] &&
                recentHighs[i] > recentHighs[i-2] && recentHighs[i] > recentHighs[i+2])
            {
                resistanceLevels.Add(recentHighs[i]);
            }
            
            if (recentLows[i] < recentLows[i-1] && recentLows[i] < recentLows[i+1] &&
                recentLows[i] < recentLows[i-2] && recentLows[i] < recentLows[i+2])
            {
                supportLevels.Add(recentLows[i]);
            }
        }
        
        var currentSupport = supportLevels.Where(s => s < currentPrice).OrderByDescending(s => s).FirstOrDefault();
        var currentResistance = resistanceLevels.Where(r => r > currentPrice).OrderBy(r => r).FirstOrDefault();
        
        return new SupportResistance
        {
            SupportLevels = supportLevels.OrderByDescending(s => s).Take(3).ToList(),
            ResistanceLevels = resistanceLevels.OrderBy(r => r).Take(3).ToList(),
            CurrentSupport = currentSupport,
            CurrentResistance = currentResistance,
            DistanceToSupport = currentSupport > 0 ? Math.Abs(currentPrice - currentSupport) : 0,
            DistanceToResistance = currentResistance > 0 ? Math.Abs(currentResistance - currentPrice) : 0
        };
    }

    public async Task<IndicatorValues> CalculateAllIndicatorsAsync(Guid symbolId, string timeframe, List<Candle> candles, IndicatorSettings settings)
    {
        var cacheKey = $"indicators_{symbolId}_{timeframe}_{candles.LastOrDefault()?.Timestamp:yyyyMMddHHmmss}";
        
        if (settings.EnableCaching && _cache.TryGetValue(cacheKey, out IndicatorValues? cachedValues))
        {
            return cachedValues!;
        }
        
        var lockKey = $"calc_{symbolId}_{timeframe}";
        var lockObject = _calculationLocks.GetOrAdd(lockKey, _ => new object());
        
        return await Task.Run(() =>
        {
            lock (lockObject)
            {
                if (settings.EnableCaching && _cache.TryGetValue(cacheKey, out IndicatorValues? doubleCheckedCachedValues))
                {
                    return doubleCheckedCachedValues!;
                }
                
                var result = CalculateAllIndicators(symbolId, timeframe, candles, settings);
                
                if (settings.EnableCaching)
                {
                    var expiry = TimeSpan.FromMinutes(settings.CacheExpiryMinutes);
                    _cache.Set(cacheKey, result, expiry);
                }
                
                return result;
            }
        });
    }
    
    public async Task<IndicatorValues> UpdateIndicatorsAsync(IndicatorValues previous, Candle newCandle, IndicatorSettings settings)
    {
        // For real-time updates, we'd need to implement incremental calculation
        // For now, return a basic update
        return await Task.FromResult(new IndicatorValues
        {
            SymbolId = previous.SymbolId,
            Timeframe = previous.Timeframe,
            Timestamp = newCandle.Timestamp,
            Close = newCandle.Close,
            Volume = newCandle.Volume
        });
    }

    private IndicatorValues CalculateAllIndicators(Guid symbolId, string timeframe, List<Candle> candles, IndicatorSettings settings)
    {
        if (!candles.Any())
            throw new ArgumentException("No candle data provided");

        var highs = candles.Select(c => c.High).ToList();
        var lows = candles.Select(c => c.Low).ToList();
        var closes = candles.Select(c => c.Close).ToList();
        var volumes = candles.Select(c => c.Volume).ToList();
        var lastCandle = candles.Last();
        
        var result = new IndicatorValues
        {
            SymbolId = symbolId,
            Timeframe = timeframe,
            Timestamp = lastCandle.Timestamp,
            Open = lastCandle.Open,
            High = lastCandle.High,
            Low = lastCandle.Low,
            Close = lastCandle.Close,
            Volume = lastCandle.Volume
        };
        
        try
        {
            // Basic indicators
            if (closes.Count >= settings.RSI.Period + 1)
            {
                var rsi = CalculateRSI(closes, settings.RSI);
                result.Rsi = rsi.Value;
            }
            
            if (closes.Count >= settings.BollingerBands.Period)
            {
                var bb = CalculateBollingerBands(closes, settings.BollingerBands);
                result.BbUpper = bb.Upper;
                result.BbMiddle = bb.Middle;
                result.BbLower = bb.Lower;
                result.BbPosition = CalculateBBPosition(result.Close, bb);
            }
            
            if (closes.Count >= settings.MACD.SlowPeriod)
            {
                var macd = CalculateMACD(closes, settings.MACD);
                result.Macd = macd.Value;
                result.MacdSignal = macd.Signal;
                result.MacdHistogram = macd.Histogram;
            }
            
            // EMAs
            foreach (var period in settings.EMAPeriods)
            {
                if (closes.Count >= period)
                {
                    var ema = CalculateEMA(closes, period);
                    switch (period)
                    {
                        case 9: result.Ema9 = ema; break;
                        case 21: result.Ema21 = ema; break;
                        case 50: result.Ema50 = ema; break;
                        case 100: result.Ema100 = ema; break;
                        case 200: result.Ema200 = ema; break;
                    }
                }
            }
            
            // SMAs
            foreach (var period in settings.SMAPeriods)
            {
                if (closes.Count >= period)
                {
                    var sma = CalculateSMA(closes, period);
                    switch (period)
                    {
                        case 20: result.Sma20 = sma; break;
                        case 50: result.Sma50 = sma; break;
                        case 100: result.Sma100 = sma; break;
                        case 200: result.Sma200 = sma; break;
                    }
                }
            }
            
            // Volume indicators
            if (settings.CalculateVWAP && candles.Count > 1)
            {
                result.Vwap = CalculateVWAP(candles);
            }
            
            // ATR
            if (highs.Count >= settings.ATRPeriod + 1)
            {
                var atr = CalculateATR(highs, lows, closes, settings.ATRPeriod);
                result.Atr = atr.Value;
                result.AtrPercentage = atr.Percentage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating some indicators for {SymbolId} {Timeframe}", symbolId, timeframe);
        }
        
        return result;
    }
    
    private decimal CalculateBBPosition(decimal price, BollingerBands bb)
    {
        if (bb.Upper == bb.Lower)
            return 0;
            
        return (price - bb.Lower) / (bb.Upper - bb.Lower);
    }
}