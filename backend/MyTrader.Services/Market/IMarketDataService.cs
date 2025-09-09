using MyTrader.Core.DTOs.Market;

namespace MyTrader.Services.Market;

public interface IMarketDataService
{
    Task<ImportResponse> ImportDailyPricesAsync(ImportRequest request);
    Task<MarketDataResponse> GetMarketDataAsync(string symbol, string timeframe, DateTime? start = null, DateTime? end = null);
}

public class ImportResponse
{
    public string Message { get; set; } = string.Empty;
    public int Inserted { get; set; }
}

public class MarketDataResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public List<CandleData> Data { get; set; } = new();
}

public class CandleData
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}