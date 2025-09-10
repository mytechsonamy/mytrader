namespace MyTrader.Core.DTOs.Indicators;

public class RSI
{
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string Signal { get; set; } = string.Empty; // "Overbought", "Oversold", "Neutral"
}