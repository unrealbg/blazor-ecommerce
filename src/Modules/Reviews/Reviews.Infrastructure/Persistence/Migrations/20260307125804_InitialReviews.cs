using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reviews.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reviews");

            migrationBuilder.CreateTable(
                name: "product_questions",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    question_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderation_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    answer_count = table.Column<int>(type: "integer", nullable: false),
                    report_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_verified_purchase = table.Column<bool>(type: "boolean", nullable: false),
                    verified_purchase_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderation_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    helpful_count = table.Column<int>(type: "integer", nullable: false),
                    not_helpful_count = table.Column<int>(type: "integer", nullable: false),
                    report_count = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "review_aggregate_snapshots",
                schema: "reviews",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_review_count = table.Column<int>(type: "integer", nullable: false),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    five_star_count = table.Column<int>(type: "integer", nullable: false),
                    four_star_count = table.Column<int>(type: "integer", nullable: false),
                    three_star_count = table.Column<int>(type: "integer", nullable: false),
                    two_star_count = table.Column<int>(type: "integer", nullable: false),
                    one_star_count = table.Column<int>(type: "integer", nullable: false),
                    last_updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_aggregate_snapshots", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "review_reports",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "review_votes",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vote_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_votes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_answers",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_by_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    answer_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_official_answer = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderation_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_answers_product_questions_question_id",
                        column: x => x.question_id,
                        principalSchema: "reviews",
                        principalTable: "product_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_answers_question_id_status",
                schema: "reviews",
                table: "product_answers",
                columns: new[] { "question_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_product_questions_customer_id",
                schema: "reviews",
                table: "product_questions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_questions_product_id_status_created_at_utc",
                schema: "reviews",
                table: "product_questions",
                columns: new[] { "product_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_customer_id",
                schema: "reviews",
                table: "product_reviews",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_product_id_customer_id",
                schema: "reviews",
                table: "product_reviews",
                columns: new[] { "product_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_product_id_status_created_at_utc",
                schema: "reviews",
                table: "product_reviews",
                columns: new[] { "product_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_rating",
                schema: "reviews",
                table: "product_reviews",
                column: "rating");

            migrationBuilder.CreateIndex(
                name: "IX_review_reports_review_id_status",
                schema: "reviews",
                table: "review_reports",
                columns: new[] { "review_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_review_votes_review_id_customer_id",
                schema: "reviews",
                table: "review_votes",
                columns: new[] { "review_id", "customer_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_answers",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "product_reviews",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "review_aggregate_snapshots",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "review_reports",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "review_votes",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "product_questions",
                schema: "reviews");
        }
    }
}
