namespace MyTrader.Core.DTOs.Signals;

public class SignalResponse
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? Rsi { get; set; }
    public decimal? Macd { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SignalsListResponse
{
    public List<SignalResponse> Items { get; set; } = new();
    public int? NextCursor { get; set; }
    public int Total { get; set; }
}