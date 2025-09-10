using System;
using System.Threading.Tasks;
using MyTrader.Contracts;

namespace MyTrader.Application.Interfaces;

public interface IBacktestService
{
    Task<(Guid id, string status)> StartAsync(Guid userId, Guid strategyId, BacktestRequest request);
    Task<BacktestResult?> GetResultAsync(Guid userId, Guid backtestId);
}
