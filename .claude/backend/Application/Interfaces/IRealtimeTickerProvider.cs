using System.Collections.Generic;
using System.Threading;

namespace MyTrader.Application.Interfaces;

public record TickerUpdate(string Ticker, decimal Price, decimal Change, long Ts);

public interface IRealtimeTickerProvider
{
    IAsyncEnumerable<TickerUpdate> StreamAsync(IEnumerable<string> tickers, CancellationToken ct);
}
