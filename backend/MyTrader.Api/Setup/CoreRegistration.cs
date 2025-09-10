using Microsoft.Extensions.DependencyInjection;
using MyTrader.Services.Market;
using MyTrader.Core.Services;

namespace MyTrader.Api.Setup;

public static class CoreRegistration
{
    public static IServiceCollection AddMyTraderAgentPack(this IServiceCollection services)
    {
        // Register new Symbol and Alert services
        services.AddScoped<MyTrader.Services.Market.IAlertService, MyTrader.Services.Market.AlertService>();
        services.AddScoped<MyTrader.Core.Services.ISymbolService, MyTrader.Core.Services.SymbolService>();
        services.AddScoped<MyTrader.Core.Services.IMarketDataService, MyTrader.Core.Services.MarketDataService>();
        
        // Register queue management services
        services.AddScoped<MyTrader.Core.Services.IBacktestQueueService, MyTrader.Core.Services.BacktestQueueService>();
        services.AddSingleton<MyTrader.Core.Services.BacktestQueueProcessor>();
        
        // TODO: Register providers when implemented
        // services.AddScoped<IHistoricalDataProvider, BinanceHistoricalDataProvider>();
        // services.AddScoped<IRealtimeTickerProvider, BinanceRealtimeProvider>();
        
        return services;
    }
}