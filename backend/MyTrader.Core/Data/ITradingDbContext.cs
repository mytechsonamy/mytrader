using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyTrader.Core.Models;

namespace MyTrader.Core.Data;

public interface ITradingDbContext
{
    DbSet<User> Users { get; }
    DbSet<Symbol> Symbols { get; }
    DbSet<Strategy> Strategies { get; }
    DbSet<UserStrategy> UserStrategies { get; }
    DbSet<MarketData> MarketData { get; }
    DbSet<BacktestResults> BacktestResults { get; }
    DbSet<BacktestQueue> BacktestQueue { get; }
    DbSet<HistoricalMarketData> HistoricalMarketData { get; }
    DbSet<UserDashboardPreferences> UserDashboardPreferences { get; }

    // Additional DbSets
    DbSet<AssetClass> AssetClasses { get; }
    DbSet<Market> Markets { get; }
    DbSet<TradingSession> TradingSessions { get; }
    DbSet<DataProvider> DataProviders { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Generic Set method
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    // Database property for transactions
    DatabaseFacade Database { get; }
}