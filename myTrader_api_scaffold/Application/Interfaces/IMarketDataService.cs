using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyTrader.Contracts;

namespace MyTrader.Application.Interfaces;

public interface IMarketDataService
{
    Task<IReadOnlyList<CandleDto>> GetCandlesAsync(string symbol, string timeframe, DateTimeOffset from, DateTimeOffset to);
}
