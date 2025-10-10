using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetClassToMarketData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "password_resets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetClass",
                table: "market_data",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "historical_market_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    trade_date = table.Column<DateOnly>(type: "date", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_ticker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    market_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "DAILY"),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    open_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    high_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    low_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    close_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    adjusted_close_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    volume = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    vwap = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    bist_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    previous_close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    price_change = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    price_change_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    trading_value = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    transaction_count = table.Column<long>(type: "bigint", nullable: true),
                    market_cap = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    free_float_market_cap = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    shares_outstanding = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    free_float_shares = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    index_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    index_change_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    usd_try_rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    eur_try_rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    rsi = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    macd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    macd_signal = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    bollinger_upper = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    bollinger_lower = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    sma_20 = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    sma_50 = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    sma_200 = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false, defaultValue: "USD"),
                    data_quality_score = table.Column<int>(type: "integer", nullable: true),
                    extended_data = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    source_metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    data_flags = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    source_priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    data_collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_market_data", x => new { x.id, x.trade_date });
                    table.ForeignKey(
                        name: "FK_historical_market_data_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "market_data_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_ticker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    period_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    trading_days = table.Column<int>(type: "integer", nullable: false),
                    period_open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    period_close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    period_high = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    period_low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    period_vwap = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    total_return_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    avg_daily_return_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    volatility = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: true),
                    annualized_volatility = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: true),
                    sharpe_ratio = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    max_drawdown_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    beta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    total_volume = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    avg_daily_volume = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    total_trading_value = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    avg_daily_trading_value = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    total_transactions = table.Column<long>(type: "bigint", nullable: true),
                    avg_daily_transactions = table.Column<long>(type: "bigint", nullable: true),
                    support_level = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    resistance_level = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    week_52_high = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    week_52_low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    avg_rsi = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    avg_macd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    days_above_sma20_percent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    days_above_sma50_percent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    vs_market_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    market_correlation = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: true),
                    performance_percentile = table.Column<int>(type: "integer", nullable: true),
                    volume_percentile = table.Column<int>(type: "integer", nullable: true),
                    quality_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_summaries", x => new { x.id, x.period_start });
                    table.ForeignKey(
                        name: "FK_market_data_summaries_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_dashboard_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    custom_alias = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    widget_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    widget_config = table.Column<string>(type: "jsonb", nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_dashboard_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_dashboard_preferences_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_dashboard_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_bist",
                table: "historical_market_data",
                columns: new[] { "bist_code", "trade_date" },
                filter: "bist_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_date_source",
                table: "historical_market_data",
                columns: new[] { "trade_date", "data_source", "source_priority" });

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_dedup",
                table: "historical_market_data",
                columns: new[] { "symbol_ticker", "trade_date", "timeframe", "data_source", "source_priority" });

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_intraday",
                table: "historical_market_data",
                columns: new[] { "symbol_ticker", "timestamp" },
                descending: new[] { false, true },
                filter: "timestamp IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_primary",
                table: "historical_market_data",
                columns: new[] { "symbol_ticker", "timeframe", "trade_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_symbol_date",
                table: "historical_market_data",
                columns: new[] { "symbol_id", "trade_date", "timeframe" });

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_technical",
                table: "historical_market_data",
                columns: new[] { "trade_date", "rsi", "macd" },
                filter: "rsi IS NOT NULL OR macd IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_historical_market_data_volume",
                table: "historical_market_data",
                columns: new[] { "trade_date", "volume" },
                descending: new bool[0],
                filter: "volume IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_market_data_summaries_performance",
                table: "market_data_summaries",
                columns: new[] { "period_type", "period_start", "total_return_percent" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "idx_market_data_summaries_primary",
                table: "market_data_summaries",
                columns: new[] { "symbol_ticker", "period_type", "period_start" },
                unique: true,
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_market_data_summaries_quality",
                table: "market_data_summaries",
                columns: new[] { "period_type", "quality_score", "period_start" },
                descending: new[] { false, true, true },
                filter: "quality_score >= 80");

            migrationBuilder.CreateIndex(
                name: "idx_market_data_summaries_volume",
                table: "market_data_summaries",
                columns: new[] { "period_type", "period_start", "avg_daily_volume" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_market_data_summaries_symbol_id",
                table: "market_data_summaries",
                column: "symbol_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_dashboard_preferences_symbol_id",
                table: "user_dashboard_preferences",
                column: "symbol_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_dashboard_preferences_user_id",
                table: "user_dashboard_preferences",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historical_market_data");

            migrationBuilder.DropTable(
                name: "market_data_summaries");

            migrationBuilder.DropTable(
                name: "user_dashboard_preferences");

            migrationBuilder.DropColumn(
                name: "AssetClass",
                table: "market_data");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "password_resets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
