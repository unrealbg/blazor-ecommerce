using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackInventoryByVariantAndReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_stock_items_product_id_sku",
                schema: "inventory",
                table: "stock_items");

            migrationBuilder.DropIndex(
                name: "ux_stock_items_product_id_without_sku",
                schema: "inventory",
                table: "stock_items");

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                schema: "inventory",
                table: "stock_reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                schema: "inventory",
                table: "stock_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                UPDATE inventory.stock_items AS item
                SET
                    variant_id = product.default_variant_id,
                    sku = COALESCE(NULLIF(BTRIM(item.sku), ''), variant."Sku")
                FROM catalog.products AS product
                INNER JOIN catalog.product_variants AS variant
                    ON variant."Id" = product.default_variant_id
                WHERE item.product_id = product."Id";

                UPDATE inventory.stock_reservations AS reservation
                SET
                    variant_id = product.default_variant_id,
                    sku = COALESCE(NULLIF(BTRIM(reservation.sku), ''), variant."Sku")
                FROM catalog.products AS product
                INNER JOIN catalog.product_variants AS variant
                    ON variant."Id" = product.default_variant_id
                WHERE reservation.product_id = product."Id";

                DELETE FROM inventory.stock_reservations
                WHERE variant_id = '00000000-0000-0000-0000-000000000000';

                DELETE FROM inventory.stock_items
                WHERE variant_id = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservations_variant_status",
                schema: "inventory",
                table: "stock_reservations",
                columns: new[] { "variant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_product_id_sku",
                schema: "inventory",
                table: "stock_items",
                columns: new[] { "product_id", "sku" });

            migrationBuilder.CreateIndex(
                name: "ux_stock_items_variant_id",
                schema: "inventory",
                table: "stock_items",
                column: "variant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_reservations_variant_status",
                schema: "inventory",
                table: "stock_reservations");

            migrationBuilder.DropIndex(
                name: "ix_stock_items_product_id_sku",
                schema: "inventory",
                table: "stock_items");

            migrationBuilder.DropIndex(
                name: "ux_stock_items_variant_id",
                schema: "inventory",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "variant_id",
                schema: "inventory",
                table: "stock_reservations");

            migrationBuilder.DropColumn(
                name: "variant_id",
                schema: "inventory",
                table: "stock_items");

            migrationBuilder.CreateIndex(
                name: "ux_stock_items_product_id_sku",
                schema: "inventory",
                table: "stock_items",
                columns: new[] { "product_id", "sku" },
                unique: true,
                filter: "sku IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_stock_items_product_id_without_sku",
                schema: "inventory",
                table: "stock_items",
                column: "product_id",
                unique: true,
                filter: "sku IS NULL");
        }
    }
}
