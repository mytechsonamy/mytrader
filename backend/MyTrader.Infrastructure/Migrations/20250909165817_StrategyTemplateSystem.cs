using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrategyTemplateSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserStrategyId",
                table: "trade_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserStrategyId",
                table: "signals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserStrategyId",
                table: "backtest_results",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "strategy_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    ParameterSchema = table.Column<string>(type: "jsonb", nullable: true),
                    EntryRules = table.Column<string>(type: "jsonb", nullable: false),
                    ExitRules = table.Column<string>(type: "jsonb", nullable: false),
                    RiskManagement = table.Column<string>(type: "jsonb", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupportedAssetClasses = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupportedTimeframes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    MinRecommendedCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    VolatilityLevel = table.Column<int>(type: "integer", nullable: true),
                    ExpectedWinRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    UsageStats = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategy_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_strategy_templates_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    CustomEntryRules = table.Column<string>(type: "jsonb", nullable: true),
                    CustomExitRules = table.Column<string>(type: "jsonb", nullable: true),
                    CustomRiskManagement = table.Column<string>(type: "jsonb", nullable: true),
                    TargetSymbols = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Timeframe = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCustom = table.Column<bool>(type: "boolean", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    InitialCapital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    MaxPositionSizePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TemplateVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LastBacktestResults = table.Column<string>(type: "jsonb", nullable: true),
                    LastBacktestAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PerformanceStats = table.Column<string>(type: "jsonb", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_strategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_strategies_strategy_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "strategy_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_strategies_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trade_history_UserStrategyId",
                table: "trade_history",
                column: "UserStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_signals_UserStrategyId",
                table: "signals",
                column: "UserStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_backtest_results_UserStrategyId",
                table: "backtest_results",
                column: "UserStrategyId");

            migrationBuilder.CreateIndex(
                name: "gin_templates_params",
                table: "strategy_templates",
                column: "Parameters")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "gin_templates_schema",
                table: "strategy_templates",
                column: "ParameterSchema")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_strategy_templates_CreatedBy",
                table: "strategy_templates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "ix_templates_active",
                table: "strategy_templates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_templates_category",
                table: "strategy_templates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "ix_templates_default",
                table: "strategy_templates",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "ix_templates_public",
                table: "strategy_templates",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "ux_templates_name_ver",
                table: "strategy_templates",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "gin_user_strategies_params",
                table: "user_strategies",
                column: "Parameters")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_user_strategies_active",
                table: "user_strategies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_user_strategies_favorite",
                table: "user_strategies",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "ix_user_strategies_template",
                table: "user_strategies",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "ix_user_strategies_user",
                table: "user_strategies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ux_user_strategies_name",
                table: "user_strategies",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_backtest_results_user_strategies_UserStrategyId",
                table: "backtest_results",
                column: "UserStrategyId",
                principalTable: "user_strategies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_signals_user_strategies_UserStrategyId",
                table: "signals",
                column: "UserStrategyId",
                principalTable: "user_strategies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_trade_history_user_strategies_UserStrategyId",
                table: "trade_history",
                column: "UserStrategyId",
                principalTable: "user_strategies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_backtest_results_user_strategies_UserStrategyId",
                table: "backtest_results");

            migrationBuilder.DropForeignKey(
                name: "FK_signals_user_strategies_UserStrategyId",
                table: "signals");

            migrationBuilder.DropForeignKey(
                name: "FK_trade_history_user_strategies_UserStrategyId",
                table: "trade_history");

            migrationBuilder.DropTable(
                name: "user_strategies");

            migrationBuilder.DropTable(
                name: "strategy_templates");

            migrationBuilder.DropIndex(
                name: "IX_trade_history_UserStrategyId",
                table: "trade_history");

            migrationBuilder.DropIndex(
                name: "IX_signals_UserStrategyId",
                table: "signals");

            migrationBuilder.DropIndex(
                name: "IX_backtest_results_UserStrategyId",
                table: "backtest_results");

            migrationBuilder.DropColumn(
                name: "UserStrategyId",
                table: "trade_history");

            migrationBuilder.DropColumn(
                name: "UserStrategyId",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "UserStrategyId",
                table: "backtest_results");
        }
    }
}
