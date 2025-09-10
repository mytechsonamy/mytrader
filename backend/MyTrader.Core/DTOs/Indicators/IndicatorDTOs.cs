using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.DTOs.Indicators;

public class IndicatorResponse
{
    public Guid SymbolId { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Basic indicators
    public decimal? RSI { get; set; }
    public MACDDto? MACD { get; set; }
    public BollingerBandsDto? BollingerBands { get; set; }
    
    // Moving averages
    public decimal? EMA9 { get; set; }
    public decimal? EMA21 { get; set; }
    public decimal? EMA50 { get; set; }
    public decimal? EMA100 { get; set; }
    public decimal? EMA200 { get; set; }
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? SMA100 { get; set; }
    public decimal? SMA200 { get; set; }
    
    // Volume
    public decimal? Volume { get; set; }
    public decimal? VWAP { get; set; }
    public decimal? VolumeRatio { get; set; }
    public decimal? OBV { get; set; }
    
    // Volatility
    public ATRDto? ATR { get; set; }
    
    // Advanced indicators (optional)
    public StochasticDto? Stochastic { get; set; }
    public decimal? Williams { get; set; }
    public decimal? CCI { get; set; }
    public decimal? MFI { get; set; }
    public ADXDto? ADX { get; set; }
    public IchimokuDto? Ichimoku { get; set; }
    public SupportResistanceDto? SupportResistance { get; set; }
    
    // Quality metrics
    public IndicatorQualityDto? Quality { get; set; }
}

public class MACDDto
{
    public decimal Value { get; set; }
    public decimal Signal { get; set; }
    public decimal Histogram { get; set; }
}

public class BollingerBandsDto
{
    public decimal Upper { get; set; }
    public decimal Middle { get; set; }
    public decimal Lower { get; set; }
    public decimal Position { get; set; } // 0-1 where price sits within bands
}

public class ATRDto
{
    public decimal Value { get; set; }
    public decimal Percentage { get; set; }
}

public class StochasticDto
{
    public decimal K { get; set; }
    public decimal D { get; set; }
}

public class ADXDto
{
    public decimal Value { get; set; }
    public decimal PlusDI { get; set; }
    public decimal MinusDI { get; set; }
    public string TrendDirection { get; set; } = string.Empty;
    public decimal TrendStrength { get; set; }
}

public class IchimokuDto
{
    public decimal TenkanSen { get; set; }
    public decimal KijunSen { get; set; }
    public decimal SenkouSpanA { get; set; }
    public decimal SenkouSpanB { get; set; }
    public decimal ChikouSpan { get; set; }
    public string CloudStatus { get; set; } = string.Empty;
}

public class SupportResistanceDto
{
    public decimal CurrentSupport { get; set; }
    public decimal CurrentResistance { get; set; }
    public List<decimal> SupportLevels { get; set; } = new();
    public List<decimal> ResistanceLevels { get; set; } = new();
    public decimal DistanceToSupport { get; set; }
    public decimal DistanceToResistance { get; set; }
}

public class IndicatorQualityDto
{
    public decimal ReliabilityScore { get; set; }
    public int DataPoints { get; set; }
    public bool HasSufficientData { get; set; }
    public List<string> Warnings { get; set; } = new();
}

// Signal DTOs
public class SignalResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public List<SignalInfo> Signals { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class SignalInfo
{
    public string Type { get; set; } = string.Empty; // Buy, Sell, Hold
    public string Source { get; set; } = string.Empty; // RSI, MACD, etc.
    public decimal Confidence { get; set; }
    public decimal Strength { get; set; }
    public decimal Price { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class ConsensusResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string ConsensusType { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public decimal Strength { get; set; }
    public int TotalSignals { get; set; }
    public int BullishSignals { get; set; }
    public int BearishSignals { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class PerformanceResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int TotalSignals { get; set; }
    public int ProfitableSignals { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageReturn { get; set; }
    public decimal MaxReturn { get; set; }
    public decimal MinReturn { get; set; }
    public decimal AverageHoldingTime { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class SymbolInfo
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
}

// Settings DTOs
public class IndicatorSettingsResponse
{
    public RSISettingsDto RSI { get; set; } = new();
    public MACDSettingsDto MACD { get; set; } = new();
    public BollingerBandsSettingsDto BollingerBands { get; set; } = new();
    public List<int> EMAPeriods { get; set; } = new();
    public List<int> SMAPeriods { get; set; } = new();
    public int ATRPeriod { get; set; }
    public int SupportResistanceLookback { get; set; }
}

public class RSISettingsDto
{
    [Range(5, 50)]
    public int Period { get; set; } = 14;
    
    [Range(50, 90)]
    public decimal OverboughtLevel { get; set; } = 70;
    
    [Range(10, 50)]
    public decimal OversoldLevel { get; set; } = 30;
}

public class MACDSettingsDto
{
    [Range(5, 20)]
    public int FastPeriod { get; set; } = 12;
    
    [Range(15, 50)]
    public int SlowPeriod { get; set; } = 26;
    
    [Range(5, 15)]
    public int SignalPeriod { get; set; } = 9;
}

public class BollingerBandsSettingsDto
{
    [Range(10, 30)]
    public int Period { get; set; } = 20;
    
    [Range(1.0, 3.0)]
    public decimal Multiplier { get; set; } = 2.0m;
}

public class IndicatorPreferencesRequest
{
    public RSISettingsDto? RSI { get; set; }
    public MACDSettingsDto? MACD { get; set; }
    public BollingerBandsSettingsDto? BollingerBands { get; set; }
    public List<int>? EMAPeriods { get; set; }
    public List<int>? SMAPeriods { get; set; }
    public int? ATRPeriod { get; set; }
    public bool EnableRealTimeSignals { get; set; } = true;
    public decimal MinSignalConfidence { get; set; } = 50m;
    public List<string> PreferredSignalSources { get; set; } = new();
}

// Real-time updates
public class IndicatorUpdateDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> UpdatedIndicators { get; set; } = new();
    public List<SignalInfo> NewSignals { get; set; } = new();
}

public class IndicatorSubscriptionRequest
{
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
    
    public List<string> IndicatorTypes { get; set; } = new(); // RSI, MACD, etc.
    public bool IncludeSignals { get; set; } = true;
    public decimal MinSignalConfidence { get; set; } = 50m;
}

// Batch requests
public class BatchIndicatorRequest
{
    public List<SymbolTimeframe> Symbols { get; set; } = new();
    public List<string> IndicatorTypes { get; set; } = new();
    public bool IncludeExtended { get; set; } = false;
}

public class SymbolTimeframe
{
    [Required]
    public string Symbol { get; set; } = string.Empty;
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
}

public class BatchIndicatorResponse
{
    public List<IndicatorResponse> Indicators { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public int TotalCount { get; set; }
    public List<string> Errors { get; set; } = new();
}