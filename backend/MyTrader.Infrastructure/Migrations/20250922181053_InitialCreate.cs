using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asset_classes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_tr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    primary_currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    default_price_precision = table.Column<int>(type: "integer", nullable: false),
                    default_quantity_precision = table.Column<int>(type: "integer", nullable: false),
                    supports_24_7_trading = table.Column<bool>(type: "boolean", nullable: false),
                    supports_fractional = table.Column<bool>(type: "boolean", nullable: false),
                    min_trade_amount = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    configuration = table.Column<string>(type: "jsonb", nullable: true),
                    regulatory_class = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BacktestConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    InitialBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StrategyParameters = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "candles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Vwap = table.Column<decimal>(type: "numeric", nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "market_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "temp_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temp_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    parameters = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    custom_entry_rules = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    custom_exit_rules = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    custom_risk_management = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    target_symbols = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_custom = table.Column<bool>(type: "boolean", nullable: false),
                    is_favorite = table.Column<bool>(type: "boolean", nullable: false),
                    initial_capital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    max_position_size_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    template_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    last_backtest_results = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    last_backtest_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    performance_stats = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_strategies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TelegramId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Preferences = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultInitialCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    DefaultRiskPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlanExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "markets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AssetClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    primary_currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    market_maker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    api_base_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    websocket_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    default_commission_rate = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: true),
                    min_commission = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    market_config = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    has_realtime_data = table.Column<bool>(type: "boolean", nullable: false),
                    data_delay_minutes = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_markets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_markets_asset_classes_AssetClassId",
                        column: x => x.AssetClassId,
                        principalTable: "asset_classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "indicator_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BollingerPeriod = table.Column<int>(type: "integer", nullable: false),
                    BollingerStdDev = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    BollingerTouchTolerance = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    MacdFast = table.Column<int>(type: "integer", nullable: false),
                    MacdSlow = table.Column<int>(type: "integer", nullable: false),
                    MacdSignal = table.Column<int>(type: "integer", nullable: false),
                    UseMacdFilter = table.Column<bool>(type: "boolean", nullable: false),
                    RsiPeriod = table.Column<int>(type: "integer", nullable: false),
                    UseRsiFilter = table.Column<bool>(type: "boolean", nullable: false),
                    RsiBuyMax = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    RsiSellMin = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    UseEmaTrend = table.Column<bool>(type: "boolean", nullable: false),
                    EmaTrendLength = table.Column<int>(type: "integer", nullable: false),
                    EmaTrendMode = table.Column<string>(type: "text", nullable: false),
                    UseAtr = table.Column<bool>(type: "boolean", nullable: false),
                    AtrLength = table.Column<int>(type: "integer", nullable: false),
                    AtrStopMultiplier = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    AtrTrailMultiplier = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    UseVolumeFilter = table.Column<bool>(type: "boolean", nullable: false),
                    VolumeMultiplier = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    VolumeLookbackPeriod = table.Column<int>(type: "integer", nullable: false),
                    SlippagePercentage = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    FeePercentage = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    MaxPositionSize = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    StopLossPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    TakeProfitPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomIndicators = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_indicator_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_indicator_configs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_resets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_resets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_resets_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    alert_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_price = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    percentage_change = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_triggered = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    triggered_price = table.Column<decimal>(type: "numeric(18,8)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_alerts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strategy_performance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_return = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    win_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    max_drawdown = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    sharpe_ratio = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    total_trades = table.Column<int>(type: "integer", nullable: false),
                    profitable_trades = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_performance", x => x.id);
                    table.ForeignKey(
                        name: "FK_strategy_performance_user_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalTable: "user_strategies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_strategy_performance_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    achievement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    achievement_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    earned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_achievements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnableEmailSignals = table.Column<bool>(type: "boolean", nullable: false),
                    EnableEmailTradeExecutions = table.Column<bool>(type: "boolean", nullable: false),
                    EnableEmailBacktestResults = table.Column<bool>(type: "boolean", nullable: false),
                    EnableEmailAccountUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    EnableEmailMarketing = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePushSignals = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePushTradeExecutions = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePushBacktestResults = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePushAccountUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePushMarketing = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSmsSignals = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSmsTradeExecutions = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSmsBacktestResults = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSmsAccountUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    EnableTelegramSignals = table.Column<bool>(type: "boolean", nullable: false),
                    EnableTelegramTradeExecutions = table.Column<bool>(type: "boolean", nullable: false),
                    EnableTelegramBacktestResults = table.Column<bool>(type: "boolean", nullable: false),
                    EnableTelegramAccountUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    MinSignalConfidence = table.Column<decimal>(type: "numeric", nullable: false),
                    PreferredTradingHours = table.Column<string>(type: "text", nullable: true),
                    FilterBySymbols = table.Column<string>(type: "text", nullable: true),
                    FilterByStrategies = table.Column<string>(type: "text", nullable: true),
                    EnableSignalAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePortfolioAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    EnableStrategyAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    EnableMarketAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    PortfolioChangeThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    AlertMethods = table.Column<string>(type: "jsonb", nullable: false),
                    QuietHoursEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    QuietHoursStart = table.Column<TimeSpan>(type: "interval", nullable: false),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "interval", nullable: false),
                    QuietHoursDays = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notification_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_portfolios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    base_ccy = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    initial_capital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    current_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    cash_balance = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    total_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    daily_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    total_return_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_portfolios", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_portfolios_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    JwtId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TokenFamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RotatedFrom = table.Column<Guid>(type: "uuid", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDevice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevice_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "data_providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    feed_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    endpoint_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    websocket_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    backup_endpoint_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    auth_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    api_secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    auth_config = table.Column<string>(type: "jsonb", nullable: true),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: true),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    retry_delay_ms = table.Column<int>(type: "integer", nullable: false),
                    data_delay_minutes = table.Column<int>(type: "integer", nullable: false),
                    supported_data_types = table.Column<string>(type: "jsonb", nullable: true),
                    provider_config = table.Column<string>(type: "jsonb", nullable: true),
                    connection_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_count_hourly = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    cost_per_1k_calls = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: true),
                    monthly_limit = table.Column<int>(type: "integer", nullable: true),
                    monthly_usage = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_data_providers_markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "symbols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    venue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    asset_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    asset_class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    market_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    quote_currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    full_name_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    display = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    isin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    is_popular = table.Column<bool>(type: "boolean", nullable: false),
                    price_precision = table.Column<int>(type: "integer", nullable: true),
                    quantity_precision = table.Column<int>(type: "integer", nullable: true),
                    tick_size = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    step_size = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    min_order_value = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    max_order_value = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    volume_24h = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    market_cap = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    current_price = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    price_change_24h = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    price_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    trading_config = table.Column<string>(type: "jsonb", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symbols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_symbols_asset_classes_asset_class_id",
                        column: x => x.asset_class_id,
                        principalTable: "asset_classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_symbols_markets_market_id",
                        column: x => x.market_id,
                        principalTable: "markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "trading_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    session_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    session_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: true),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    spans_midnight = table.Column<bool>(type: "boolean", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_trading_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    volume_multiplier = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    session_config = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trading_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trading_sessions_markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCustom = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    StrategyType = table.Column<int>(type: "integer", nullable: false),
                    InitialCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    MaxPositionSize = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    IndicatorConfigId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    EntryRules = table.Column<string>(type: "jsonb", nullable: false),
                    ExitRules = table.Column<string>(type: "jsonb", nullable: false),
                    TotalReturn = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    WinRate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    TotalTrades = table.Column<int>(type: "integer", nullable: true),
                    LastBacktestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PerformanceScore = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_strategies_indicator_configs_IndicatorConfigId",
                        column: x => x.IndicatorConfigId,
                        principalTable: "indicator_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_strategies_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_history_UserDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "UserDevice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_notification_history_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "indicator_values",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Rsi = table.Column<decimal>(type: "numeric", nullable: true),
                    RsiSma = table.Column<decimal>(type: "numeric", nullable: true),
                    Macd = table.Column<decimal>(type: "numeric", nullable: true),
                    MacdSignal = table.Column<decimal>(type: "numeric", nullable: true),
                    MacdHistogram = table.Column<decimal>(type: "numeric", nullable: true),
                    BbUpper = table.Column<decimal>(type: "numeric", nullable: true),
                    BbMiddle = table.Column<decimal>(type: "numeric", nullable: true),
                    BbLower = table.Column<decimal>(type: "numeric", nullable: true),
                    BbPosition = table.Column<decimal>(type: "numeric", nullable: true),
                    Ema9 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ema21 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ema50 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ema100 = table.Column<decimal>(type: "numeric", nullable: true),
                    Ema200 = table.Column<decimal>(type: "numeric", nullable: true),
                    Sma20 = table.Column<decimal>(type: "numeric", nullable: true),
                    Sma50 = table.Column<decimal>(type: "numeric", nullable: true),
                    Sma100 = table.Column<decimal>(type: "numeric", nullable: true),
                    Sma200 = table.Column<decimal>(type: "numeric", nullable: true),
                    Atr = table.Column<decimal>(type: "numeric", nullable: true),
                    AtrPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    VolumeAvg20 = table.Column<decimal>(type: "numeric", nullable: true),
                    VolumeRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    Vwap = table.Column<decimal>(type: "numeric", nullable: true),
                    StochK = table.Column<decimal>(type: "numeric", nullable: true),
                    StochD = table.Column<decimal>(type: "numeric", nullable: true),
                    WilliamsR = table.Column<decimal>(type: "numeric", nullable: true),
                    Cci = table.Column<decimal>(type: "numeric", nullable: true),
                    Mfi = table.Column<decimal>(type: "numeric", nullable: true),
                    RelativeVolume = table.Column<decimal>(type: "numeric", nullable: true),
                    Volatility = table.Column<decimal>(type: "numeric", nullable: true),
                    Support1 = table.Column<decimal>(type: "numeric", nullable: true),
                    Support2 = table.Column<decimal>(type: "numeric", nullable: true),
                    Resistance1 = table.Column<decimal>(type: "numeric", nullable: true),
                    Resistance2 = table.Column<decimal>(type: "numeric", nullable: true),
                    TrendDirection = table.Column<decimal>(type: "numeric", nullable: true),
                    TrendStrength = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CustomIndicators = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_indicator_values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_indicator_values_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    average_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    market_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    unrealized_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    unrealized_pnl_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    realized_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    cost_basis = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    first_purchased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_traded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_portfolio_positions_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_portfolio_positions_user_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "user_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    fee = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    execution_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transactions_user_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "user_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "backtest_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TotalReturn = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    TotalReturnPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    AnnualizedReturn = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    MaxDrawdownPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    SharpeRatio = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    WinRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalTrades = table.Column<int>(type: "integer", nullable: false),
                    WinningTrades = table.Column<int>(type: "integer", nullable: false),
                    LosingTrades = table.Column<int>(type: "integer", nullable: false),
                    StartingCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    EndingCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    DetailedResults = table.Column<string>(type: "jsonb", nullable: true),
                    StrategyConfig = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    BacktestResultsId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backtest_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_backtest_results_backtest_results_BacktestResultsId",
                        column: x => x.BacktestResultsId,
                        principalTable: "backtest_results",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_backtest_results_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_backtest_results_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_backtest_results_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Rsi = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    Macd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    BollingerBandUpper = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    BollingerBandLower = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    BollingerPosition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    AdditionalIndicators = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_signals_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_signals_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "backtest_queue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Queued"),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    Parameters = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ActualDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResultId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backtest_queue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_backtest_queue_BacktestConfiguration_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "BacktestConfiguration",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_backtest_queue_backtest_results_ResultId",
                        column: x => x.ResultId,
                        principalTable: "backtest_results",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_backtest_queue_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_backtest_queue_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_backtest_queue_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trade_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: true),
                    BacktestResultsId = table.Column<Guid>(type: "uuid", nullable: true),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeType = table.Column<int>(type: "integer", nullable: false),
                    TradeSource = table.Column<int>(type: "integer", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    EntryValue = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    EntryFee = table.Column<decimal>(type: "numeric", nullable: true),
                    ExitTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExitPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ExitValue = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ExitFee = table.Column<decimal>(type: "numeric", nullable: true),
                    RealizedPnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    RealizedPnlPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    UnrealizedPnl = table.Column<decimal>(type: "numeric", nullable: true),
                    UnrealizedPnlPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    HoldingPeriod = table.Column<TimeSpan>(type: "interval", nullable: true),
                    MaxProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxProfitPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxLossPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    EntryRsi = table.Column<decimal>(type: "numeric", nullable: true),
                    EntryMacd = table.Column<decimal>(type: "numeric", nullable: true),
                    EntryBollingerPosition = table.Column<decimal>(type: "numeric", nullable: true),
                    ExitRsi = table.Column<decimal>(type: "numeric", nullable: true),
                    ExitMacd = table.Column<decimal>(type: "numeric", nullable: true),
                    ExitBollingerPosition = table.Column<decimal>(type: "numeric", nullable: true),
                    StopLossPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    TakeProfitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    WasStopLossHit = table.Column<bool>(type: "boolean", nullable: true),
                    WasTakeProfitHit = table.Column<bool>(type: "boolean", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TradeContext = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trade_history_backtest_results_BacktestResultsId",
                        column: x => x.BacktestResultsId,
                        principalTable: "backtest_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_trade_history_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_trade_history_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trade_history_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_classes_code",
                table: "asset_classes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_classes_is_active_display_order",
                table: "asset_classes",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_backtest_queue_ConfigurationId",
                table: "backtest_queue",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_queue_ResultId",
                table: "backtest_queue",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_queue_Status_Priority_CreatedAt",
                table: "backtest_queue",
                columns: new[] { "Status", "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_backtest_queue_StrategyId",
                table: "backtest_queue",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_queue_SymbolId",
                table: "backtest_queue",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_queue_UserId_Status",
                table: "backtest_queue",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_BacktestResultsId",
                table: "backtest_results",
                column: "BacktestResultsId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_StrategyId",
                table: "backtest_results",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_SymbolId",
                table: "backtest_results",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_UserId",
                table: "backtest_results",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "brin_candles_ts",
                table: "candles",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "ux_candles_symbol_tf_ts",
                table: "candles",
                columns: new[] { "SymbolId", "Timeframe", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_data_providers_code",
                table: "data_providers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_data_providers_MarketId",
                table: "data_providers",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_data_providers_MarketId_is_active_priority",
                table: "data_providers",
                columns: new[] { "MarketId", "is_active", "priority" });

            migrationBuilder.CreateIndex(
                name: "IX_email_verifications_Email",
                table: "email_verifications",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_indicator_configs_UserId",
                table: "indicator_configs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_indicator_values_SymbolId_Timeframe_Timestamp",
                table: "indicator_values",
                columns: new[] { "SymbolId", "Timeframe", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_market_data_Symbol_Timeframe_Timestamp",
                table: "market_data",
                columns: new[] { "Symbol", "Timeframe", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_markets_AssetClassId",
                table: "markets",
                column: "AssetClassId");

            migrationBuilder.CreateIndex(
                name: "IX_markets_code",
                table: "markets",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_markets_is_active_display_order",
                table: "markets",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_history_DeviceId",
                table: "notification_history",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_history_UserId",
                table: "notification_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_password_resets_UserId",
                table: "password_resets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_positions_portfolio_id",
                table: "portfolio_positions",
                column: "portfolio_id");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_positions_portfolio_id_symbol_id",
                table: "portfolio_positions",
                columns: new[] { "portfolio_id", "symbol_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_positions_symbol_id",
                table: "portfolio_positions",
                column: "symbol_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_user_id",
                table: "price_alerts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_signals_StrategyId",
                table: "signals",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_signals_SymbolId",
                table: "signals",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_IndicatorConfigId",
                table: "strategies",
                column: "IndicatorConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_UserId",
                table: "strategies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_strategy_performance_strategy_id",
                table: "strategy_performance",
                column: "strategy_id");

            migrationBuilder.CreateIndex(
                name: "IX_strategy_performance_user_id",
                table: "strategy_performance",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_symbols_asset_class_id",
                table: "symbols",
                column: "asset_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_symbols_asset_class_is_active_is_popular",
                table: "symbols",
                columns: new[] { "asset_class", "is_active", "is_popular" });

            migrationBuilder.CreateIndex(
                name: "IX_symbols_market_id",
                table: "symbols",
                column: "market_id");

            migrationBuilder.CreateIndex(
                name: "IX_symbols_sector_industry",
                table: "symbols",
                columns: new[] { "sector", "industry" });

            migrationBuilder.CreateIndex(
                name: "IX_symbols_ticker_venue",
                table: "symbols",
                columns: new[] { "ticker", "venue" });

            migrationBuilder.CreateIndex(
                name: "IX_symbols_volume_24h",
                table: "symbols",
                column: "volume_24h");

            migrationBuilder.CreateIndex(
                name: "IX_temp_registrations_Email",
                table: "temp_registrations",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trade_history_BacktestResultsId",
                table: "trade_history",
                column: "BacktestResultsId");

            migrationBuilder.CreateIndex(
                name: "IX_trade_history_StrategyId",
                table: "trade_history",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_trade_history_SymbolId",
                table: "trade_history",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_trade_history_UserId",
                table: "trade_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_trading_sessions_MarketId",
                table: "trading_sessions",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_trading_sessions_MarketId_day_of_week_is_primary",
                table: "trading_sessions",
                columns: new[] { "MarketId", "day_of_week", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_executed_at",
                table: "transactions",
                column: "executed_at");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_portfolio_id",
                table: "transactions",
                column: "portfolio_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_portfolio_id_executed_at",
                table: "transactions",
                columns: new[] { "portfolio_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_portfolio_id_symbol_id_executed_at",
                table: "transactions",
                columns: new[] { "portfolio_id", "symbol_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_symbol_id",
                table: "transactions",
                column: "symbol_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_user_id",
                table: "user_achievements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_preferences_UserId",
                table: "user_notification_preferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_portfolios_user_id",
                table: "user_portfolios",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_portfolios_user_id_is_default",
                table: "user_portfolios",
                columns: new[] { "user_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_SessionToken",
                table: "user_sessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_UserId",
                table: "user_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevice_UserId",
                table: "UserDevice",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backtest_queue");

            migrationBuilder.DropTable(
                name: "candles");

            migrationBuilder.DropTable(
                name: "data_providers");

            migrationBuilder.DropTable(
                name: "email_verifications");

            migrationBuilder.DropTable(
                name: "indicator_values");

            migrationBuilder.DropTable(
                name: "market_data");

            migrationBuilder.DropTable(
                name: "notification_history");

            migrationBuilder.DropTable(
                name: "password_resets");

            migrationBuilder.DropTable(
                name: "portfolio_positions");

            migrationBuilder.DropTable(
                name: "price_alerts");

            migrationBuilder.DropTable(
                name: "signals");

            migrationBuilder.DropTable(
                name: "strategy_performance");

            migrationBuilder.DropTable(
                name: "temp_registrations");

            migrationBuilder.DropTable(
                name: "trade_history");

            migrationBuilder.DropTable(
                name: "trading_sessions");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "BacktestConfiguration");

            migrationBuilder.DropTable(
                name: "UserDevice");

            migrationBuilder.DropTable(
                name: "user_strategies");

            migrationBuilder.DropTable(
                name: "backtest_results");

            migrationBuilder.DropTable(
                name: "user_portfolios");

            migrationBuilder.DropTable(
                name: "strategies");

            migrationBuilder.DropTable(
                name: "symbols");

            migrationBuilder.DropTable(
                name: "indicator_configs");

            migrationBuilder.DropTable(
                name: "markets");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "asset_classes");
        }
    }
}
