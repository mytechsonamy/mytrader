using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.Models.Indicators;
using MyTrader.Infrastructure.Data;
using MyTrader.Services.Trading;
using System.Collections.Concurrent;

namespace MyTrader.Services.Signals;

public class SignalGenerationEngine : ISignalGenerationEngine
{
    private readonly TradingDbContext _context;
    private readonly IIndicatorService _indicatorService;
    private readonly ILogger<SignalGenerationEngine> _logger;
    private readonly ConcurrentDictionary<string, List<Func<TradingSignal, Task>>> _subscribers = new();
    
    public SignalGenerationEngine(
        TradingDbContext context,
        IIndicatorService indicatorService,
        ILogger<SignalGenerationEngine> logger)
    {
        _context = context;
        _indicatorService = indicatorService;
        _logger = logger;
    }

    public async Task<List<TradingSignal>> GenerateSignalsAsync(Guid symbolId, string timeframe, List<Candle> candles, SignalGenerationSettings settings)
    {
        try
        {
            if (!candles.Any())
                return new List<TradingSignal>();

            var indicators = await _indicatorService.CalculateAllIndicatorsAsync(symbolId, timeframe, candles, settings.Indicators);
            var signals = new List<TradingSignal>();
            
            var tasks = new List<Task<TradingSignal?>>();

            // Generate signals from different sources in parallel if enabled
            if (settings.EnableParallelProcessing)
            {
                if (settings.EnableRSISignals)
                    tasks.Add(GenerateRSISignalAsync(symbolId, timeframe, indicators, settings));
                
                if (settings.EnableMACDSignals)
                    tasks.Add(GenerateMACDSignalAsync(symbolId, timeframe, indicators, settings));
                
                if (settings.EnableBollingerSignals)
                    tasks.Add(GenerateBollingerSignalAsync(symbolId, timeframe, indicators, settings));
                
                if (settings.EnableStochasticSignals && indicators.StochK.HasValue)
                    tasks.Add(GenerateStochasticSignalAsync(symbolId, timeframe, indicators, settings));
                    
                if (settings.EnableSupportResistanceSignals)
                    tasks.Add(GenerateSupportResistanceSignalAsync(symbolId, timeframe, candles, indicators, settings));
                
                if (settings.EnableVolumeSignals)
                    tasks.Add(GenerateVolumeSignalAsync(symbolId, timeframe, indicators, settings));
                
                if (settings.EnablePriceActionSignals)
                    tasks.Add(GeneratePriceActionSignalAsync(symbolId, timeframe, candles, indicators, settings));

                var results = await Task.WhenAll(tasks);
                signals.AddRange(results.Where(s => s != null).Cast<TradingSignal>());
            }
            else
            {
                // Generate signals sequentially
                if (settings.EnableRSISignals)
                {
                    var signal = await GenerateRSISignalAsync(symbolId, timeframe, indicators, settings);
                    if (signal != null) signals.Add(signal);
                }
                
                if (settings.EnableMACDSignals)
                {
                    var signal = await GenerateMACDSignalAsync(symbolId, timeframe, indicators, settings);
                    if (signal != null) signals.Add(signal);
                }
                
                // Add other sequential signal generation...
            }

            // Filter by minimum thresholds
            signals = signals.Where(s => 
                s.Confidence >= settings.MinConfidence && 
                s.Strength >= settings.MinStrength).ToList();

            // Set expiry times
            foreach (var signal in signals)
            {
                signal.ExpiresAt = signal.GeneratedAt.Add(settings.SignalExpiryTime);
            }

            // Limit number of signals
            if (signals.Count > settings.MaxSignalsPerSymbol)
            {
                signals = signals.OrderByDescending(s => s.Confidence)
                                .ThenByDescending(s => s.Strength)
                                .Take(settings.MaxSignalsPerSymbol)
                                .ToList();
            }

            _logger.LogInformation("Generated {Count} signals for {SymbolId} {Timeframe}", 
                signals.Count, symbolId, timeframe);

            return signals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signals for {SymbolId} {Timeframe}", symbolId, timeframe);
            return new List<TradingSignal>();
        }
    }

    public async Task<List<TradingSignal>> GenerateRealTimeSignalsAsync(Guid symbolId, string timeframe, Candle newCandle, IndicatorValues currentIndicators, SignalGenerationSettings settings)
    {
        var signals = new List<TradingSignal>();
        
        // Generate quick real-time signals based on current indicators
        if (settings.EnableRSISignals && currentIndicators.Rsi.HasValue)
        {
            var rsiSignal = await GenerateRSISignalAsync(symbolId, timeframe, currentIndicators, settings);
            if (rsiSignal != null) signals.Add(rsiSignal);
        }
        
        if (settings.EnableMACDSignals && currentIndicators.Macd.HasValue)
        {
            var macdSignal = await GenerateMACDSignalAsync(symbolId, timeframe, currentIndicators, settings);
            if (macdSignal != null) signals.Add(macdSignal);
        }
        
        // Notify subscribers
        var subscriptionKey = $"{symbolId}_{timeframe}";
        if (_subscribers.TryGetValue(subscriptionKey, out var subscribers))
        {
            var notificationTasks = signals.SelectMany(signal => 
                subscribers.Select(callback => callback(signal)));
            await Task.WhenAll(notificationTasks);
        }
        
        return signals;
    }

    public async Task<List<ScoredSignal>> ScoreSignalsAsync(List<TradingSignal> signals, SignalScoringSettings settings)
    {
        var scoredSignals = new List<ScoredSignal>();
        
        foreach (var signal in signals)
        {
            var scoreBreakdown = new Dictionary<string, decimal>();
            decimal totalScore = 0;
            
            // Confidence score
            var confidenceScore = (signal.Confidence / 100m) * settings.ConfidenceWeight;
            scoreBreakdown["Confidence"] = confidenceScore;
            totalScore += confidenceScore;
            
            // Strength score
            var strengthScore = (signal.Strength / 100m) * settings.StrengthWeight;
            scoreBreakdown["Strength"] = strengthScore;
            totalScore += strengthScore;
            
            // Reliability score
            var reliabilityScore = (signal.ReliabilityScore / 100m) * settings.ReliabilityWeight;
            scoreBreakdown["Reliability"] = reliabilityScore;
            totalScore += reliabilityScore;
            
            // Market condition score
            var marketScore = (signal.MarketConditionScore / 100m) * settings.MarketConditionWeight;
            scoreBreakdown["MarketCondition"] = marketScore;
            totalScore += marketScore;
            
            // Supporting indicators score
            var supportScore = Math.Min(signal.SupportingIndicators / 3m, 1m) * settings.SupportingIndicatorsWeight;
            scoreBreakdown["SupportingIndicators"] = supportScore;
            totalScore += supportScore;
            
            // Volume confirmation (simplified)
            var volumeScore = settings.VolumeConfirmationWeight; // Assume neutral
            scoreBreakdown["VolumeConfirmation"] = volumeScore;
            totalScore += volumeScore;
            
            var rating = totalScore switch
            {
                >= 81 => SignalRating.Excellent,
                >= 61 => SignalRating.Strong,
                >= 41 => SignalRating.Good,
                >= 21 => SignalRating.Fair,
                _ => SignalRating.Poor
            };
            
            var reason = $"Scored {totalScore:F1}/100 - " +
                        $"Confidence: {signal.Confidence:F0}%, " +
                        $"Strength: {signal.Strength:F0}%, " +
                        $"Supporting: {signal.SupportingIndicators}";
            
            scoredSignals.Add(new ScoredSignal
            {
                Signal = signal,
                OverallScore = Math.Round(totalScore, 1),
                ScoreBreakdown = scoreBreakdown,
                ScoreReason = reason,
                Rating = rating
            });
        }
        
        return scoredSignals.OrderByDescending(s => s.OverallScore).ToList();
    }

    public async Task<ConsensusSignal> AggregateSignalsAsync(List<TradingSignal> signals, SignalAggregationSettings settings)
    {
        if (!signals.Any())
        {
            return new ConsensusSignal
            {
                ConsensusType = SignalType.Hold,
                ConsensusConfidence = 0,
                TotalSignals = 0
            };
        }

        var bullishSignals = signals.Where(s => s.SignalType == SignalType.Buy || s.SignalType == SignalType.StrongBuy).ToList();
        var bearishSignals = signals.Where(s => s.SignalType == SignalType.Sell || s.SignalType == SignalType.StrongSell).ToList();
        var neutralSignals = signals.Where(s => s.SignalType == SignalType.Hold).ToList();
        
        // Calculate weighted votes
        decimal bullishWeight = 0, bearishWeight = 0;
        
        foreach (var signal in bullishSignals)
        {
            var sourceWeight = settings.SourceWeights.GetValueOrDefault(signal.Source, 1.0m);
            var timeDecay = settings.ApplyTimeDecay ? CalculateTimeDecay(signal.GeneratedAt, settings.TimeDecayWindow) : 1.0m;
            bullishWeight += signal.Confidence * signal.Strength * sourceWeight * timeDecay / 10000m; // Normalize
        }
        
        foreach (var signal in bearishSignals)
        {
            var sourceWeight = settings.SourceWeights.GetValueOrDefault(signal.Source, 1.0m);
            var timeDecay = settings.ApplyTimeDecay ? CalculateTimeDecay(signal.GeneratedAt, settings.TimeDecayWindow) : 1.0m;
            bearishWeight += signal.Confidence * signal.Strength * sourceWeight * timeDecay / 10000m; // Normalize
        }
        
        var totalWeight = bullishWeight + bearishWeight;
        var consensusType = SignalType.Hold;
        var consensusConfidence = 0m;
        var consensusStrength = 0m;
        var reason = "No clear consensus";
        
        if (totalWeight > 0)
        {
            var bullishPercentage = bullishWeight / totalWeight * 100;
            var bearishPercentage = bearishWeight / totalWeight * 100;
            
            if (bullishPercentage >= settings.MinConsensusThreshold)
            {
                consensusType = SignalType.Buy;
                consensusConfidence = bullishPercentage;
                consensusStrength = bullishSignals.Average(s => s.Strength);
                reason = $"{bullishPercentage:F1}% bullish consensus from {bullishSignals.Count} signals";
            }
            else if (bearishPercentage >= settings.MinConsensusThreshold)
            {
                consensusType = SignalType.Sell;
                consensusConfidence = bearishPercentage;
                consensusStrength = bearishSignals.Average(s => s.Strength);
                reason = $"{bearishPercentage:F1}% bearish consensus from {bearishSignals.Count} signals";
            }
            else
            {
                // Conflicting signals - reduce strength
                consensusConfidence = Math.Max(bullishPercentage, bearishPercentage) * settings.ConflictingSignalDiscount / 100;
                reason = $"Conflicting signals - {bullishSignals.Count} bullish, {bearishSignals.Count} bearish";
            }
        }
        
        return new ConsensusSignal
        {
            SymbolId = signals.First().SymbolId,
            Timeframe = signals.First().Timeframe,
            ConsensusType = consensusType,
            ConsensusConfidence = Math.Round(consensusConfidence, 1),
            ConsensusStrength = Math.Round(consensusStrength, 1),
            ContributingSignals = signals,
            TotalSignals = signals.Count,
            BullishSignals = bullishSignals.Count,
            BearishSignals = bearishSignals.Count,
            NeutralSignals = neutralSignals.Count,
            ConsensusReason = reason
        };
    }

    public async Task<List<TradingSignal>> FilterSignalsAsync(List<TradingSignal> signals, SignalFilterSettings settings)
    {
        var filtered = signals.AsEnumerable();
        
        // Quality filters
        filtered = filtered.Where(s => s.Confidence >= settings.MinConfidence);
        filtered = filtered.Where(s => s.ReliabilityScore >= settings.MinReliability);
        
        // Signal type filters
        if (settings.AllowedSignalTypes.Any())
        {
            filtered = filtered.Where(s => settings.AllowedSignalTypes.Contains(s.SignalType));
        }
        
        // Source filters
        if (settings.PreferredSources.Any())
        {
            filtered = filtered.Where(s => settings.PreferredSources.Contains(s.Source));
        }
        
        if (settings.ExcludedSources.Any())
        {
            filtered = filtered.Where(s => !settings.ExcludedSources.Contains(s.Source));
        }
        
        // Age filter
        var cutoffTime = DateTime.UtcNow.Subtract(settings.MaxSignalAge);
        filtered = filtered.Where(s => s.GeneratedAt >= cutoffTime);
        
        // Remove duplicates if enabled
        if (settings.RemoveDuplicates)
        {
            filtered = RemoveDuplicateSignals(filtered.ToList(), settings);
        }
        
        return filtered.ToList();
    }

    public async Task<SignalPerformanceStats> GetSignalPerformanceAsync(Guid symbolId, string timeframe, DateTime fromDate)
    {
        // This would typically query historical signal performance data
        // For now, return mock performance stats
        
        return new SignalPerformanceStats
        {
            SymbolId = symbolId,
            Timeframe = timeframe,
            TotalSignals = 100,
            ProfitableSignals = 65,
            WinRate = 65m,
            AverageReturn = 2.3m,
            MaxReturn = 15.2m,
            MinReturn = -8.1m,
            AverageHoldingTime = 4.2m,
            BySignalType = new Dictionary<SignalType, PerformanceMetrics>
            {
                [SignalType.Buy] = new() { Count = 60, WinRate = 68m, AverageReturn = 2.8m },
                [SignalType.Sell] = new() { Count = 40, WinRate = 60m, AverageReturn = 1.5m }
            }
        };
    }

    public async Task SubscribeToSignalsAsync(Guid symbolId, string timeframe, Func<TradingSignal, Task> onSignalGenerated)
    {
        var key = $"{symbolId}_{timeframe}";
        _subscribers.AddOrUpdate(key,
            new List<Func<TradingSignal, Task>> { onSignalGenerated },
            (k, existing) => { existing.Add(onSignalGenerated); return existing; });
            
        _logger.LogInformation("Added signal subscription for {SymbolId} {Timeframe}", symbolId, timeframe);
    }

    // Private signal generation methods
    private async Task<TradingSignal?> GenerateRSISignalAsync(Guid symbolId, string timeframe, IndicatorValues indicators, SignalGenerationSettings settings)
    {
        if (!indicators.Rsi.HasValue)
            return null;

        var rsi = indicators.Rsi.Value;
        
        if (rsi <= settings.RSIOversoldLevel)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Buy,
                Source = SignalSource.RSI,
                Confidence = Math.Min(100m, (settings.RSIOversoldLevel - rsi + 10) * 2),
                Strength = Math.Min(100m, (settings.RSIOversoldLevel - rsi + 15) * 1.5m),
                Price = indicators.Close,
                Reason = $"RSI oversold at {rsi:F1} (threshold: {settings.RSIOversoldLevel})",
                IndicatorValues = new Dictionary<string, object> { ["RSI"] = rsi },
                ReliabilityScore = 75m,
                SupportingIndicators = 1,
                MarketConditionScore = 70m
            };
        }
        else if (rsi >= settings.RSIOverboughtLevel)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Sell,
                Source = SignalSource.RSI,
                Confidence = Math.Min(100m, (rsi - settings.RSIOverboughtLevel + 10) * 2),
                Strength = Math.Min(100m, (rsi - settings.RSIOverboughtLevel + 15) * 1.5m),
                Price = indicators.Close,
                Reason = $"RSI overbought at {rsi:F1} (threshold: {settings.RSIOverboughtLevel})",
                IndicatorValues = new Dictionary<string, object> { ["RSI"] = rsi },
                ReliabilityScore = 75m,
                SupportingIndicators = 1,
                MarketConditionScore = 70m
            };
        }
        
        return null;
    }
    
    private async Task<TradingSignal?> GenerateMACDSignalAsync(Guid symbolId, string timeframe, IndicatorValues indicators, SignalGenerationSettings settings)
    {
        if (!indicators.Macd.HasValue || !indicators.MacdSignal.HasValue || !indicators.MacdHistogram.HasValue)
            return null;

        var macd = indicators.Macd.Value;
        var signal = indicators.MacdSignal.Value;
        var histogram = indicators.MacdHistogram.Value;
        
        // MACD bullish crossover
        if (macd > signal && histogram > 0)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Buy,
                Source = SignalSource.MACD,
                Confidence = Math.Min(100m, Math.Abs(histogram) * 100 + 50),
                Strength = Math.Min(100m, Math.Abs(macd - signal) * 50 + 40),
                Price = indicators.Close,
                Reason = $"MACD bullish crossover - MACD: {macd:F4}, Signal: {signal:F4}",
                IndicatorValues = new Dictionary<string, object> 
                { 
                    ["MACD"] = macd, 
                    ["Signal"] = signal, 
                    ["Histogram"] = histogram 
                },
                ReliabilityScore = 80m,
                SupportingIndicators = 1,
                MarketConditionScore = 75m
            };
        }
        // MACD bearish crossover
        else if (macd < signal && histogram < 0)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Sell,
                Source = SignalSource.MACD,
                Confidence = Math.Min(100m, Math.Abs(histogram) * 100 + 50),
                Strength = Math.Min(100m, Math.Abs(macd - signal) * 50 + 40),
                Price = indicators.Close,
                Reason = $"MACD bearish crossover - MACD: {macd:F4}, Signal: {signal:F4}",
                IndicatorValues = new Dictionary<string, object> 
                { 
                    ["MACD"] = macd, 
                    ["Signal"] = signal, 
                    ["Histogram"] = histogram 
                },
                ReliabilityScore = 80m,
                SupportingIndicators = 1,
                MarketConditionScore = 75m
            };
        }
        
        return null;
    }
    
    private async Task<TradingSignal?> GenerateBollingerSignalAsync(Guid symbolId, string timeframe, IndicatorValues indicators, SignalGenerationSettings settings)
    {
        if (!indicators.BbUpper.HasValue || !indicators.BbLower.HasValue || !indicators.BbPosition.HasValue)
            return null;

        var price = indicators.Close;
        var bbUpper = indicators.BbUpper.Value;
        var bbLower = indicators.BbLower.Value;
        var bbPosition = indicators.BbPosition.Value;
        
        // Bollinger Band bounce signals
        if (bbPosition <= 0.1m) // Near lower band
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Buy,
                Source = SignalSource.BollingerBands,
                Confidence = Math.Min(100m, (0.1m - bbPosition) * 500 + 60),
                Strength = Math.Min(100m, (0.1m - bbPosition) * 400 + 50),
                Price = price,
                Reason = $"Price near Bollinger lower band - Position: {bbPosition:F3}",
                IndicatorValues = new Dictionary<string, object> 
                { 
                    ["BBUpper"] = bbUpper,
                    ["BBLower"] = bbLower,
                    ["BBPosition"] = bbPosition
                },
                ReliabilityScore = 70m,
                SupportingIndicators = 1,
                MarketConditionScore = 65m
            };
        }
        else if (bbPosition >= 0.9m) // Near upper band
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Sell,
                Source = SignalSource.BollingerBands,
                Confidence = Math.Min(100m, (bbPosition - 0.9m) * 500 + 60),
                Strength = Math.Min(100m, (bbPosition - 0.9m) * 400 + 50),
                Price = price,
                Reason = $"Price near Bollinger upper band - Position: {bbPosition:F3}",
                IndicatorValues = new Dictionary<string, object> 
                { 
                    ["BBUpper"] = bbUpper,
                    ["BBLower"] = bbLower,
                    ["BBPosition"] = bbPosition
                },
                ReliabilityScore = 70m,
                SupportingIndicators = 1,
                MarketConditionScore = 65m
            };
        }
        
        return null;
    }
    
    private async Task<TradingSignal?> GenerateStochasticSignalAsync(Guid symbolId, string timeframe, IndicatorValues indicators, SignalGenerationSettings settings)
    {
        if (!indicators.StochK.HasValue || !indicators.StochD.HasValue)
            return null;

        var stochK = indicators.StochK.Value;
        var stochD = indicators.StochD.Value;
        
        // Stochastic oversold/overbought signals
        if (stochK <= 20 && stochD <= 20 && stochK > stochD)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Buy,
                Source = SignalSource.Stochastic,
                Confidence = Math.Min(100m, (20 - Math.Min(stochK, stochD)) * 3 + 50),
                Strength = Math.Min(100m, (stochK - stochD) * 5 + 40),
                Price = indicators.Close,
                Reason = $"Stochastic oversold bullish signal - %K: {stochK:F1}, %D: {stochD:F1}",
                IndicatorValues = new Dictionary<string, object> { ["%K"] = stochK, ["%D"] = stochD },
                ReliabilityScore = 65m,
                SupportingIndicators = 1,
                MarketConditionScore = 60m
            };
        }
        else if (stochK >= 80 && stochD >= 80 && stochK < stochD)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Sell,
                Source = SignalSource.Stochastic,
                Confidence = Math.Min(100m, (Math.Max(stochK, stochD) - 80) * 3 + 50),
                Strength = Math.Min(100m, (stochD - stochK) * 5 + 40),
                Price = indicators.Close,
                Reason = $"Stochastic overbought bearish signal - %K: {stochK:F1}, %D: {stochD:F1}",
                IndicatorValues = new Dictionary<string, object> { ["%K"] = stochK, ["%D"] = stochD },
                ReliabilityScore = 65m,
                SupportingIndicators = 1,
                MarketConditionScore = 60m
            };
        }
        
        return null;
    }
    
    private async Task<TradingSignal?> GenerateSupportResistanceSignalAsync(Guid symbolId, string timeframe, List<Candle> candles, IndicatorValues indicators, SignalGenerationSettings settings)
    {
        if (candles.Count < 20)
            return null;

        var highs = candles.Select(c => c.High).ToList();
        var lows = candles.Select(c => c.Low).ToList();
        var currentPrice = indicators.Close;
        
        try
        {
            var sr = _indicatorService.CalculateSupportResistance(highs, lows, Math.Min(50, candles.Count - 1));
            
            // Signal when price is near support (buy) or resistance (sell)
            if (sr.CurrentSupport > 0)
            {
                var distanceToSupport = Math.Abs(currentPrice - sr.CurrentSupport) / sr.CurrentSupport * 100;
                if (distanceToSupport <= 1.0m) // Within 1% of support
                {
                    return new TradingSignal
                    {
                        SymbolId = symbolId,
                        Timeframe = timeframe,
                        SignalType = SignalType.Buy,
                        Source = SignalSource.SupportResistance,
                        Confidence = Math.Min(100m, (1.0m - (decimal)distanceToSupport) * 80 + 60),
                        Strength = Math.Min(100m, (1.0m - (decimal)distanceToSupport) * 70 + 50),
                        Price = currentPrice,
                        Reason = $"Price near support level at {sr.CurrentSupport:F2} (distance: {distanceToSupport:F2}%)",
                        IndicatorValues = new Dictionary<string, object> 
                        { 
                            ["Support"] = sr.CurrentSupport,
                            ["Distance"] = distanceToSupport
                        },
                        ReliabilityScore = 85m,
                        SupportingIndicators = 1,
                        MarketConditionScore = 80m
                    };
                }
            }
            
            if (sr.CurrentResistance > 0)
            {
                var distanceToResistance = Math.Abs(sr.CurrentResistance - currentPrice) / sr.CurrentResistance * 100;
                if (distanceToResistance <= 1.0m) // Within 1% of resistance
                {
                    return new TradingSignal
                    {
                        SymbolId = symbolId,
                        Timeframe = timeframe,
                        SignalType = SignalType.Sell,
                        Source = SignalSource.SupportResistance,
                        Confidence = Math.Min(100m, (1.0m - (decimal)distanceToResistance) * 80 + 60),
                        Strength = Math.Min(100m, (1.0m - (decimal)distanceToResistance) * 70 + 50),
                        Price = currentPrice,
                        Reason = $"Price near resistance level at {sr.CurrentResistance:F2} (distance: {distanceToResistance:F2}%)",
                        IndicatorValues = new Dictionary<string, object> 
                        { 
                            ["Resistance"] = sr.CurrentResistance,
                            ["Distance"] = distanceToResistance
                        },
                        ReliabilityScore = 85m,
                        SupportingIndicators = 1,
                        MarketConditionScore = 80m
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating support/resistance signal for {SymbolId}", symbolId);
        }
        
        return null;
    }
    
    private async Task<TradingSignal?> GenerateVolumeSignalAsync(Guid symbolId, string timeframe, IndicatorValues indicators, SignalGenerationSettings settings)
    {
        if (!indicators.VolumeAvg20.HasValue || !indicators.VolumeRatio.HasValue)
            return null;

        var volumeRatio = indicators.VolumeRatio.Value;
        
        // High volume breakout signal
        if (volumeRatio >= 2.0m) // Volume is 2x average
        {
            // Determine direction based on price action (simplified)
            var signalType = SignalType.Buy; // Default to buy on high volume
            
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = signalType,
                Source = SignalSource.VolumeAnalysis,
                Confidence = Math.Min(100m, volumeRatio * 25 + 30),
                Strength = Math.Min(100m, volumeRatio * 20 + 40),
                Price = indicators.Close,
                Reason = $"High volume breakout - Volume ratio: {volumeRatio:F1}x average",
                IndicatorValues = new Dictionary<string, object> { ["VolumeRatio"] = volumeRatio },
                ReliabilityScore = 75m,
                SupportingIndicators = 1,
                MarketConditionScore = 70m
            };
        }
        
        return null;
    }
    
    private async Task<TradingSignal?> GeneratePriceActionSignalAsync(Guid symbolId, string timeframe, List<Candle> candles, IndicatorValues indicators, SignalGenerationSettings settings)
    {
        if (candles.Count < 3)
            return null;

        var lastThree = candles.TakeLast(3).ToList();
        var current = lastThree[2];
        var previous = lastThree[1];
        var beforePrevious = lastThree[0];
        
        // Hammer/Doji pattern detection (simplified)
        var bodySize = Math.Abs(current.Close - current.Open);
        var totalRange = current.High - current.Low;
        var lowerShadow = Math.Min(current.Open, current.Close) - current.Low;
        var upperShadow = current.High - Math.Max(current.Open, current.Close);
        
        // Hammer pattern (potential bullish reversal)
        if (bodySize < totalRange * 0.3m && lowerShadow > bodySize * 2 && upperShadow < bodySize)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Buy,
                Source = SignalSource.PriceAction,
                Confidence = 65m,
                Strength = 55m,
                Price = current.Close,
                Reason = "Hammer candlestick pattern detected",
                IndicatorValues = new Dictionary<string, object> 
                { 
                    ["Pattern"] = "Hammer",
                    ["BodySize"] = bodySize,
                    ["LowerShadow"] = lowerShadow
                },
                ReliabilityScore = 60m,
                SupportingIndicators = 1,
                MarketConditionScore = 55m
            };
        }
        
        // Shooting star pattern (potential bearish reversal)
        if (bodySize < totalRange * 0.3m && upperShadow > bodySize * 2 && lowerShadow < bodySize)
        {
            return new TradingSignal
            {
                SymbolId = symbolId,
                Timeframe = timeframe,
                SignalType = SignalType.Sell,
                Source = SignalSource.PriceAction,
                Confidence = 65m,
                Strength = 55m,
                Price = current.Close,
                Reason = "Shooting star candlestick pattern detected",
                IndicatorValues = new Dictionary<string, object> 
                { 
                    ["Pattern"] = "ShootingStar",
                    ["BodySize"] = bodySize,
                    ["UpperShadow"] = upperShadow
                },
                ReliabilityScore = 60m,
                SupportingIndicators = 1,
                MarketConditionScore = 55m
            };
        }
        
        return null;
    }
    
    private decimal CalculateTimeDecay(DateTime signalTime, TimeSpan decayWindow)
    {
        var age = DateTime.UtcNow - signalTime;
        if (age >= decayWindow)
            return 0.1m; // Minimum weight
            
        var decayFactor = (decimal)(1 - (age.TotalMinutes / decayWindow.TotalMinutes));
        return Math.Max(0.1m, decayFactor);
    }
    
    private List<TradingSignal> RemoveDuplicateSignals(List<TradingSignal> signals, SignalFilterSettings settings)
    {
        var filtered = new List<TradingSignal>();
        
        foreach (var signal in signals.OrderByDescending(s => s.Confidence))
        {
            var isDuplicate = filtered.Any(existing => 
                existing.SymbolId == signal.SymbolId &&
                existing.Timeframe == signal.Timeframe &&
                existing.SignalType == signal.SignalType &&
                Math.Abs((signal.GeneratedAt - existing.GeneratedAt).TotalMinutes) <= settings.DuplicateTimeWindow.TotalMinutes &&
                Math.Abs((signal.Price - existing.Price) / existing.Price * 100) <= settings.DuplicatePriceThreshold);
                
            if (!isDuplicate)
            {
                filtered.Add(signal);
            }
        }
        
        return filtered;
    }
}