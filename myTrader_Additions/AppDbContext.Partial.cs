using Microsoft.EntityFrameworkCore;
using MyTrader.Domain.Entities;
using System.Text.Json;

namespace MyTrader.Infrastructure;

public partial class AppDbContext : DbContext
{
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<StrategyTemplate> StrategyTemplates => Set<StrategyTemplate>();
    public DbSet<UserStrategy> UserStrategies => Set<UserStrategy>();
    public DbSet<Symbol> Symbols => Set<Symbol>();
    public DbSet<Candle> Candles => Set<Candle>();
    public DbSet<Backtest> Backtests => Set<Backtest>();
    public DbSet<BacktestTrade> BacktestTrades => Set<BacktestTrade>();
    public DbSet<BacktestMetric> BacktestMetrics => Set<BacktestMetric>();
    public DbSet<UserPortfolio> UserPortfolios => Set<UserPortfolio>();
    public DbSet<UserPosition> UserPositions => Set<UserPosition>();
    public DbSet<UserTradingActivity> UserTradingActivities => Set<UserTradingActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Candle>()
            .HasKey(c => new { c.SymbolId, c.Timeframe, c.Timestamp });

        modelBuilder.Entity<StrategyTemplate>()
            .HasIndex(t => new { t.Name, t.Version })
            .IsUnique();

        modelBuilder.Entity<StrategyTemplate>()
            .HasIndex(t => t.Parameters)
            .HasMethod("gin");

        modelBuilder.Entity<UserStrategy>()
            .HasIndex(u => u.Parameters)
            .HasMethod("gin");

        modelBuilder.Entity<UserSession>()
            .HasIndex(s => new { s.UserId })
            .HasFilter(""revoked_at" IS NULL AND "expires_at" > NOW()")
            .HasDatabaseName("ix_sessions_active");

        modelBuilder.Entity<UserSession>()
            .HasIndex(s => new { s.UserId, s.JwtId })
            .IsUnique()
            .HasDatabaseName("ux_sessions_user_jwt");

        modelBuilder.Entity<UserSession>()
            .HasIndex(s => s.TokenFamilyId)
            .HasDatabaseName("ix_sessions_family");

        modelBuilder.Entity<Symbol>()
            .HasIndex(s => new { s.Ticker, s.Venue })
            .IsUnique();

        modelBuilder.Entity<Backtest>()
            .HasIndex(b => new { b.UserId, b.Status })
            .HasDatabaseName("ix_backtests_user_status");

        modelBuilder.Entity<Backtest>()
            .HasIndex(b => new { b.StrategyId, b.Symbol, b.Timeframe, b.DateRangeStart, b.DateRangeEnd, b.ConfigHash })
            .IsUnique()
            .HasDatabaseName("ux_backtests_dedup");

        modelBuilder.Entity<BacktestTrade>()
            .HasIndex(t => new { t.BacktestId, t.Timestamp })
            .HasDatabaseName("ix_trades_backtest_ts");

        modelBuilder.Entity<Candle>()
            .HasIndex(c => c.Timestamp)
            .HasMethod("brin")
            .HasDatabaseName("brin_candles_ts");
    }
}
