using MyTrader.Core.Models;
using MyTrader.Core.Models.Indicators;

namespace MyTrader.Services.Signals;

public interface ISignalGenerationEngine
{
    /// <summary>
    /// Generate trading signals for a symbol using current market data
    /// </summary>
    Task<List<TradingSignal>> GenerateSignalsAsync(Guid symbolId, string timeframe, List<Candle> candles, SignalGenerationSettings settings);
    
    /// <summary>
    /// Generate signals in real-time as new candle data arrives
    /// </summary>
    Task<List<TradingSignal>> GenerateRealTimeSignalsAsync(Guid symbolId, string timeframe, Candle newCandle, IndicatorValues currentIndicators, SignalGenerationSettings settings);
    
    /// <summary>
    /// Score and rank signals by quality and reliability
    /// </summary>
    Task<List<ScoredSignal>> ScoreSignalsAsync(List<TradingSignal> signals, SignalScoringSettings settings);
    
    /// <summary>
    /// Aggregate multiple signals into a consensus signal
    /// </summary>
    Task<ConsensusSignal> AggregateSignalsAsync(List<TradingSignal> signals, SignalAggregationSettings settings);
    
    /// <summary>
    /// Filter signals based on quality metrics and user preferences
    /// </summary>
    Task<List<TradingSignal>> FilterSignalsAsync(List<TradingSignal> signals, SignalFilterSettings settings);
    
    /// <summary>
    /// Get signal performance statistics for a symbol/timeframe
    /// </summary>
    Task<SignalPerformanceStats> GetSignalPerformanceAsync(Guid symbolId, string timeframe, DateTime fromDate);
    
    /// <summary>
    /// Subscribe to real-time signal updates for a symbol
    /// </summary>
    Task SubscribeToSignalsAsync(Guid symbolId, string timeframe, Func<TradingSignal, Task> onSignalGenerated);
}

public class TradingSignal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SymbolId { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public SignalType SignalType { get; set; }
    public SignalSource Source { get; set; }
    public decimal Confidence { get; set; } // 0-100
    public decimal Strength { get; set; } // Signal strength (0-100)
    public decimal Price { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> IndicatorValues { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public SignalStatus Status { get; set; } = SignalStatus.Active;
    
    // Quality metrics
    public decimal ReliabilityScore { get; set; } // Historical accuracy
    public int SupportingIndicators { get; set; } // How many indicators agree
    public decimal MarketConditionScore { get; set; } // How well suited for current market
    
    // Navigation properties
    public Symbol Symbol { get; set; } = null!;
}

public class ScoredSignal
{
    public TradingSignal Signal { get; set; } = null!;
    public decimal OverallScore { get; set; } // Composite score (0-100)
    public Dictionary<string, decimal> ScoreBreakdown { get; set; } = new();
    public string ScoreReason { get; set; } = string.Empty;
    public SignalRating Rating { get; set; }
}

public class ConsensusSignal
{
    public Guid SymbolId { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public SignalType ConsensusType { get; set; }
    public decimal ConsensusConfidence { get; set; }
    public decimal ConsensusStrength { get; set; }
    public List<TradingSignal> ContributingSignals { get; set; } = new();
    public int TotalSignals { get; set; }
    public int BullishSignals { get; set; }
    public int BearishSignals { get; set; }
    public int NeutralSignals { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ConsensusReason { get; set; } = string.Empty;
}

public class SignalPerformanceStats
{
    public Guid SymbolId { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public int TotalSignals { get; set; }
    public int ProfitableSignals { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageReturn { get; set; }
    public decimal MaxReturn { get; set; }
    public decimal MinReturn { get; set; }
    public decimal AverageHoldingTime { get; set; } // Hours
    public Dictionary<SignalType, PerformanceMetrics> BySignalType { get; set; } = new();
    public Dictionary<SignalSource, PerformanceMetrics> BySource { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class PerformanceMetrics
{
    public int Count { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageReturn { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal BestReturn { get; set; }
    public decimal WorstReturn { get; set; }
}

public enum SignalType
{
    Buy = 1,
    Sell = -1,
    Hold = 0,
    StrongBuy = 2,
    StrongSell = -2
}

public enum SignalSource
{
    RSI = 1,
    MACD = 2,
    BollingerBands = 3,
    Stochastic = 4,
    Williams = 5,
    CCI = 6,
    MFI = 7,
    ADX = 8,
    Ichimoku = 9,
    SupportResistance = 10,
    VolumeAnalysis = 11,
    PriceAction = 12,
    Consensus = 99
}

public enum SignalStatus
{
    Active = 1,
    Executed = 2,
    Expired = 3,
    Cancelled = 4,
    PartiallyFilled = 5
}

public enum SignalRating
{
    Poor = 1,      // 0-20
    Fair = 2,      // 21-40
    Good = 3,      // 41-60
    Strong = 4,    // 61-80
    Excellent = 5  // 81-100
}

public class SignalGenerationSettings
{
    // Indicator settings
    public IndicatorSettings Indicators { get; set; } = new();
    
    // Signal generation rules
    public bool EnableRSISignals { get; set; } = true;
    public bool EnableMACDSignals { get; set; } = true;
    public bool EnableBollingerSignals { get; set; } = true;
    public bool EnableStochasticSignals { get; set; } = true;
    public bool EnableWilliamsSignals { get; set; } = true;
    public bool EnableCCISignals { get; set; } = true;
    public bool EnableMFISignals { get; set; } = true;
    public bool EnableADXSignals { get; set; } = true;
    public bool EnableIchimokuSignals { get; set; } = true;
    public bool EnableSupportResistanceSignals { get; set; } = true;
    public bool EnableVolumeSignals { get; set; } = true;
    public bool EnablePriceActionSignals { get; set; } = true;
    
    // Signal thresholds
    public decimal RSIOverboughtLevel { get; set; } = 70m;
    public decimal RSIOversoldLevel { get; set; } = 30m;
    public decimal MinConfidence { get; set; } = 50m;
    public decimal MinStrength { get; set; } = 30m;
    
    // Risk management
    public decimal DefaultStopLossPercent { get; set; } = 2m;
    public decimal DefaultTakeProfitPercent { get; set; } = 4m;
    public bool UseATRForStops { get; set; } = true;
    public decimal ATRMultiplierForStops { get; set; } = 2m;
    
    // Signal lifecycle
    public TimeSpan SignalExpiryTime { get; set; } = TimeSpan.FromHours(4);
    public int MaxSignalsPerSymbol { get; set; } = 5;
    
    // Performance optimization
    public bool EnableParallelProcessing { get; set; } = true;
    public int MaxConcurrency { get; set; } = 4;
}

public class SignalScoringSettings
{
    // Scoring weights (should sum to 100)
    public decimal ConfidenceWeight { get; set; } = 25m;
    public decimal StrengthWeight { get; set; } = 20m;
    public decimal ReliabilityWeight { get; set; } = 20m;
    public decimal MarketConditionWeight { get; set; } = 15m;
    public decimal SupportingIndicatorsWeight { get; set; } = 10m;
    public decimal VolumeConfirmationWeight { get; set; } = 10m;
    
    // Historical performance lookback
    public int PerformanceLookbackDays { get; set; } = 30;
    public decimal MinHistoricalSamples { get; set; } = 10;
}

public class SignalAggregationSettings
{
    // Consensus rules
    public decimal MinConsensusThreshold { get; set; } = 60m; // % of signals that must agree
    public decimal ConflictingSignalDiscount { get; set; } = 50m; // Reduce strength when signals conflict
    
    // Signal weighting
    public Dictionary<SignalSource, decimal> SourceWeights { get; set; } = new()
    {
        { SignalSource.RSI, 1.0m },
        { SignalSource.MACD, 1.2m },
        { SignalSource.BollingerBands, 1.1m },
        { SignalSource.Stochastic, 0.9m },
        { SignalSource.ADX, 1.3m },
        { SignalSource.Ichimoku, 1.4m },
        { SignalSource.SupportResistance, 1.5m }
    };
    
    // Time decay
    public bool ApplyTimeDecay { get; set; } = true;
    public TimeSpan TimeDecayWindow { get; set; } = TimeSpan.FromMinutes(30);
}

public class SignalFilterSettings
{
    // Quality filters
    public decimal MinOverallScore { get; set; } = 60m;
    public SignalRating MinRating { get; set; } = SignalRating.Good;
    public decimal MinConfidence { get; set; } = 50m;
    public decimal MinReliability { get; set; } = 40m;
    
    // Market condition filters
    public bool FilterByVolume { get; set; } = true;
    public decimal MinVolumeRatio { get; set; } = 1.2m; // vs average volume
    public bool FilterBySpread { get; set; } = true;
    public decimal MaxSpreadPercent { get; set; } = 0.5m;
    
    // Duplicate detection
    public bool RemoveDuplicates { get; set; } = true;
    public TimeSpan DuplicateTimeWindow { get; set; } = TimeSpan.FromMinutes(15);
    public decimal DuplicatePriceThreshold { get; set; } = 0.1m; // %
    
    // User preferences
    public List<SignalType> AllowedSignalTypes { get; set; } = new() { SignalType.Buy, SignalType.Sell };
    public List<SignalSource> PreferredSources { get; set; } = new();
    public List<SignalSource> ExcludedSources { get; set; } = new();
    public TimeSpan MaxSignalAge { get; set; } = TimeSpan.FromHours(2);
}