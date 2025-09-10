using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SymbolNormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_indicator_values_Symbol_Timeframe_Timestamp",
                table: "indicator_values");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "trade_history");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "indicator_values");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "backtest_results");

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "trade_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId1",
                table: "trade_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "signals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "indicator_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId1",
                table: "indicator_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "backtest_results",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "symbols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ticker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Venue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssetClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BaseCurrency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    QuoteCurrency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsTracked = table.Column<bool>(type: "boolean", nullable: false),
                    TickSize = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    StepSize = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    PricePrecision = table.Column<int>(type: "integer", nullable: true),
                    QuantityPrecision = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "candles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SymbolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    High = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    TradeCount = table.Column<long>(type: "bigint", nullable: true),
                    Vwap = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DataSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_candles_symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trade_history_SymbolId",
                table: "trade_history",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_trade_history_SymbolId1",
                table: "trade_history",
                column: "SymbolId1");

            migrationBuilder.CreateIndex(
                name: "IX_signals_SymbolId",
                table: "signals",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_indicator_values_SymbolId_Timeframe_Timestamp",
                table: "indicator_values",
                columns: new[] { "SymbolId", "Timeframe", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_indicator_values_SymbolId1",
                table: "indicator_values",
                column: "SymbolId1");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_SymbolId",
                table: "backtest_results",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "brin_candles_ts",
                table: "candles",
                column: "Timestamp")
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "ix_candles_finalized",
                table: "candles",
                column: "IsFinalized");

            migrationBuilder.CreateIndex(
                name: "ix_candles_symbol",
                table: "candles",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "ix_candles_timeframe",
                table: "candles",
                column: "Timeframe");

            migrationBuilder.CreateIndex(
                name: "ux_candles_symbol_tf_ts",
                table: "candles",
                columns: new[] { "SymbolId", "Timeframe", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "gin_symbols_metadata",
                table: "symbols",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_symbols_active",
                table: "symbols",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_symbols_asset_class",
                table: "symbols",
                column: "AssetClass");

            migrationBuilder.CreateIndex(
                name: "ix_symbols_tracked",
                table: "symbols",
                column: "IsTracked");

            migrationBuilder.CreateIndex(
                name: "ux_symbols_ticker_venue",
                table: "symbols",
                columns: new[] { "Ticker", "Venue" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_backtest_results_symbols_SymbolId",
                table: "backtest_results",
                column: "SymbolId",
                principalTable: "symbols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_indicator_values_symbols_SymbolId",
                table: "indicator_values",
                column: "SymbolId",
                principalTable: "symbols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_indicator_values_symbols_SymbolId1",
                table: "indicator_values",
                column: "SymbolId1",
                principalTable: "symbols",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_signals_symbols_SymbolId",
                table: "signals",
                column: "SymbolId",
                principalTable: "symbols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_trade_history_symbols_SymbolId",
                table: "trade_history",
                column: "SymbolId",
                principalTable: "symbols",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_trade_history_symbols_SymbolId1",
                table: "trade_history",
                column: "SymbolId1",
                principalTable: "symbols",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_backtest_results_symbols_SymbolId",
                table: "backtest_results");

            migrationBuilder.DropForeignKey(
                name: "FK_indicator_values_symbols_SymbolId",
                table: "indicator_values");

            migrationBuilder.DropForeignKey(
                name: "FK_indicator_values_symbols_SymbolId1",
                table: "indicator_values");

            migrationBuilder.DropForeignKey(
                name: "FK_signals_symbols_SymbolId",
                table: "signals");

            migrationBuilder.DropForeignKey(
                name: "FK_trade_history_symbols_SymbolId",
                table: "trade_history");

            migrationBuilder.DropForeignKey(
                name: "FK_trade_history_symbols_SymbolId1",
                table: "trade_history");

            migrationBuilder.DropTable(
                name: "candles");

            migrationBuilder.DropTable(
                name: "symbols");

            migrationBuilder.DropIndex(
                name: "IX_trade_history_SymbolId",
                table: "trade_history");

            migrationBuilder.DropIndex(
                name: "IX_trade_history_SymbolId1",
                table: "trade_history");

            migrationBuilder.DropIndex(
                name: "IX_signals_SymbolId",
                table: "signals");

            migrationBuilder.DropIndex(
                name: "IX_indicator_values_SymbolId_Timeframe_Timestamp",
                table: "indicator_values");

            migrationBuilder.DropIndex(
                name: "IX_indicator_values_SymbolId1",
                table: "indicator_values");

            migrationBuilder.DropIndex(
                name: "IX_backtest_results_SymbolId",
                table: "backtest_results");

            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "trade_history");

            migrationBuilder.DropColumn(
                name: "SymbolId1",
                table: "trade_history");

            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "indicator_values");

            migrationBuilder.DropColumn(
                name: "SymbolId1",
                table: "indicator_values");

            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "backtest_results");

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "trade_history",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "signals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "indicator_values",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "backtest_results",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_indicator_values_Symbol_Timeframe_Timestamp",
                table: "indicator_values",
                columns: new[] { "Symbol", "Timeframe", "Timestamp" },
                unique: true);
        }
    }
}
