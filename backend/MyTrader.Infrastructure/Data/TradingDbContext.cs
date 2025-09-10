using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Models;
using MyTrader.Core.Data;

namespace MyTrader.Infrastructure.Data;

public class TradingDbContext : DbContext, ITradingDbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<TempRegistration> TempRegistrations { get; set; }
    public DbSet<MarketData> MarketData { get; set; }
    public DbSet<Strategy> Strategies { get; set; }
    public DbSet<Signal> Signals { get; set; }
    public DbSet<IndicatorConfig> IndicatorConfigs { get; set; }
    public DbSet<IndicatorValues> IndicatorValues { get; set; }
    public DbSet<BacktestResults> BacktestResults { get; set; }
    public DbSet<TradeHistory> TradeHistory { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<Symbol> Symbols { get; set; }
    public DbSet<UserStrategy> UserStrategies { get; set; }
    public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; }
    public DbSet<Candle> Candles { get; set; }
    public DbSet<BacktestQueue> BacktestQueue { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<StrategyPerformance> StrategyPerformances { get; set; }
    public DbSet<PriceAlert> PriceAlerts { get; set; }
    public DbSet<NotificationHistory> NotificationHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.TelegramId).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // UserSession configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.Property(e => e.SessionToken).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Sessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailVerification configuration
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.ToTable("email_verifications");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.VerificationCode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // TempRegistration configuration
        modelBuilder.Entity<TempRegistration>(entity =>
        {
            entity.ToTable("temp_registrations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // MarketData configuration
        modelBuilder.Entity<MarketData>(entity =>
        {
            entity.ToTable("market_data");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Symbol, e.Timeframe, e.Timestamp }).IsUnique();
            entity.Property(e => e.Symbol).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Timeframe).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.Property(e => e.Volume).HasPrecision(18, 8);
        });

        // Strategy configuration
        modelBuilder.Entity<Strategy>(entity =>
        {
            entity.ToTable("strategies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Symbol).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Timeframe).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Parameters).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Strategies)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Signal configuration
        modelBuilder.Entity<Signal>(entity =>
        {
            entity.ToTable("signals");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SignalType).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.Rsi).HasPrecision(10, 4);
            entity.Property(e => e.Macd).HasPrecision(18, 8);
            entity.Property(e => e.BollingerBandUpper).HasPrecision(18, 8);
            entity.Property(e => e.BollingerBandLower).HasPrecision(18, 8);
            entity.Property(e => e.BollingerPosition).HasMaxLength(20);
            entity.Property(e => e.AdditionalIndicators).HasColumnType("jsonb");
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.Strategy)
                  .WithMany(e => e.Signals)
                  .HasForeignKey(e => e.StrategyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // IndicatorConfig configuration
        modelBuilder.Entity<IndicatorConfig>(entity =>
        {
            entity.ToTable("indicator_configs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.BollingerStdDev).HasPrecision(10, 4);
            entity.Property(e => e.BollingerTouchTolerance).HasPrecision(10, 6);
            entity.Property(e => e.RsiBuyMax).HasPrecision(10, 4);
            entity.Property(e => e.RsiSellMin).HasPrecision(10, 4);
            entity.Property(e => e.AtrStopMultiplier).HasPrecision(10, 4);
            entity.Property(e => e.AtrTrailMultiplier).HasPrecision(10, 4);
            entity.Property(e => e.VolumeMultiplier).HasPrecision(10, 4);
            entity.Property(e => e.SlippagePercentage).HasPrecision(10, 6);
            entity.Property(e => e.FeePercentage).HasPrecision(10, 6);
            entity.Property(e => e.MaxPositionSize).HasPrecision(10, 4);
            entity.Property(e => e.StopLossPercentage).HasPrecision(10, 4);
            entity.Property(e => e.TakeProfitPercentage).HasPrecision(10, 4);
            entity.Property(e => e.CustomIndicators).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.IndicatorConfigs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // IndicatorValues configuration
        modelBuilder.Entity<IndicatorValues>(entity =>
        {
            entity.ToTable("indicator_values");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SymbolId, e.Timeframe, e.Timestamp }).IsUnique();
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Timeframe).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.Property(e => e.Volume).HasPrecision(18, 8);
            entity.Property(e => e.CustomIndicators).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // BacktestResults configuration
        modelBuilder.Entity<BacktestResults>(entity =>
        {
            entity.ToTable("backtest_results");
            entity.HasKey(e => e.Id);
            // Symbol is a navigation via SymbolId
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Timeframe).HasMaxLength(10).IsRequired();
            entity.Property(e => e.TotalReturn).HasPrecision(18, 8);
            entity.Property(e => e.TotalReturnPercentage).HasPrecision(10, 4);
            entity.Property(e => e.AnnualizedReturn).HasPrecision(10, 4);
            entity.Property(e => e.MaxDrawdown).HasPrecision(18, 8);
            entity.Property(e => e.MaxDrawdownPercentage).HasPrecision(10, 4);
            entity.Property(e => e.SharpeRatio).HasPrecision(10, 4);
            entity.Property(e => e.StartingCapital).HasPrecision(18, 8);
            entity.Property(e => e.EndingCapital).HasPrecision(18, 8);
            entity.Property(e => e.DetailedResults).HasColumnType("jsonb");
            entity.Property(e => e.StrategyConfig).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.BacktestResults)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Strategy)
                  .WithMany(e => e.BacktestResults)
                  .HasForeignKey(e => e.StrategyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TradeHistory configuration
        modelBuilder.Entity<TradeHistory>(entity =>
        {
            entity.ToTable("trade_history");
            entity.HasKey(e => e.Id);
            // Symbol is a navigation via SymbolId
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EntryPrice).HasPrecision(18, 8);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.EntryValue).HasPrecision(18, 8);
            entity.Property(e => e.ExitPrice).HasPrecision(18, 8);
            entity.Property(e => e.ExitValue).HasPrecision(18, 8);
            entity.Property(e => e.RealizedPnl).HasPrecision(18, 8);
            entity.Property(e => e.RealizedPnlPercentage).HasPrecision(10, 4);
            entity.Property(e => e.TradeContext).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.TradeHistory)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Strategy)
                  .WithMany(e => e.TradeHistory)
                  .HasForeignKey(e => e.StrategyId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(e => e.BacktestResults)
                  .WithMany(e => e.TradeHistory)
                  .HasForeignKey(e => e.BacktestResultsId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PasswordReset configuration
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.ToTable("password_resets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(6).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.PasswordResets)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Updated Strategy configuration with new relationships
        modelBuilder.Entity<Strategy>(entity =>
        {
            entity.Property(e => e.EntryRules).HasColumnType("jsonb");
            entity.Property(e => e.ExitRules).HasColumnType("jsonb");
            entity.Property(e => e.InitialCapital).HasPrecision(18, 8);
            entity.Property(e => e.MaxPositionSize).HasPrecision(10, 4);
            entity.Property(e => e.TotalReturn).HasPrecision(10, 4);
            entity.Property(e => e.WinRate).HasPrecision(10, 4);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasOne(e => e.IndicatorConfig)
                  .WithMany(e => e.Strategies)
                  .HasForeignKey(e => e.IndicatorConfigId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Updated User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Preferences).HasColumnType("jsonb");
            entity.Property(e => e.DefaultInitialCapital).HasPrecision(18, 8);
            entity.Property(e => e.DefaultRiskPercentage).HasPrecision(10, 4);
            entity.Property(e => e.Plan).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // UserNotificationPreferences configuration
        modelBuilder.Entity<UserNotificationPreferences>(entity =>
        {
            entity.ToTable("user_notification_preferences");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.AlertMethods).HasColumnType("jsonb");
            entity.Property(e => e.QuietHoursDays).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Candles configuration
        modelBuilder.Entity<Candle>(entity =>
        {
            entity.ToTable("candles");
            entity.HasKey(e => e.Id);
            // FK to symbols
            entity.Property(e => e.SymbolId).IsRequired();
            entity.Property(e => e.Timeframe).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.Property(e => e.Volume).HasPrecision(38, 18);
            // Map OpenTime -> existing column name (case-sensitive in this schema)
            entity.Property(e => e.OpenTime).HasColumnName("Timestamp");

            entity.HasIndex(e => e.OpenTime).HasDatabaseName("brin_candles_ts");
            entity.HasIndex(e => new { e.SymbolId, e.Timeframe, e.OpenTime })
                  .IsUnique()
                  .HasDatabaseName("ux_candles_symbol_tf_ts");
        });

        // BacktestQueue configuration
        modelBuilder.Entity<BacktestQueue>(entity =>
        {
            entity.ToTable("backtest_queue");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Queued");
            entity.Property(e => e.TriggerType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.Parameters).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt });
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Strategy)
                  .WithMany()
                  .HasForeignKey(e => e.StrategyId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
