using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Migrations
{
    public partial class AddTradingCoreEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

            migrationBuilder.CreateTable(
                name: "symbols",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    ticker = table.Column<string>(maxLength: 50, nullable: false),
                    venue = table.Column<string>(maxLength: 50, nullable: false),
                    asset_class = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "CRYPTO"),
                    base_ccy = table.Column<string>(maxLength: 12, nullable: true),
                    quote_ccy = table.Column<string>(maxLength: 12, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symbols", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_symbols_ticker_venue",
                table: "symbols",
                columns: new[] { "ticker", "venue" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    jwt_id = table.Column<string>(maxLength: 255, nullable: false),
                    refresh_token_hash = table.Column<string>(maxLength: 255, nullable: false),
                    token_family_id = table.Column<Guid>(nullable: false),
                    rotated_from = table.Column<Guid>(nullable: true),
                    user_agent = table.Column<string>(nullable: true),
                    ip_address = table.Column<string>(maxLength: 45, nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(nullable: true),
                    created_at = table.Column<DateTimeOffset>(nullable: false),
                    expires_at = table.Column<DateTimeOffset>(nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_active",
                table: "user_sessions",
                column: "user_id",
                filter: "revoked_at IS NULL AND expires_at > NOW()"
            );

            migrationBuilder.CreateIndex(
                name: "ux_sessions_user_jwt",
                table: "user_sessions",
                columns: new[] {"user_id", "jwt_id"},
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_sessions_family",
                table: "user_sessions",
                column: "token_family_id"
            );

            migrationBuilder.CreateTable(
                name: "strategy_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    name = table.Column<string>(maxLength: 100, nullable: false),
                    version = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "1.0"),
                    parameters = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    param_schema = table.Column<string>(type: "jsonb", nullable: true),
                    created_by = table.Column<Guid>(nullable: true),
                    is_default = table.Column<bool>(nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_templates_name_ver",
                table: "strategy_templates",
                columns: new[] { "name", "version" },
                unique: true
            ).Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "gin_templates_params",
                table: "strategy_templates",
                column: "parameters"
            ).Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateTable(
                name: "user_strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    template_id = table.Column<Guid>(nullable: true),
                    name = table.Column<string>(maxLength: 100, nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    is_active = table.Column<bool>(nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false),
                    template_version = table.Column<string>(maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_strategies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "gin_user_strategies_params",
                table: "user_strategies",
                column: "parameters"
            ).Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateTable(
                name: "candles",
                columns: table => new
                {
                    symbol_id = table.Column<Guid>(nullable: false),
                    timeframe = table.Column<string>(maxLength: 10, nullable: false),
                    ts = table.Column<DateTimeOffset>(nullable: false),
                    open = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    high = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    low  = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    close= table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    volume= table.Column<decimal>(type: "numeric(38,18)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candles", x => new { x.symbol_id, x.timeframe, x.ts });
                    table.ForeignKey(
                        name: "FK_candles_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "brin_candles_ts",
                table: "candles",
                column: "ts"
            ).Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateTable(
                name: "backtests",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    strategy_id = table.Column<Guid>(nullable: false),
                    symbol = table.Column<string>(maxLength: 20, nullable: false),
                    timeframe = table.Column<string>(maxLength: 10, nullable: false),
                    date_range_start = table.Column<DateTimeOffset>(nullable: false),
                    date_range_end = table.Column<DateTimeOffset>(nullable: false),
                    config_snapshot = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    status = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "running"),
                    started_at = table.Column<DateTimeOffset>(nullable: false),
                    finished_at = table.Column<DateTimeOffset>(nullable: true),
                    total_return = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    sharpe_ratio = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    max_drawdown = table.Column<decimal>(type: "numeric(38,18)", nullable: true),
                    total_trades = table.Column<int>(nullable: true),
                    code_ref = table.Column<string>(maxLength: 64, nullable: false),
                    engine_version = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "1.0"),
                    indicator_versions = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    config_hash = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backtests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_backtests_user_status",
                table: "backtests",
                columns: new[] { "user_id", "status" }
            );

            migrationBuilder.CreateIndex(
                name: "ux_backtests_dedup",
                table: "backtests",
                columns: new[] { "strategy_id", "symbol", "timeframe", "date_range_start", "date_range_end", "config_hash" },
                unique: true
            );

            migrationBuilder.CreateTable(
                name: "backtest_trades",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    backtest_id = table.Column<Guid>(nullable: false),
                    timestamp = table.Column<DateTimeOffset>(nullable: false),
                    symbol = table.Column<string>(maxLength: 20, nullable: false),
                    side = table.Column<string>(maxLength: 10, nullable: false),
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
                columns: new[] { "backtest_id", "timestamp" }
            );

            migrationBuilder.CreateTable(
                name: "backtest_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    backtest_id = table.Column<Guid>(nullable: false),
                    metric = table.Column<string>(maxLength: 64, nullable: false),
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
                columns: new[] { "backtest_id", "metric" }
            );

            migrationBuilder.CreateTable(
                name: "user_portfolios",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    base_ccy = table.Column<string>(maxLength: 12, nullable: false, defaultValue: "USD"),
                    updated_at = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_portfolios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    portfolio_id = table.Column<Guid>(nullable: false),
                    symbol_id = table.Column<Guid>(nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    avg_price = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_positions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_user_positions_portfolio_symbol",
                table: "user_positions",
                columns: new[] { "portfolio_id", "symbol_id" },
                unique: true
            );

            migrationBuilder.CreateTable(
                name: "user_trading_activity",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false),
                    symbol_id = table.Column<Guid>(nullable: false),
                    source = table.Column<string>(maxLength: 10, nullable: false),
                    side = table.Column<string>(maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(38,18)", nullable: false),
                    ts = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_trading_activity", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "backtest_metrics");
            migrationBuilder.DropTable(name: "backtest_trades");
            migrationBuilder.DropTable(name: "candles");
            migrationBuilder.DropTable(name: "strategy_templates");
            migrationBuilder.DropTable(name: "user_positions");
            migrationBuilder.DropTable(name: "user_portfolios");
            migrationBuilder.DropTable(name: "user_strategies");
            migrationBuilder.DropTable(name: "user_sessions");
            migrationBuilder.DropTable(name: "user_trading_activity");
            migrationBuilder.DropTable(name: "backtests");
            migrationBuilder.DropTable(name: "symbols");
        }
    }
}
