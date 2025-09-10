using System;
using System.Text.Json;
using System.Threading.Tasks;
using MyTrader.Contracts;

namespace MyTrader.Application.Interfaces;

public interface IStrategyService
{
    Task<StrategyTemplateResponse> CreateTemplateAsync(Guid userId, CreateStrategyTemplateRequest request);
    Task<UserStrategyResponse> CreateUserStrategyAsync(Guid userId, CreateUserStrategyRequest request);
}
