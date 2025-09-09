namespace MyTrader.Core.Models.Indicators;

public class RSI
{
    public decimal Value { get; set; }
}

public class RSISettings
{
    public int Period { get; set; } = 14;
}