namespace MyTrader.Core.DTOs.Indicators;

public class BollingerBands
{
    public decimal UpperBand { get; set; }
    public decimal MiddleBand { get; set; }
    public decimal LowerBand { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime Timestamp { get; set; }
    public string Signal { get; set; } = string.Empty; // "Upper", "Lower", "Middle", "Neutral"
}