using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backoffice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBackofficeAuditAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "backoffice");

            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    actor_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    actor_display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    action_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    target_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    target_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    summary = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "order_internal_notes",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    author_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    author_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    author_display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_internal_notes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_actor_user_id",
                schema: "audit",
                table: "audit_entries",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_occurred_at_utc",
                schema: "audit",
                table: "audit_entries",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_target",
                schema: "audit",
                table: "audit_entries",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_order_internal_notes_order_created",
                schema: "backoffice",
                table: "order_internal_notes",
                columns: new[] { "order_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "order_internal_notes",
                schema: "backoffice");
        }
    }
}
