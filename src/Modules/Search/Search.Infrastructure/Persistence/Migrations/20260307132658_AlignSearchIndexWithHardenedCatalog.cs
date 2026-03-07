using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Search.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSearchIndexWithHardenedCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "category_slug",
                schema: "search",
                table: "product_search_documents",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                schema: "search",
                table: "product_search_documents",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "search",
                table: "product_search_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "search_text",
                schema: "search",
                table: "product_search_documents",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_category_id",
                schema: "search",
                table: "product_search_documents",
                column: "category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_search_documents_category_id",
                schema: "search",
                table: "product_search_documents");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "search",
                table: "product_search_documents");

            migrationBuilder.DropColumn(
                name: "search_text",
                schema: "search",
                table: "product_search_documents");

            migrationBuilder.AlterColumn<string>(
                name: "category_slug",
                schema: "search",
                table: "product_search_documents",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "brand",
                schema: "search",
                table: "product_search_documents",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldNullable: true);
        }
    }
}
