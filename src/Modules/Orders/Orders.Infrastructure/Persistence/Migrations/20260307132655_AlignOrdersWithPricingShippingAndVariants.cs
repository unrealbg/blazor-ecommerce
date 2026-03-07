using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignOrdersWithPricingShippingAndVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                schema: "orders",
                table: "order_lines",
                newName: "product_name");

            migrationBuilder.AddColumn<string>(
                name: "applied_coupons_json",
                schema: "orders",
                table: "orders",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "applied_promotions_json",
                schema: "orders",
                table: "orders",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cart_discount_total_amount",
                schema: "orders",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "cart_discount_total_currency",
                schema: "orders",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_total_amount",
                schema: "orders",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_discount_total_currency",
                schema: "orders",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_discount_total_amount",
                schema: "orders",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "shipping_discount_total_currency",
                schema: "orders",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_before_discount_amount",
                schema: "orders",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "subtotal_before_discount_currency",
                schema: "orders",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                schema: "orders",
                table: "order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "applied_discounts_json",
                schema: "orders",
                table: "order_lines",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_unit_amount",
                schema: "orders",
                table: "order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "compare_at_price_amount",
                schema: "orders",
                table: "order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_total_amount",
                schema: "orders",
                table: "order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "selected_options_json",
                schema: "orders",
                table: "order_lines",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sku",
                schema: "orders",
                table: "order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variant_name",
                schema: "orders",
                table: "order_lines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE orders.orders
                SET
                    subtotal_before_discount_amount = subtotal_amount,
                    subtotal_before_discount_currency = subtotal_currency,
                    line_discount_total_amount = 0,
                    line_discount_total_currency = subtotal_currency,
                    cart_discount_total_amount = 0,
                    cart_discount_total_currency = subtotal_currency,
                    shipping_discount_total_amount = 0,
                    shipping_discount_total_currency = shipping_currency;

                UPDATE orders.order_lines AS line
                SET
                    variant_id = product.default_variant_id,
                    sku = variant."Sku",
                    variant_name = variant."Name",
                    base_unit_amount = line.unit_amount,
                    discount_total_amount = 0
                FROM catalog.products AS product
                INNER JOIN catalog.product_variants AS variant
                    ON variant."Id" = product.default_variant_id
                WHERE line.product_id = product."Id";

                DELETE FROM orders.order_lines
                WHERE variant_id = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_order_lines",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_lines",
                schema: "orders",
                table: "order_lines",
                columns: new[] { "order_id", "variant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_order_lines",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "applied_coupons_json",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "applied_promotions_json",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cart_discount_total_amount",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cart_discount_total_currency",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "line_discount_total_amount",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "line_discount_total_currency",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_discount_total_amount",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_discount_total_currency",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "subtotal_before_discount_amount",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "subtotal_before_discount_currency",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "variant_id",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "applied_discounts_json",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "base_unit_amount",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "compare_at_price_amount",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "discount_total_amount",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "selected_options_json",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "sku",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "variant_name",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.RenameColumn(
                name: "product_name",
                schema: "orders",
                table: "order_lines",
                newName: "name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_lines",
                schema: "orders",
                table: "order_lines",
                columns: new[] { "order_id", "product_id" });
        }
    }
}
