using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Customers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customers");

            migrationBuilder.CreateTable(
                name: "customer_sessions",
                schema: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    first_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    last_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    first_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    last_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    company = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    street1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    street2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_default_shipping = table.Column<bool>(type: "boolean", nullable: false),
                    is_default_billing = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_addresses_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_addresses_default_billing",
                schema: "customers",
                table: "addresses",
                columns: new[] { "customer_id", "is_default_billing" },
                unique: true,
                filter: "is_default_billing = true");

            migrationBuilder.CreateIndex(
                name: "ux_addresses_default_shipping",
                schema: "customers",
                table: "addresses",
                columns: new[] { "customer_id", "is_default_shipping" },
                unique: true,
                filter: "is_default_shipping = true");

            migrationBuilder.CreateIndex(
                name: "ix_customer_sessions_customer_id",
                schema: "customers",
                table: "customer_sessions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ux_customer_sessions_session_id",
                schema: "customers",
                table: "customer_sessions",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customers_email",
                schema: "customers",
                table: "customers",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customers_normalized_email",
                schema: "customers",
                table: "customers",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customers_user_id",
                schema: "customers",
                table: "customers",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "customer_sessions",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "customers");
        }
    }
}
