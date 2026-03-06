using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Search.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "product_search_documents",
                schema: "search",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    category_slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    category_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    brand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_in_stock = table.Column<bool>(type: "boolean", nullable: false),
                    image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    popularity_score = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_search_documents", x => x.product_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_brand",
                schema: "search",
                table: "product_search_documents",
                column: "brand");

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_category_slug",
                schema: "search",
                table: "product_search_documents",
                column: "category_slug");

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_is_active",
                schema: "search",
                table: "product_search_documents",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_is_in_stock",
                schema: "search",
                table: "product_search_documents",
                column: "is_in_stock");

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_normalized_name",
                schema: "search",
                table: "product_search_documents",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_price_amount",
                schema: "search",
                table: "product_search_documents",
                column: "price_amount");

            migrationBuilder.CreateIndex(
                name: "ix_product_search_documents_slug",
                schema: "search",
                table: "product_search_documents",
                column: "slug");

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_product_search_documents_search_vector
                ON search.product_search_documents
                USING GIN (to_tsvector('simple',
                    coalesce(name, '') || ' ' ||
                    coalesce(description_text, '') || ' ' ||
                    coalesce(brand, '') || ' ' ||
                    coalesce(category_name, '')));
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_product_search_documents_name_trgm
                ON search.product_search_documents
                USING GIN (name gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_search_documents",
                schema: "search");

            migrationBuilder.Sql("DROP INDEX IF EXISTS search.ix_product_search_documents_search_vector;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS search.ix_product_search_documents_name_trgm;");
        }
    }
}
