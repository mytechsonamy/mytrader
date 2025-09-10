namespace MyTrader.Core.DTOs.Market;

public class MarketDataResponse
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ImportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsImported { get; set; }
}