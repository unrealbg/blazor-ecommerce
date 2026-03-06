using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.CreateTable(
                name: "payment_idempotency_records",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_intents",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_payment_intent_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    client_secret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_intents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_transaction_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    raw_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metadata_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_inbox_messages",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_inbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_operation_idempotency_key",
                schema: "payments",
                table: "payment_idempotency_records",
                columns: new[] { "operation", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_idempotency_records_payment_intent_id",
                schema: "payments",
                table: "payment_idempotency_records",
                column: "payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_created_at_utc",
                schema: "payments",
                table: "payment_intents",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_order_id",
                schema: "payments",
                table: "payment_intents",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_provider_provider_payment_intent_id",
                schema: "payments",
                table: "payment_intents",
                columns: new[] { "provider", "provider_payment_intent_id" },
                unique: true,
                filter: "provider_payment_intent_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_status",
                schema: "payments",
                table: "payment_intents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_created_at_utc",
                schema: "payments",
                table: "payment_transactions",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_payment_intent_id",
                schema: "payments",
                table: "payment_transactions",
                column: "payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_inbox_messages_provider_external_event_id",
                schema: "payments",
                table: "webhook_inbox_messages",
                columns: new[] { "provider", "external_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_idempotency_records",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payment_intents",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payment_transactions",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "webhook_inbox_messages",
                schema: "payments");
        }
    }
}
