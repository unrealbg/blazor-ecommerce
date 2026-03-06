using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Redirects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRedirects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "redirects");

            migrationBuilder.CreateTable(
                name: "redirect_rules",
                schema: "redirects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_path = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    to_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    hit_count = table.Column<long>(type: "bigint", nullable: false),
                    last_hit_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redirect_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_redirect_rules_to_path",
                schema: "redirects",
                table: "redirect_rules",
                column: "to_path");

            migrationBuilder.CreateIndex(
                name: "ux_redirect_rules_from_path_active",
                schema: "redirects",
                table: "redirect_rules",
                column: "from_path",
                unique: true,
                filter: "is_active = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redirect_rules",
                schema: "redirects");
        }
    }
}
