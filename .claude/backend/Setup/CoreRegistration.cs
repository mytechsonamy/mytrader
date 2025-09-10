using Microsoft.Extensions.DependencyInjection;
using MyTrader.Application.Interfaces;
using MyTrader.Application.Services;

namespace MyTrader.Setup;

public static class CoreRegistration
{
    public static IServiceCollection AddMyTraderAgentPack(this IServiceCollection services)
    {
        services.AddScoped<ISymbolService, SymbolService>();
        services.AddScoped<IAlertService, AlertService>();
        // Register providers: IHistoricalDataProvider, IRealtimeTickerProvider
        return services;
    }
}
