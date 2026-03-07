using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reviews.Domain.Reviews;

namespace Reviews.Infrastructure.Persistence;

internal sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("product_reviews");
        builder.HasKey(review => review.Id);

        builder.Property(review => review.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(review => review.VariantId)
            .HasColumnName("variant_id");

        builder.Property(review => review.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(review => review.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(review => review.Title)
            .HasColumnName("title")
            .HasMaxLength(160);

        builder.Property(review => review.Body)
            .HasColumnName("body")
            .HasMaxLength(4000);

        builder.Property(review => review.Rating)
            .HasColumnName("rating")
            .IsRequired();

        builder.Property(review => review.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(review => review.IsVerifiedPurchase)
            .HasColumnName("is_verified_purchase")
            .IsRequired();

        builder.Property(review => review.VerifiedPurchaseOrderId)
            .HasColumnName("verified_purchase_order_id");

        builder.Property(review => review.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(review => review.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(review => review.ApprovedAtUtc)
            .HasColumnName("approved_at_utc");

        builder.Property(review => review.RejectedAtUtc)
            .HasColumnName("rejected_at_utc");

        builder.Property(review => review.ModerationNotes)
            .HasColumnName("moderation_notes")
            .HasMaxLength(2000);

        builder.Property(review => review.HelpfulCount)
            .HasColumnName("helpful_count")
            .IsRequired();

        builder.Property(review => review.NotHelpfulCount)
            .HasColumnName("not_helpful_count")
            .IsRequired();

        builder.Property(review => review.ReportCount)
            .HasColumnName("report_count")
            .IsRequired();

        builder.Property(review => review.Source)
            .HasColumnName("source")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(review => new { review.ProductId, review.Status, review.CreatedAtUtc });
        builder.HasIndex(review => review.CustomerId);
        builder.HasIndex(review => review.Rating);
        builder.HasIndex(review => new { review.ProductId, review.CustomerId }).IsUnique();
    }
}
