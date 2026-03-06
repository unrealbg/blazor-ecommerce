using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionId",
                schema: "orders",
                table: "orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "LastPaymentIntentId",
                schema: "orders",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                schema: "orders",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFailureMessage",
                schema: "orders",
                table: "orders",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status",
                schema: "orders",
                table: "orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_Status",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionId",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "LastPaymentIntentId",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PaymentFailureMessage",
                schema: "orders",
                table: "orders");
        }
    }
}
