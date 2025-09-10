using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBacktestQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Display",
                table: "symbols",
                newName: "display");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backtest_queue");

            migrationBuilder.DropTable(
                name: "BacktestConfiguration");

            migrationBuilder.RenameColumn(
                name: "display",
                table: "symbols",
                newName: "Display");
        }
    }
}
