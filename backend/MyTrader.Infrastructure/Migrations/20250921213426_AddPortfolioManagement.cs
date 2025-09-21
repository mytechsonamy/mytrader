using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyTrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    alert_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_price = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    percentage_change = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_triggered = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    triggered_price = table.Column<decimal>(type: "numeric(18,8)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_alerts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_portfolios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    base_currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    initial_capital = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    current_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    cash_balance = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    total_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    daily_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    total_return_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
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

            migrationBuilder.CreateTable(
                name: "UserDevice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevice_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    average_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    market_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    unrealized_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    unrealized_pnl_percent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    realized_pnl = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    cost_basis = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    first_purchased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_traded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_portfolio_positions_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_portfolio_positions_user_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "user_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    fee = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    execution_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_symbols_symbol_id",
                        column: x => x.symbol_id,
                        principalTable: "symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transactions_user_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "user_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_history_UserDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "UserDevice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_notification_history_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_history_DeviceId",
                table: "notification_history",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_history_UserId",
                table: "notification_history",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_positions_portfolio_id",
                table: "portfolio_positions",
                column: "portfolio_id");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_positions_portfolio_id_symbol_id",
                table: "portfolio_positions",
                columns: new[] { "portfolio_id", "symbol_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_positions_symbol_id",
                table: "portfolio_positions",
                column: "symbol_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_user_id",
                table: "price_alerts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_executed_at",
                table: "transactions",
                column: "executed_at");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_portfolio_id",
                table: "transactions",
                column: "portfolio_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_portfolio_id_executed_at",
                table: "transactions",
                columns: new[] { "portfolio_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_portfolio_id_symbol_id_executed_at",
                table: "transactions",
                columns: new[] { "portfolio_id", "symbol_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_symbol_id",
                table: "transactions",
                column: "symbol_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_portfolios_user_id",
                table: "user_portfolios",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_portfolios_user_id_is_default",
                table: "user_portfolios",
                columns: new[] { "user_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDevice_UserId",
                table: "UserDevice",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_history");

            migrationBuilder.DropTable(
                name: "portfolio_positions");

            migrationBuilder.DropTable(
                name: "price_alerts");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "UserDevice");

            migrationBuilder.DropTable(
                name: "user_portfolios");
        }
    }
}
