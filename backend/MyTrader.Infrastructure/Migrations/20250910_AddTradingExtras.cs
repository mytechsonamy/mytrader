using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyTrader.Infrastructure.Data;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    [DbContext(typeof(TradingDbContext))]
    [Migration("20250910235900_AddTradingExtras")]
    public partial class AddTradingExtras : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // user_notification_preferences
            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnableEmailSignals = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableEmailTradeExecutions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableEmailBacktestResults = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableEmailAccountUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableEmailMarketing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnablePushSignals = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnablePushTradeExecutions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnablePushBacktestResults = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnablePushAccountUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnablePushMarketing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnableSmsSignals = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnableSmsTradeExecutions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableSmsBacktestResults = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnableSmsAccountUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableTelegramSignals = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnableTelegramTradeExecutions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnableTelegramBacktestResults = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EnableTelegramAccountUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MinSignalConfidence = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0.7m),
                    PreferredTradingHours = table.Column<string>(type: "text", nullable: true),
                    FilterBySymbols = table.Column<string>(type: "text", nullable: true),
                    FilterByStrategies = table.Column<string>(type: "text", nullable: true),
                    EnableSignalAlerts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnablePortfolioAlerts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableStrategyAlerts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableMarketAlerts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PortfolioChangeThreshold = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 5m),
                    AlertMethods = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    QuietHoursEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    QuietHoursStart = table.Column<TimeSpan>(type: "interval", nullable: false, defaultValueSql: "interval '22 hours'"),
                    QuietHoursEnd = table.Column<TimeSpan>(type: "interval", nullable: false, defaultValueSql: "interval '8 hours'"),
                    QuietHoursDays = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[6,0]'::jsonb"),
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

            migrationBuilder.CreateIndex(
                name: "IX_user_notification_preferences_UserId",
                table: "user_notification_preferences",
                column: "UserId",
                unique: true);

            // user_portfolios
            migrationBuilder.CreateTable(
                name: "user_portfolios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_ccy = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false, defaultValue: "USD"),
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

            migrationBuilder.CreateIndex(
                name: "IX_user_portfolios_user_id",
                table: "user_portfolios",
                column: "user_id");

            // user_positions
            migrationBuilder.CreateTable(
                name: "user_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    avg_price = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_positions_user_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "user_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_positions_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_user_positions_portfolio_symbol",
                table: "user_positions",
                columns: new[] { "portfolio_id", "symbol_id" },
                unique: true);

            // user_trading_activity
            migrationBuilder.CreateTable(
                name: "user_trading_activity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_trading_activity", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_trading_activity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_trading_activity_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_trading_activity_user_id",
                table: "user_trading_activity",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_trading_activity_symbol_id",
                table: "user_trading_activity",
                column: "symbol_id");

            // backtests
            migrationBuilder.CreateTable(
                name: "backtests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    date_range_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_range_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    config_snapshot = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "running"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_return = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    sharpe_ratio = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    max_drawdown = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    total_trades = table.Column<int>(type: "integer", nullable: true),
                    code_ref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    engine_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "1.0"),
                    indicator_versions = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    config_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backtests", x => x.id);
                    table.ForeignKey(
                        name: "FK_backtests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_backtests_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalTable: "strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_backtests_user_status",
                table: "backtests",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_backtests_dedup",
                table: "backtests",
                columns: new[] { "strategy_id", "symbol", "timeframe", "date_range_start", "date_range_end", "config_hash" },
                unique: true);

            // backtest_trades
            migrationBuilder.CreateTable(
                name: "backtest_trades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    backtest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    pnl = table.Column<decimal>(type: "numeric(38,18)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backtest_trades", x => x.id);
                    table.ForeignKey(
                        name: "FK_backtest_trades_backtests_backtest_id",
                        column: x => x.backtest_id,
                        principalTable: "backtests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_trades_backtest_ts",
                table: "backtest_trades",
                columns: new[] { "backtest_id", "timestamp" });

            // backtest_metrics
            migrationBuilder.CreateTable(
                name: "backtest_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    backtest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<decimal>(type: "numeric(38,18)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backtest_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_backtest_metrics_backtests_backtest_id",
                        column: x => x.backtest_id,
                        principalTable: "backtests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backtest_metrics_backtest_id_metric",
                table: "backtest_metrics",
                columns: new[] { "backtest_id", "metric" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "backtest_metrics");
            migrationBuilder.DropTable(name: "backtest_trades");
            migrationBuilder.DropTable(name: "user_positions");
            migrationBuilder.DropTable(name: "user_trading_activity");
            migrationBuilder.DropTable(name: "user_notification_preferences");
            migrationBuilder.DropTable(name: "backtests");
            migrationBuilder.DropTable(name: "user_portfolios");
        }
    }
}
