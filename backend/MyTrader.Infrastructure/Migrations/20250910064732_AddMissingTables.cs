using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "user_sessions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JwtId",
                table: "user_sessions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "user_sessions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RotatedFrom",
                table: "user_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TokenFamilyId",
                table: "user_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "user_sessions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "symbols",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "symbols",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "symbols",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTracked",
                table: "symbols",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "symbols",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricePrecision",
                table: "symbols",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityPrecision",
                table: "symbols",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StepSize",
                table: "symbols",
                type: "numeric(38,18)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TickSize",
                table: "symbols",
                type: "numeric(38,18)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "symbols",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "candles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    Vwap = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "brin_candles_ts",
                table: "candles",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ux_candles_symbol_tf_ts",
                table: "candles",
                columns: new[] { "Symbol", "Timeframe", "timestamp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candles");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "JwtId",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "RotatedFrom",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "TokenFamilyId",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "IsTracked",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "PricePrecision",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "QuantityPrecision",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "StepSize",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "TickSize",
                table: "symbols");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "symbols");
        }
    }
}
