using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
    public DbSet<UserPortfolio> UserPortfolios { get; set; }
    public DbSet<PortfolioPosition> PortfolioPositions { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    // Multi-asset support entities
    public DbSet<AssetClass> AssetClasses { get; set; }
    public DbSet<Market> Markets { get; set; }
    public DbSet<TradingSession> TradingSessions { get; set; }
    public DbSet<DataProvider> DataProviders { get; set; }
    public DbSet<UserDashboardPreferences> UserDashboardPreferences { get; set; }

    // Historical market data entities
    public DbSet<HistoricalMarketData> HistoricalMarketData { get; set; }
    public DbSet<MarketDataSummary> MarketDataSummaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserStrategy configuration - MUST come FIRST to override any naming conventions
        modelBuilder.Entity<UserStrategy>(entity =>
        {
            entity.ToTable("user_strategies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);

            // Configure JsonDocument properties - ignore for in-memory database
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                // For in-memory database, ignore JsonDocument properties to avoid conversion issues
                entity.Ignore(e => e.Parameters);
                entity.Ignore(e => e.CustomEntryRules);
                entity.Ignore(e => e.CustomExitRules);
                entity.Ignore(e => e.CustomRiskManagement);
                entity.Ignore(e => e.LastBacktestResults);
                entity.Ignore(e => e.PerformanceStats);
            }
            else
            {
                entity.Property(e => e.Parameters).HasColumnName("parameters").HasColumnType("jsonb");
                entity.Property(e => e.CustomEntryRules).HasColumnName("custom_entry_rules").HasColumnType("jsonb");
                entity.Property(e => e.CustomExitRules).HasColumnName("custom_exit_rules").HasColumnType("jsonb");
                entity.Property(e => e.CustomRiskManagement).HasColumnName("custom_risk_management").HasColumnType("jsonb");
                entity.Property(e => e.LastBacktestResults).HasColumnName("last_backtest_results").HasColumnType("jsonb");
                entity.Property(e => e.PerformanceStats).HasColumnName("performance_stats").HasColumnType("jsonb");
            }

            entity.Property(e => e.TargetSymbols).HasColumnName("target_symbols").HasMaxLength(1000);
            entity.Property(e => e.Timeframe).HasColumnName("timeframe").HasMaxLength(10).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsCustom).HasColumnName("is_custom");
            entity.Property(e => e.IsFavorite).HasColumnName("is_favorite");
            entity.Property(e => e.InitialCapital).HasColumnName("initial_capital").HasPrecision(18, 8);
            entity.Property(e => e.MaxPositionSizePercent).HasColumnName("max_position_size_percent").HasPrecision(5, 2);
            entity.Property(e => e.TemplateVersion).HasColumnName("template_version").HasMaxLength(20);
            entity.Property(e => e.LastBacktestAt).HasColumnName("last_backtest_at");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
            entity.Property(e => e.Tags).HasColumnName("tags").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        // CRITICAL: Disable any default naming conventions AFTER our explicit configuration
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Skip UserStrategy as we've already configured it explicitly
            if (entity.ClrType == typeof(UserStrategy))
                continue;
                
            // Ensure table names are not converted for other entities
            if (entity.GetTableName() != null)
            {
                // Keep table names as defined
            }
            
            // Ensure property names match exactly what we specify for other entities
            foreach (var property in entity.GetProperties())
            {
                if (property.GetColumnName() == null)
                {
                    // For properties without explicit column mapping, use property name as-is
                    property.SetColumnName(property.Name);
                }
            }
        }

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
            entity.Property(e => e.SessionToken).IsRequired();
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
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
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

        // UserPortfolio configuration
        modelBuilder.Entity<UserPortfolio>(entity =>
        {
            entity.ToTable("user_portfolios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.BaseCurrency).HasColumnName("base_ccy").HasMaxLength(12).IsRequired();
            entity.Property(e => e.InitialCapital).HasColumnName("initial_capital").HasPrecision(18, 8);
            entity.Property(e => e.CurrentValue).HasColumnName("current_value").HasPrecision(18, 8);
            entity.Property(e => e.CashBalance).HasColumnName("cash_balance").HasPrecision(18, 8);
            entity.Property(e => e.TotalPnL).HasColumnName("total_pnl").HasPrecision(18, 8);
            entity.Property(e => e.DailyPnL).HasColumnName("daily_pnl").HasPrecision(18, 8);
            entity.Property(e => e.TotalReturnPercent).HasColumnName("total_return_percent").HasPrecision(10, 4);
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsDefault });
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PortfolioPosition configuration
        modelBuilder.Entity<PortfolioPosition>(entity =>
        {
            entity.ToTable("portfolio_positions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.AveragePrice).HasPrecision(18, 8);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 8);
            entity.Property(e => e.MarketValue).HasPrecision(18, 8);
            entity.Property(e => e.UnrealizedPnL).HasPrecision(18, 8);
            entity.Property(e => e.UnrealizedPnLPercent).HasPrecision(10, 4);
            entity.Property(e => e.RealizedPnL).HasPrecision(18, 8);
            entity.Property(e => e.CostBasis).HasPrecision(18, 8);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.PortfolioId);
            entity.HasIndex(e => new { e.PortfolioId, e.SymbolId }).IsUnique();
            
            entity.HasOne(e => e.Portfolio)
                  .WithMany(p => p.Positions)
                  .HasForeignKey(e => e.PortfolioId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionType).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Side).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 8);
            entity.Property(e => e.Fee).HasPrecision(18, 8);
            entity.Property(e => e.Currency).HasMaxLength(12).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OrderId).HasMaxLength(100);
            entity.Property(e => e.ExecutionId).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.PortfolioId);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => new { e.PortfolioId, e.ExecutedAt });
            entity.HasIndex(e => new { e.PortfolioId, e.SymbolId, e.ExecutedAt });
            
            entity.HasOne(e => e.Portfolio)
                  .WithMany(p => p.Transactions)
                  .HasForeignKey(e => e.PortfolioId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


        // AssetClass configuration
        modelBuilder.Entity<AssetClass>(entity =>
        {
            entity.ToTable("asset_classes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.DisplayOrder });

            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NameTurkish).HasColumnName("name_tr").HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.PrimaryCurrency).HasColumnName("primary_currency").HasMaxLength(12).IsRequired();
            entity.Property(e => e.DefaultPricePrecision).HasColumnName("default_price_precision");
            entity.Property(e => e.DefaultQuantityPrecision).HasColumnName("default_quantity_precision");
            entity.Property(e => e.Supports24x7Trading).HasColumnName("supports_24_7_trading");
            entity.Property(e => e.SupportsFractional).HasColumnName("supports_fractional");
            entity.Property(e => e.MinTradeAmount).HasColumnName("min_trade_amount").HasPrecision(18, 8);
            entity.Property(e => e.Configuration).HasColumnName("configuration").HasColumnType("jsonb");
            entity.Property(e => e.RegulatoryClass).HasColumnName("regulatory_class").HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        // Market configuration
        modelBuilder.Entity<Market>(entity =>
        {
            entity.ToTable("markets");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.DisplayOrder });
            entity.HasIndex(e => e.AssetClassId);

            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameTurkish).HasColumnName("name_tr").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Timezone).HasColumnName("timezone").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PrimaryCurrency).HasColumnName("primary_currency").HasMaxLength(12).IsRequired();
            entity.Property(e => e.MarketMaker).HasColumnName("market_maker").HasMaxLength(50);
            entity.Property(e => e.ApiBaseUrl).HasColumnName("api_base_url").HasMaxLength(200);
            entity.Property(e => e.WebSocketUrl).HasColumnName("websocket_url").HasMaxLength(200);
            entity.Property(e => e.DefaultCommissionRate).HasColumnName("default_commission_rate").HasPrecision(10, 6);
            entity.Property(e => e.MinCommission).HasColumnName("min_commission").HasPrecision(18, 8);
            entity.Property(e => e.MarketConfig).HasColumnName("market_config").HasColumnType("jsonb");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.StatusUpdatedAt).HasColumnName("status_updated_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.HasRealtimeData).HasColumnName("has_realtime_data");
            entity.Property(e => e.DataDelayMinutes).HasColumnName("data_delay_minutes");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.AssetClass)
                  .WithMany(a => a.Markets)
                  .HasForeignKey(e => e.AssetClassId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TradingSession configuration
        modelBuilder.Entity<TradingSession>(entity =>
        {
            entity.ToTable("trading_sessions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MarketId);
            entity.HasIndex(e => new { e.MarketId, e.DayOfWeek, e.IsPrimary });

            entity.Property(e => e.SessionName).HasColumnName("session_name").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SessionType).HasColumnName("session_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.SpansMidnight).HasColumnName("spans_midnight");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.IsTradingEnabled).HasColumnName("is_trading_enabled");
            entity.Property(e => e.VolumeMultiplier).HasColumnName("volume_multiplier").HasPrecision(10, 4);
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.SessionConfig).HasColumnName("session_config").HasColumnType("jsonb");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Market)
                  .WithMany(m => m.TradingSessions)
                  .HasForeignKey(e => e.MarketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DataProvider configuration
        modelBuilder.Entity<DataProvider>(entity =>
        {
            entity.ToTable("data_providers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.MarketId);
            entity.HasIndex(e => new { e.MarketId, e.IsActive, e.Priority });

            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(e => e.ProviderType).HasColumnName("provider_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.FeedType).HasColumnName("feed_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.EndpointUrl).HasColumnName("endpoint_url").HasMaxLength(500);
            entity.Property(e => e.WebSocketUrl).HasColumnName("websocket_url").HasMaxLength(500);
            entity.Property(e => e.BackupEndpointUrl).HasColumnName("backup_endpoint_url").HasMaxLength(500);
            entity.Property(e => e.AuthType).HasColumnName("auth_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ApiKey).HasColumnName("api_key").HasMaxLength(500);
            entity.Property(e => e.ApiSecret).HasColumnName("api_secret").HasMaxLength(500);
            entity.Property(e => e.AuthConfig).HasColumnName("auth_config").HasColumnType("jsonb");
            entity.Property(e => e.RateLimitPerMinute).HasColumnName("rate_limit_per_minute");
            entity.Property(e => e.TimeoutSeconds).HasColumnName("timeout_seconds");
            entity.Property(e => e.MaxRetries).HasColumnName("max_retries");
            entity.Property(e => e.RetryDelayMs).HasColumnName("retry_delay_ms");
            entity.Property(e => e.DataDelayMinutes).HasColumnName("data_delay_minutes");
            entity.Property(e => e.SupportedDataTypes).HasColumnName("supported_data_types").HasColumnType("jsonb");
            entity.Property(e => e.ProviderConfig).HasColumnName("provider_config").HasColumnType("jsonb");
            entity.Property(e => e.ConnectionStatus).HasColumnName("connection_status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.LastConnectedAt).HasColumnName("last_connected_at");
            entity.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(1000);
            entity.Property(e => e.ErrorCountHourly).HasColumnName("error_count_hourly");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.CostPer1kCalls).HasColumnName("cost_per_1k_calls").HasPrecision(10, 6);
            entity.Property(e => e.MonthlyLimit).HasColumnName("monthly_limit");
            entity.Property(e => e.MonthlyUsage).HasColumnName("monthly_usage");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Market)
                  .WithMany(m => m.DataProviders)
                  .HasForeignKey(e => e.MarketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Update Symbol configuration to include new relationships
        modelBuilder.Entity<Symbol>(entity =>
        {
            // Add new foreign key relationships
            entity.HasOne(e => e.AssetClassEntity)
                  .WithMany(a => a.Symbols)
                  .HasForeignKey(e => e.AssetClassId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Market)
                  .WithMany(m => m.Symbols)
                  .HasForeignKey(e => e.MarketId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Add new indexes for performance
            entity.HasIndex(e => new { e.Ticker, e.Venue });
            entity.HasIndex(e => new { e.AssetClass, e.IsActive, e.IsPopular });
            entity.HasIndex(e => new { e.Sector, e.Industry });
            entity.HasIndex(e => e.Volume24h);
            entity.HasIndex(e => e.AssetClassId);
            entity.HasIndex(e => e.MarketId);
        });

        // === HISTORICAL MARKET DATA CONFIGURATION ===
        ConfigureHistoricalMarketData(modelBuilder);
        ConfigureMarketDataSummaries(modelBuilder);
    }

    /// <summary>
    /// Configure HistoricalMarketData entity with comprehensive mappings and indexes
    /// </summary>
    private void ConfigureHistoricalMarketData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistoricalMarketData>(entity =>
        {
            entity.ToTable("historical_market_data");
            entity.HasKey(e => new { e.Id, e.TradeDate }); // Composite key for partitioning

            // === BASIC PROPERTIES ===
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.SymbolId).HasColumnName("symbol_id").IsRequired();
            entity.Property(e => e.SymbolTicker).HasColumnName("symbol_ticker").HasMaxLength(50).IsRequired();
            entity.Property(e => e.DataSource).HasColumnName("data_source").HasMaxLength(20).IsRequired();
            entity.Property(e => e.MarketCode).HasColumnName("market_code").HasMaxLength(20);
            entity.Property(e => e.TradeDate).HasColumnName("trade_date").IsRequired();
            entity.Property(e => e.Timeframe).HasColumnName("timeframe").HasMaxLength(10).IsRequired().HasDefaultValue("DAILY");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");

            // === STANDARD OHLCV DATA ===
            entity.Property(e => e.OpenPrice).HasColumnName("open_price").HasPrecision(18, 8);
            entity.Property(e => e.HighPrice).HasColumnName("high_price").HasPrecision(18, 8);
            entity.Property(e => e.LowPrice).HasColumnName("low_price").HasPrecision(18, 8);
            entity.Property(e => e.ClosePrice).HasColumnName("close_price").HasPrecision(18, 8);
            entity.Property(e => e.AdjustedClosePrice).HasColumnName("adjusted_close_price").HasPrecision(18, 8);
            entity.Property(e => e.Volume).HasColumnName("volume").HasPrecision(38, 18);
            entity.Property(e => e.VWAP).HasColumnName("vwap").HasPrecision(18, 8);

            // === BIST SPECIFIC DATA ===
            entity.Property(e => e.BistCode).HasColumnName("bist_code").HasMaxLength(20);
            entity.Property(e => e.PreviousClose).HasColumnName("previous_close").HasPrecision(18, 8);
            entity.Property(e => e.PriceChange).HasColumnName("price_change").HasPrecision(18, 8);
            entity.Property(e => e.PriceChangePercent).HasColumnName("price_change_percent").HasPrecision(10, 4);
            entity.Property(e => e.TradingValue).HasColumnName("trading_value").HasPrecision(38, 18);
            entity.Property(e => e.TransactionCount).HasColumnName("transaction_count");
            entity.Property(e => e.MarketCap).HasColumnName("market_cap").HasPrecision(38, 18);
            entity.Property(e => e.FreeFloatMarketCap).HasColumnName("free_float_market_cap").HasPrecision(38, 18);
            entity.Property(e => e.SharesOutstanding).HasColumnName("shares_outstanding").HasPrecision(38, 18);
            entity.Property(e => e.FreeFloatShares).HasColumnName("free_float_shares").HasPrecision(38, 18);

            // === INDEX AND CURRENCY DATA ===
            entity.Property(e => e.IndexValue).HasColumnName("index_value").HasPrecision(18, 8);
            entity.Property(e => e.IndexChangePercent).HasColumnName("index_change_percent").HasPrecision(10, 4);
            entity.Property(e => e.UsdTryRate).HasColumnName("usd_try_rate").HasPrecision(18, 8);
            entity.Property(e => e.EurTryRate).HasColumnName("eur_try_rate").HasPrecision(18, 8);

            // === TECHNICAL INDICATORS ===
            entity.Property(e => e.RSI).HasColumnName("rsi").HasPrecision(10, 4);
            entity.Property(e => e.MACD).HasColumnName("macd").HasPrecision(18, 8);
            entity.Property(e => e.MACDSignal).HasColumnName("macd_signal").HasPrecision(18, 8);
            entity.Property(e => e.BollingerUpper).HasColumnName("bollinger_upper").HasPrecision(18, 8);
            entity.Property(e => e.BollingerLower).HasColumnName("bollinger_lower").HasPrecision(18, 8);
            entity.Property(e => e.SMA20).HasColumnName("sma_20").HasPrecision(18, 8);
            entity.Property(e => e.SMA50).HasColumnName("sma_50").HasPrecision(18, 8);
            entity.Property(e => e.SMA200).HasColumnName("sma_200").HasPrecision(18, 8);

            // === METADATA ===
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(12).HasDefaultValue("USD");
            entity.Property(e => e.DataQualityScore).HasColumnName("data_quality_score");

            // Configure JsonDocument properties - ignore for in-memory database
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                entity.Ignore(e => e.ExtendedData);
                entity.Ignore(e => e.SourceMetadata);
            }
            else
            {
                entity.Property(e => e.ExtendedData).HasColumnName("extended_data").HasColumnType("jsonb");
                entity.Property(e => e.SourceMetadata).HasColumnName("source_metadata").HasColumnType("jsonb");
            }

            entity.Property(e => e.DataFlags).HasColumnName("data_flags").HasDefaultValue(0);
            entity.Property(e => e.SourcePriority).HasColumnName("source_priority").HasDefaultValue(10);

            // === AUDIT FIELDS ===
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DataCollectedAt).HasColumnName("data_collected_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // === RELATIONSHIPS ===
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Cascade);

            // === INDEXES FOR PERFORMANCE ===
            // Primary time-series index
            entity.HasIndex(e => new { e.SymbolTicker, e.Timeframe, e.TradeDate })
                  .HasDatabaseName("idx_historical_market_data_primary")
                  .IsUnique();

            // Symbol-based queries
            entity.HasIndex(e => new { e.SymbolId, e.TradeDate, e.Timeframe })
                  .HasDatabaseName("idx_historical_market_data_symbol_date");

            // Date partitioning support
            entity.HasIndex(e => new { e.TradeDate, e.DataSource, e.SourcePriority })
                  .HasDatabaseName("idx_historical_market_data_date_source");

            // Data quality index
            entity.HasIndex(e => new { e.SymbolTicker, e.TradeDate, e.Timeframe, e.DataSource, e.SourcePriority })
                  .HasDatabaseName("idx_historical_market_data_dedup");

            // BIST specific index
            entity.HasIndex(e => new { e.BistCode, e.TradeDate })
                  .HasDatabaseName("idx_historical_market_data_bist")
                  .HasFilter("bist_code IS NOT NULL");

            // Volume analysis index
            entity.HasIndex(e => new { e.TradeDate, e.Volume })
                  .HasDatabaseName("idx_historical_market_data_volume")
                  .IsDescending(true, true)
                  .HasFilter("volume IS NOT NULL");

            // Technical indicators index
            entity.HasIndex(e => new { e.TradeDate, e.RSI, e.MACD })
                  .HasDatabaseName("idx_historical_market_data_technical")
                  .HasFilter("rsi IS NOT NULL OR macd IS NOT NULL");

            // Intraday data index
            entity.HasIndex(e => new { e.SymbolTicker, e.Timestamp })
                  .HasDatabaseName("idx_historical_market_data_intraday")
                  .IsDescending(false, true)
                  .HasFilter("timestamp IS NOT NULL");
        });
    }

    /// <summary>
    /// Configure MarketDataSummary entity for pre-aggregated analytics
    /// </summary>
    private void ConfigureMarketDataSummaries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketDataSummary>(entity =>
        {
            entity.ToTable("market_data_summaries");
            entity.HasKey(e => new { e.Id, e.PeriodStart }); // Composite key for partitioning

            // === BASIC PROPERTIES ===
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.SymbolId).HasColumnName("symbol_id").IsRequired();
            entity.Property(e => e.SymbolTicker).HasColumnName("symbol_ticker").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PeriodType).HasColumnName("period_type").HasMaxLength(10).IsRequired();
            entity.Property(e => e.PeriodStart).HasColumnName("period_start").IsRequired();
            entity.Property(e => e.PeriodEnd).HasColumnName("period_end").IsRequired();
            entity.Property(e => e.TradingDays).HasColumnName("trading_days");

            // === PRICE STATISTICS ===
            entity.Property(e => e.PeriodOpen).HasColumnName("period_open").HasPrecision(18, 8);
            entity.Property(e => e.PeriodClose).HasColumnName("period_close").HasPrecision(18, 8);
            entity.Property(e => e.PeriodHigh).HasColumnName("period_high").HasPrecision(18, 8);
            entity.Property(e => e.PeriodLow).HasColumnName("period_low").HasPrecision(18, 8);
            entity.Property(e => e.PeriodVWAP).HasColumnName("period_vwap").HasPrecision(18, 8);
            entity.Property(e => e.TotalReturnPercent).HasColumnName("total_return_percent").HasPrecision(10, 4);
            entity.Property(e => e.AvgDailyReturnPercent).HasColumnName("avg_daily_return_percent").HasPrecision(10, 4);
            entity.Property(e => e.Volatility).HasColumnName("volatility").HasPrecision(10, 6);
            entity.Property(e => e.AnnualizedVolatility).HasColumnName("annualized_volatility").HasPrecision(10, 6);
            entity.Property(e => e.SharpeRatio).HasColumnName("sharpe_ratio").HasPrecision(10, 4);
            entity.Property(e => e.MaxDrawdownPercent).HasColumnName("max_drawdown_percent").HasPrecision(10, 4);
            entity.Property(e => e.Beta).HasColumnName("beta").HasPrecision(10, 4);

            // === VOLUME STATISTICS ===
            entity.Property(e => e.TotalVolume).HasColumnName("total_volume").HasPrecision(38, 18);
            entity.Property(e => e.AvgDailyVolume).HasColumnName("avg_daily_volume").HasPrecision(38, 18);
            entity.Property(e => e.TotalTradingValue).HasColumnName("total_trading_value").HasPrecision(38, 18);
            entity.Property(e => e.AvgDailyTradingValue).HasColumnName("avg_daily_trading_value").HasPrecision(38, 18);
            entity.Property(e => e.TotalTransactions).HasColumnName("total_transactions");
            entity.Property(e => e.AvgDailyTransactions).HasColumnName("avg_daily_transactions");

            // === PRICE LEVELS ===
            entity.Property(e => e.SupportLevel).HasColumnName("support_level").HasPrecision(18, 8);
            entity.Property(e => e.ResistanceLevel).HasColumnName("resistance_level").HasPrecision(18, 8);
            entity.Property(e => e.Week52High).HasColumnName("week_52_high").HasPrecision(18, 8);
            entity.Property(e => e.Week52Low).HasColumnName("week_52_low").HasPrecision(18, 8);

            // === TECHNICAL INDICATORS SUMMARY ===
            entity.Property(e => e.AvgRSI).HasColumnName("avg_rsi").HasPrecision(10, 4);
            entity.Property(e => e.AvgMACD).HasColumnName("avg_macd").HasPrecision(18, 8);
            entity.Property(e => e.DaysAboveSMA20Percent).HasColumnName("days_above_sma20_percent").HasPrecision(10, 2);
            entity.Property(e => e.DaysAboveSMA50Percent).HasColumnName("days_above_sma50_percent").HasPrecision(10, 2);

            // === MARKET COMPARISON ===
            entity.Property(e => e.VsMarketPercent).HasColumnName("vs_market_percent").HasPrecision(10, 4);
            entity.Property(e => e.MarketCorrelation).HasColumnName("market_correlation").HasPrecision(10, 6);

            // === RANKING METRICS ===
            entity.Property(e => e.PerformancePercentile).HasColumnName("performance_percentile");
            entity.Property(e => e.VolumePercentile).HasColumnName("volume_percentile");
            entity.Property(e => e.QualityScore).HasColumnName("quality_score").HasDefaultValue(100);

            // === AUDIT FIELDS ===
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CalculatedAt).HasColumnName("calculated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // === RELATIONSHIPS ===
            entity.HasOne(e => e.Symbol)
                  .WithMany()
                  .HasForeignKey(e => e.SymbolId)
                  .OnDelete(DeleteBehavior.Cascade);

            // === INDEXES ===
            // Primary lookup index
            entity.HasIndex(e => new { e.SymbolTicker, e.PeriodType, e.PeriodStart })
                  .HasDatabaseName("idx_market_data_summaries_primary")
                  .IsUnique()
                  .IsDescending(false, false, true);

            // Performance ranking
            entity.HasIndex(e => new { e.PeriodType, e.PeriodStart, e.TotalReturnPercent })
                  .HasDatabaseName("idx_market_data_summaries_performance")
                  .IsDescending(false, true, true);

            // Volume ranking
            entity.HasIndex(e => new { e.PeriodType, e.PeriodStart, e.AvgDailyVolume })
                  .HasDatabaseName("idx_market_data_summaries_volume")
                  .IsDescending(false, true, true);

            // Quality filtering
            entity.HasIndex(e => new { e.PeriodType, e.QualityScore, e.PeriodStart })
                  .HasDatabaseName("idx_market_data_summaries_quality")
                  .IsDescending(false, true, true)
                  .HasFilter("quality_score >= 80");
        });
    }
}
