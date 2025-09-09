namespace MyTrader.Core.Models.Indicators;

public class BollingerBands
{
    public decimal Upper { get; set; }
    public decimal Middle { get; set; }
    public decimal Lower { get; set; }
}

public class BollingerBandSettings
{
    public int Period { get; set; } = 20;
    public decimal Multiplier { get; set; } = 2.0m;
}