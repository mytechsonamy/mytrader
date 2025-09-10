using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedSessionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "indicator_values",
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
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
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
                name: "strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    LastBacktestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "backtest_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalReturn = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    TotalReturnPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    AnnualizedReturn = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    MaxDrawdownPercentage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    SharpeRatio = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    SortinoRatio = table.Column<decimal>(type: "numeric", nullable: false),
                    CalmarRatio = table.Column<decimal>(type: "numeric", nullable: false),
                    Volatility = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalTrades = table.Column<int>(type: "integer", nullable: false),
                    WinningTrades = table.Column<int>(type: "integer", nullable: false),
                    LosingTrades = table.Column<int>(type: "integer", nullable: false),
                    WinRate = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageWin = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageLoss = table.Column<decimal>(type: "numeric", nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    StartingCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    EndingCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    PeakCapital = table.Column<decimal>(type: "numeric", nullable: false),
                    LowestCapital = table.Column<decimal>(type: "numeric", nullable: false),
                    TradingDays = table.Column<int>(type: "integer", nullable: false),
                    AverageHoldingPeriod = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxHoldingPeriod = table.Column<decimal>(type: "numeric", nullable: false),
                    MinHoldingPeriod = table.Column<decimal>(type: "numeric", nullable: false),
                    BetaToMarket = table.Column<decimal>(type: "numeric", nullable: false),
                    AlphaToMarket = table.Column<decimal>(type: "numeric", nullable: false),
                    TrackingError = table.Column<decimal>(type: "numeric", nullable: false),
                    InformationRatio = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageSlippage = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFees = table.Column<decimal>(type: "numeric", nullable: false),
                    FeeImpactPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DetailedResults = table.Column<string>(type: "jsonb", nullable: true),
                    StrategyConfig = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backtest_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_backtest_results_strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "trade_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: true),
                    BacktestResultsId = table.Column<Guid>(type: "uuid", nullable: true),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TradeType = table.Column<int>(type: "integer", nullable: false),
                    TradeSource = table.Column<int>(type: "integer", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    EntryValue = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    EntryFee = table.Column<decimal>(type: "numeric", nullable: false),
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
                        name: "FK_trade_history_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_StrategyId",
                table: "backtest_results",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_UserId",
                table: "backtest_results",
                column: "UserId");

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
                name: "IX_indicator_values_Symbol_Timeframe_Timestamp",
                table: "indicator_values",
                columns: new[] { "Symbol", "Timeframe", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_market_data_Symbol_Timeframe_Timestamp",
                table: "market_data",
                columns: new[] { "Symbol", "Timeframe", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_resets_UserId",
                table: "password_resets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_signals_StrategyId",
                table: "signals",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_IndicatorConfigId",
                table: "strategies",
                column: "IndicatorConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_UserId",
                table: "strategies",
                column: "UserId");

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
                name: "IX_trade_history_UserId",
                table: "trade_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_active",
                table: "user_sessions",
                column: "UserId",
                filter: "\"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_family",
                table: "user_sessions",
                column: "TokenFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_SessionToken",
                table: "user_sessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sessions_user_jwt",
                table: "user_sessions",
                columns: new[] { "UserId", "JwtId" },
                unique: true);

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
                name: "email_verifications");

            migrationBuilder.DropTable(
                name: "indicator_values");

            migrationBuilder.DropTable(
                name: "market_data");

            migrationBuilder.DropTable(
                name: "password_resets");

            migrationBuilder.DropTable(
                name: "signals");

            migrationBuilder.DropTable(
                name: "temp_registrations");

            migrationBuilder.DropTable(
                name: "trade_history");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "backtest_results");

            migrationBuilder.DropTable(
                name: "strategies");

            migrationBuilder.DropTable(
                name: "indicator_configs");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
