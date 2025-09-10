namespace MyTrader.Core.Models.Indicators;

public class StochasticOscillator
{
    public decimal K { get; set; } // %K line
    public decimal D { get; set; } // %D line (signal)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class StochasticSettings
{
    public int KPeriod { get; set; } = 14;
    public int DPeriod { get; set; } = 3; // SMA period for %D
    public int Slowing { get; set; } = 3; // Slowing factor
}

public class ATR
{
    public decimal Value { get; set; }
    public decimal Percentage { get; set; } // ATR as percentage of current price
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Williams
{
    public decimal Value { get; set; } // Williams %R value (-100 to 0)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CCI
{
    public decimal Value { get; set; } // Commodity Channel Index
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class MFI
{
    public decimal Value { get; set; } // Money Flow Index (0-100)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ADX
{
    public decimal Value { get; set; } // ADX value
    public decimal PlusDI { get; set; } // +DI
    public decimal MinusDI { get; set; } // -DI
    public TrendDirection Direction { get; set; }
    public decimal TrendStrength { get; set; } // 0-100
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Ichimoku
{
    public decimal TenkanSen { get; set; } // Conversion line (9-period)
    public decimal KijunSen { get; set; } // Base line (26-period)
    public decimal SenkouSpanA { get; set; } // Leading span A
    public decimal SenkouSpanB { get; set; } // Leading span B
    public decimal ChikouSpan { get; set; } // Lagging span
    public CloudStatus CloudStatus { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class IchimokuSettings
{
    public int TenkanPeriod { get; set; } = 9;
    public int KijunPeriod { get; set; } = 26;
    public int SenkouBPeriod { get; set; } = 52;
    public int Displacement { get; set; } = 26;
}

public class SupportResistance
{
    public List<decimal> SupportLevels { get; set; } = new();
    public List<decimal> ResistanceLevels { get; set; } = new();
    public decimal CurrentSupport { get; set; }
    public decimal CurrentResistance { get; set; }
    public decimal DistanceToSupport { get; set; }
    public decimal DistanceToResistance { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class IndicatorSettings
{
    // Basic indicators
    public BollingerBandSettings BollingerBands { get; set; } = new();
    public RSISettings RSI { get; set; } = new();
    public MACDSettings MACD { get; set; } = new();
    
    // Advanced indicators
    public StochasticSettings Stochastic { get; set; } = new();
    public IchimokuSettings Ichimoku { get; set; } = new();
    
    // Periods for various indicators
    public int ATRPeriod { get; set; } = 14;
    public int WilliamsRPeriod { get; set; } = 14;
    public int CCIPeriod { get; set; } = 20;
    public int MFIPeriod { get; set; } = 14;
    public int ADXPeriod { get; set; } = 14;
    
    // EMA periods
    public List<int> EMAPeriods { get; set; } = new() { 9, 21, 50, 100, 200 };
    public List<int> SMAPeriods { get; set; } = new() { 20, 50, 100, 200 };
    
    // Support/Resistance
    public int SupportResistanceLookback { get; set; } = 50;
    
    // Volume indicators
    public bool CalculateVWAP { get; set; } = true;
    public bool CalculateOBV { get; set; } = true;
    
    // Performance optimization
    public bool EnableCaching { get; set; } = true;
    public int CacheExpiryMinutes { get; set; } = 15;
}

public enum TrendDirection
{
    Unknown = 0,
    Bullish = 1,
    Bearish = -1,
    Sideways = 2
}

public enum CloudStatus
{
    Unknown = 0,
    Bullish = 1,  // Price above cloud
    Bearish = -1, // Price below cloud
    InCloud = 2   // Price within cloud
}

public class IndicatorQuality
{
    public decimal ReliabilityScore { get; set; } // 0-100
    public int DataPoints { get; set; }
    public bool HasSufficientData { get; set; }
    public List<string> Warnings { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}