namespace MyTrader.Core.Models.Indicators;

public class MACD
{
    public decimal Value { get; set; }
    public decimal Signal { get; set; }
    public decimal Histogram { get; set; }
}

public class MACDSettings
{
    public int FastPeriod { get; set; } = 12;
    public int SlowPeriod { get; set; } = 26;
    public int SignalPeriod { get; set; } = 9;
}