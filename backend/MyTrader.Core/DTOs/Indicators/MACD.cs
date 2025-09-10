namespace MyTrader.Core.DTOs.Indicators;

public class MACD
{
    public decimal MACDLine { get; set; }
    public decimal SignalLine { get; set; }
    public decimal Histogram { get; set; }
    public DateTime Timestamp { get; set; }
    public string Signal { get; set; } = string.Empty; // "Bullish", "Bearish", "Neutral"
}