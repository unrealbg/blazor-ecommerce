using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reviews.Domain.Reports;

namespace Reviews.Infrastructure.Persistence;

internal sealed class ReviewReportConfiguration : IEntityTypeConfiguration<ReviewReport>
{
    public void Configure(EntityTypeBuilder<ReviewReport> builder)
    {
        builder.ToTable("review_reports");
        builder.HasKey(report => report.Id);

        builder.Property(report => report.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(report => report.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(report => report.ReasonType)
            .HasColumnName("reason_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(report => report.Message)
            .HasColumnName("message")
            .HasMaxLength(2000);

        builder.Property(report => report.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(report => report.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(report => report.ResolvedAtUtc)
            .HasColumnName("resolved_at_utc");

        builder.Property(report => report.ResolutionNotes)
            .HasColumnName("resolution_notes")
            .HasMaxLength(2000);

        builder.HasIndex(report => new { report.ReviewId, report.Status });
    }
}
