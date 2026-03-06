using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingAndFulfillmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LastShipmentId",
                schema: "orders",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fulfillment_status",
                schema: "orders",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_amount",
                schema: "orders",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "shipping_currency",
                schema: "orders",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_code",
                schema: "orders",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_name",
                schema: "orders",
                table: "orders",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_orders_fulfillment_status",
                schema: "orders",
                table: "orders",
                column: "fulfillment_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_fulfillment_status",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "LastShipmentId",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_status",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_amount",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_currency",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_code",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_name",
                schema: "orders",
                table: "orders");
        }
    }
}
