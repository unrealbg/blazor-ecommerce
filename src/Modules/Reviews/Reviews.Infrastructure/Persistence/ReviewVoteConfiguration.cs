using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reviews.Domain.Reviews;

namespace Reviews.Infrastructure.Persistence;

internal sealed class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.ToTable("review_votes");
        builder.HasKey(vote => vote.Id);

        builder.Property(vote => vote.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(vote => vote.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(vote => vote.VoteType)
            .HasColumnName("vote_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(vote => vote.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(vote => vote.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(vote => new { vote.ReviewId, vote.CustomerId }).IsUnique();
    }
}
