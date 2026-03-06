using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAddressSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_city",
                schema: "orders",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_country",
                schema: "orders",
                table: "orders",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_first_name",
                schema: "orders",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_last_name",
                schema: "orders",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_phone",
                schema: "orders",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_postal_code",
                schema: "orders",
                table: "orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_street",
                schema: "orders",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_city",
                schema: "orders",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_country",
                schema: "orders",
                table: "orders",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_first_name",
                schema: "orders",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_last_name",
                schema: "orders",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_phone",
                schema: "orders",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_postal_code",
                schema: "orders",
                table: "orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_street",
                schema: "orders",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_city",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_country",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_first_name",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_last_name",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_phone",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_postal_code",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_street",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_city",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_country",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_first_name",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_last_name",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_phone",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_postal_code",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_street",
                schema: "orders",
                table: "orders");
        }
    }
}
