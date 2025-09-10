using Microsoft.Extensions.DependencyInjection;

namespace MyTrader.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyTraderCore(this IServiceCollection services)
    {
        // Core service registrations go here
        // The actual service implementations should be registered in Program.cs
        // to avoid circular dependencies between Infrastructure and Services
        
        return services;
    }
}