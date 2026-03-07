using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCartForVariantsAndCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "applied_coupon_code",
                schema: "cart",
                table: "carts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                schema: "cart",
                table: "cart_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                schema: "cart",
                table: "cart_lines",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selected_options_json",
                schema: "cart",
                table: "cart_lines",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sku",
                schema: "cart",
                table: "cart_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variant_name",
                schema: "cart",
                table: "cart_lines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE cart.cart_lines AS line
                SET
                    variant_id = product.default_variant_id,
                    sku = variant."Sku",
                    variant_name = COALESCE(variant."Name", line.product_name),
                    image_url = image.source_url
                FROM catalog.products AS product
                INNER JOIN catalog.product_variants AS variant
                    ON variant."Id" = product.default_variant_id
                LEFT JOIN LATERAL
                (
                    SELECT source_url
                    FROM catalog.product_images
                    WHERE product_id = product."Id"
                    ORDER BY is_primary DESC, position ASC
                    LIMIT 1
                ) AS image ON TRUE
                WHERE line.product_id = product."Id";

                DELETE FROM cart.cart_lines
                WHERE variant_id = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_cart_lines",
                schema: "cart",
                table: "cart_lines");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cart_lines",
                schema: "cart",
                table: "cart_lines",
                columns: new[] { "cart_id", "variant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_cart_lines",
                schema: "cart",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "applied_coupon_code",
                schema: "cart",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "variant_id",
                schema: "cart",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "image_url",
                schema: "cart",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "selected_options_json",
                schema: "cart",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "sku",
                schema: "cart",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "variant_name",
                schema: "cart",
                table: "cart_lines");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cart_lines",
                schema: "cart",
                table: "cart_lines",
                columns: new[] { "cart_id", "product_id" });
        }
    }
}
