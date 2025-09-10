using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSymbolEntityMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_candles_symbol_tf_ts",
                table: "candles");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "candles");

            migrationBuilder.RenameColumn(
                name: "venue",
                table: "symbols",
                newName: "Venue");

            migrationBuilder.RenameColumn(
                name: "ticker",
                table: "symbols",
                newName: "Ticker");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "symbols",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "quote_ccy",
                table: "symbols",
                newName: "QuoteCurrency");

            migrationBuilder.RenameColumn(
                name: "base_ccy",
                table: "symbols",
                newName: "BaseCurrency");

            migrationBuilder.RenameColumn(
                name: "asset_class",
                table: "symbols",
                newName: "AssetClass");

            migrationBuilder.RenameColumn(
                name: "timestamp",
                table: "candles",
                newName: "Timestamp");

            migrationBuilder.AlterColumn<decimal>(
                name: "Vwap",
                table: "candles",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "candles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ux_candles_symbol_tf_ts",
                table: "candles",
                columns: new[] { "SymbolId", "Timeframe", "Timestamp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_candles_symbol_tf_ts",
                table: "candles");

            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "candles");

            migrationBuilder.RenameColumn(
                name: "Venue",
                table: "symbols",
                newName: "venue");

            migrationBuilder.RenameColumn(
                name: "Ticker",
                table: "symbols",
                newName: "ticker");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "symbols",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "QuoteCurrency",
                table: "symbols",
                newName: "quote_ccy");

            migrationBuilder.RenameColumn(
                name: "BaseCurrency",
                table: "symbols",
                newName: "base_ccy");

            migrationBuilder.RenameColumn(
                name: "AssetClass",
                table: "symbols",
                newName: "asset_class");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "candles",
                newName: "timestamp");

            migrationBuilder.AlterColumn<decimal>(
                name: "Vwap",
                table: "candles",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "candles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_candles_symbol_tf_ts",
                table: "candles",
                columns: new[] { "Symbol", "Timeframe", "timestamp" },
                unique: true);
        }
    }
}
