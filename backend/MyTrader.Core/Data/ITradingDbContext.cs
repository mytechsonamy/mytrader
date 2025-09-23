using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}