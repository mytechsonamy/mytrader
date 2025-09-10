using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyTrader.Application.Interfaces;

public record CandlePoint(DateTimeOffset Ts, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

public interface IHistoricalDataProvider
{
    Task<IReadOnlyList<CandlePoint>> GetAsync(string ticker, string timeframe, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
