using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgentPackUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BacktestResultsJson",
                table: "strategies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Configuration",
                table: "strategies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PerformanceScore",
                table: "strategies",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SymbolId",
                table: "strategies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parameters = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    template_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_strategies", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_strategies");

            migrationBuilder.DropColumn(
                name: "BacktestResultsJson",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "Configuration",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "PerformanceScore",
                table: "strategies");

            migrationBuilder.DropColumn(
                name: "SymbolId",
                table: "strategies");
        }
    }
}
