using MyTrader.Core.DTOs.Signals;

namespace MyTrader.Services.Signals;

public interface ISignalService
{
    Task<SignalsListResponse> GetSignalsAsync(int limit = 50, int cursor = 0);
    Task<MarketDataResponse> GetCurrentMarketDataAsync();
}

public class MarketDataResponse
{
    public decimal Price { get; set; }
    public string? CurrentSignal { get; set; }
    public string? BbPosition { get; set; }
    public MarketIndicatorValues? Indicators { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MarketIndicatorValues
{
    public decimal? Rsi { get; set; }
    public decimal? Macd { get; set; }
    public decimal? BbLower { get; set; }
    public decimal? BbUpper { get; set; }
}