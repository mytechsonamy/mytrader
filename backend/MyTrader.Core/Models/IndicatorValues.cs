using System.ComponentModel.DataAnnotations;

namespace MyTrader.Core.Models;

public class IndicatorValues
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid SymbolId { get; set; }
    
    [Required]
    public string Timeframe { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    // Price data (OHLCV)
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    
    // RSI
    public decimal? Rsi { get; set; }
    public decimal? RsiSma { get; set; }
    
    // MACD
    public decimal? Macd { get; set; }
    public decimal? MacdSignal { get; set; }
    public decimal? MacdHistogram { get; set; }
    
    // Bollinger Bands
    public decimal? BbUpper { get; set; }
    public decimal? BbMiddle { get; set; }
    public decimal? BbLower { get; set; }
    public decimal? BbPosition { get; set; } // Position relative to bands (-1 to 1)
    
    // EMA (Exponential Moving Averages)
    public decimal? Ema9 { get; set; }
    public decimal? Ema21 { get; set; }
    public decimal? Ema50 { get; set; }
    public decimal? Ema100 { get; set; }
    public decimal? Ema200 { get; set; }
    
    // SMA (Simple Moving Averages)
    public decimal? Sma20 { get; set; }
    public decimal? Sma50 { get; set; }
    public decimal? Sma100 { get; set; }
    public decimal? Sma200 { get; set; }
    
    // ATR (Average True Range)
    public decimal? Atr { get; set; }
    public decimal? AtrPercentage { get; set; }
    
    // Volume Indicators
    public decimal? VolumeAvg20 { get; set; }
    public decimal? VolumeRatio { get; set; }
    public decimal? Vwap { get; set; } // Volume Weighted Average Price
    
    // Stochastic
    public decimal? StochK { get; set; }
    public decimal? StochD { get; set; }
    
    // Williams %R
    public decimal? WilliamsR { get; set; }
    
    // Commodity Channel Index
    public decimal? Cci { get; set; }
    
    // Money Flow Index
    public decimal? Mfi { get; set; }
    
    // Relative Volume
    public decimal? RelativeVolume { get; set; }
    
    // Volatility
    public decimal? Volatility { get; set; }
    
    // Support and Resistance
    public decimal? Support1 { get; set; }
    public decimal? Support2 { get; set; }
    public decimal? Resistance1 { get; set; }
    public decimal? Resistance2 { get; set; }
    
    // Trend Direction
    public decimal? TrendDirection { get; set; } // -1 (down), 0 (sideways), 1 (up)
    public decimal? TrendStrength { get; set; } // 0-1 scale
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Additional custom indicators as JSON
    public string? CustomIndicators { get; set; } = "{}";
    
    // Navigation properties
    public Symbol Symbol { get; set; } = null!;
}