using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reviews.Domain.Reviews;

namespace Reviews.Infrastructure.Persistence;

internal sealed class ReviewAggregateSnapshotConfiguration : IEntityTypeConfiguration<ReviewAggregateSnapshot>
{
    public void Configure(EntityTypeBuilder<ReviewAggregateSnapshot> builder)
    {
        builder.ToTable("review_aggregate_snapshots");
        builder.HasKey(snapshot => snapshot.ProductId);

        builder.Property(snapshot => snapshot.Id)
            .HasColumnName("id");

        builder.Property(snapshot => snapshot.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(snapshot => snapshot.ApprovedReviewCount)
            .HasColumnName("approved_review_count")
            .IsRequired();

        builder.Property(snapshot => snapshot.AverageRating)
            .HasColumnName("average_rating")
            .HasPrecision(3, 2)
            .IsRequired();

        builder.Property(snapshot => snapshot.FiveStarCount)
            .HasColumnName("five_star_count")
            .IsRequired();

        builder.Property(snapshot => snapshot.FourStarCount)
            .HasColumnName("four_star_count")
            .IsRequired();

        builder.Property(snapshot => snapshot.ThreeStarCount)
            .HasColumnName("three_star_count")
            .IsRequired();

        builder.Property(snapshot => snapshot.TwoStarCount)
            .HasColumnName("two_star_count")
            .IsRequired();

        builder.Property(snapshot => snapshot.OneStarCount)
            .HasColumnName("one_star_count")
            .IsRequired();

        builder.Property(snapshot => snapshot.LastUpdatedAtUtc)
            .HasColumnName("last_updated_at_utc")
            .IsRequired();
    }
}
