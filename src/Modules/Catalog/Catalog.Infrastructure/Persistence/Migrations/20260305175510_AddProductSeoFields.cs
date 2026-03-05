using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSeoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                schema: "catalog",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "catalog",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                schema: "catalog",
                table: "products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category_name",
                schema: "catalog",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category_slug",
                schema: "catalog",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_in_stock",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_category_slug",
                schema: "catalog",
                table: "products",
                column: "category_slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_category_slug",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Brand",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Sku",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "category_name",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "category_slug",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_in_stock",
                schema: "catalog",
                table: "products");
        }
    }
}
