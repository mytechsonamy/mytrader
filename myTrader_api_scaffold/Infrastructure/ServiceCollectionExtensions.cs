using Microsoft.Extensions.DependencyInjection;
using MyTrader.Application.Interfaces;
using MyTrader.Application.Services;

namespace MyTrader.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyTraderCore(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStrategyService, StrategyService>();
        services.AddScoped<IBacktestService, BacktestService>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        return services;
    }
}
