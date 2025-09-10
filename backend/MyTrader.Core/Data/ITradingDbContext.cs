using Microsoft.EntityFrameworkCore;
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
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}