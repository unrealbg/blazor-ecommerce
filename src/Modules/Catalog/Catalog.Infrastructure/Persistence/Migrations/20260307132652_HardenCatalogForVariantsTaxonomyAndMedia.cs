using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCatalogForVariantsTaxonomyAndMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS "pgcrypto";""");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "catalog",
                table: "products",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "catalog",
                table: "products",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "catalog",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<Guid>(
                name: "brand_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                schema: "catalog",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<Guid>(
                name: "default_category_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "default_variant_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "product_type",
                schema: "catalog",
                table: "products",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Simple");

            migrationBuilder.AddColumn<string>(
                name: "canonical_url",
                schema: "catalog",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at_utc",
                schema: "catalog",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description",
                schema: "catalog",
                table: "products",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_title",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_description",
                schema: "catalog",
                table: "products",
                type: "character varying(800)",
                maxLength: 800,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                schema: "catalog",
                table: "products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.CreateTable(
                name: "brands",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    website_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    logo_image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    seo_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    seo_description = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    seo_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    seo_description = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_attributes",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    is_filterable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_attributes_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_categories",
                schema: "catalog",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => new { x.product_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_product_categories_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    alt_text = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_options",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_options_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_relations",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_relations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_relations_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    Barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    compare_at_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_option_values",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_option_values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_option_values_product_options_product_option_id",
                        column: x => x.product_option_id,
                        principalSchema: "catalog",
                        principalTable: "product_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant_option_assignments",
                schema: "catalog",
                columns: table => new
                {
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_option_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variant_option_assignments", x => new { x.variant_id, x.product_option_id });
                    table.ForeignKey(
                        name: "FK_variant_option_assignments_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "catalog",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE temp_catalog_default_variants
                (
                    product_id uuid PRIMARY KEY,
                    variant_id uuid NOT NULL
                );

                INSERT INTO temp_catalog_default_variants (product_id, variant_id)
                SELECT "Id", gen_random_uuid()
                FROM catalog.products;

                INSERT INTO catalog.product_variants
                (
                    "Id",
                    product_id,
                    "Sku",
                    "Name",
                    "Slug",
                    "Barcode",
                    price_amount,
                    "Currency",
                    compare_at_price_amount,
                    weight_kg,
                    is_active,
                    position,
                    created_at_utc,
                    updated_at_utc
                )
                SELECT
                    seed.variant_id,
                    product."Id",
                    COALESCE(NULLIF(BTRIM(product."Sku"), ''), CONCAT('SKU-', UPPER(REPLACE(seed.variant_id::text, '-', '')))),
                    NULL,
                    NULL,
                    NULL,
                    COALESCE(product.amount, 0),
                    COALESCE(NULLIF(BTRIM(product.currency), ''), 'EUR'),
                    NULL,
                    NULL,
                    COALESCE(product."IsActive", FALSE),
                    0,
                    NOW(),
                    NOW()
                FROM catalog.products AS product
                INNER JOIN temp_catalog_default_variants AS seed
                    ON seed.product_id = product."Id";

                UPDATE catalog.products AS product
                SET
                    "Status" = CASE WHEN COALESCE(product."IsActive", FALSE) THEN 'Active' ELSE 'Draft' END,
                    product_type = 'Simple',
                    short_description = CASE
                        WHEN product."Description" IS NULL OR BTRIM(product."Description") = '' THEN NULL
                        ELSE LEFT(product."Description", 800)
                    END,
                    published_at_utc = CASE WHEN COALESCE(product."IsActive", FALSE) THEN NOW() ELSE NULL END,
                    default_variant_id = seed.variant_id,
                    created_at_utc = NOW(),
                    updated_at_utc = NOW()
                FROM temp_catalog_default_variants AS seed
                WHERE seed.product_id = product."Id";

                INSERT INTO catalog.product_images
                (
                    "Id",
                    product_id,
                    variant_id,
                    source_url,
                    alt_text,
                    position,
                    is_primary,
                    created_at_utc
                )
                SELECT
                    gen_random_uuid(),
                    product."Id",
                    NULL,
                    product."ImageUrl",
                    product."Name",
                    0,
                    TRUE,
                    NOW()
                FROM catalog.products AS product
                WHERE NULLIF(BTRIM(product."ImageUrl"), '') IS NOT NULL;

                DROP TABLE temp_catalog_default_variants;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "default_variant_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_products_category_slug",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Brand",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Sku",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "amount",
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
                name: "currency",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_in_stock",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "catalog",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_brand_id",
                schema: "catalog",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_Status",
                schema: "catalog",
                table: "products",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_brands_Slug",
                schema: "catalog",
                table: "brands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_category_id",
                schema: "catalog",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Slug",
                schema: "catalog",
                table: "categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_attributes_product_id",
                schema: "catalog",
                table: "product_attributes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_category_id",
                schema: "catalog",
                table: "product_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id_variant_id_position",
                schema: "catalog",
                table: "product_images",
                columns: new[] { "product_id", "variant_id", "position" });

            migrationBuilder.CreateIndex(
                name: "IX_product_option_values_product_option_id",
                schema: "catalog",
                table: "product_option_values",
                column: "product_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_options_product_id",
                schema: "catalog",
                table: "product_options",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_relations_product_id_relation_type",
                schema: "catalog",
                table: "product_relations",
                columns: new[] { "product_id", "relation_type" });

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_product_id_is_active",
                schema: "catalog",
                table: "product_variants",
                columns: new[] { "product_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Sku",
                schema: "catalog",
                table: "product_variants",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brands",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_attributes",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_images",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_option_values",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_relations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "variant_option_assignments",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_options",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_products_brand_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_Status",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "brand_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "default_category_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "default_variant_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_featured",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_type",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "published_at_utc",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "seo_description",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "seo_title",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "short_description",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "canonical_url",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                schema: "catalog",
                table: "products");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "catalog",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                schema: "catalog",
                table: "products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                schema: "catalog",
                table: "products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "catalog",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

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

            migrationBuilder.AddColumn<string>(
                name: "currency",
                schema: "catalog",
                table: "products",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_in_stock",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "catalog",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_category_slug",
                schema: "catalog",
                table: "products",
                column: "category_slug");
        }
    }
}
