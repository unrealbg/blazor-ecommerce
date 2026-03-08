using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Customers.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityStaffProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "department",
                schema: "identity",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "identity",
                table: "users",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_staff",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login_at_utc",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_staff_active",
                schema: "identity",
                table: "users",
                columns: new[] { "is_staff", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_staff_active",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "department",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_staff",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_login_at_utc",
                schema: "identity",
                table: "users");
        }
    }
}
