using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reviews.Domain.Questions;
using Reviews.Domain.Reviews;

namespace Reviews.Infrastructure.Persistence;

internal sealed class ProductAnswerConfiguration : IEntityTypeConfiguration<ProductAnswer>
{
    public void Configure(EntityTypeBuilder<ProductAnswer> builder)
    {
        builder.ToTable("product_answers");
        builder.HasKey(answer => answer.Id);

        builder.Property(answer => answer.QuestionId)
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(answer => answer.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(answer => answer.AnsweredByType)
            .HasColumnName("answered_by_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(answer => answer.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(answer => answer.AnswerText)
            .HasColumnName("answer_text")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(answer => answer.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(answer => answer.IsOfficialAnswer)
            .HasColumnName("is_official_answer")
            .IsRequired();

        builder.Property(answer => answer.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(answer => answer.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(answer => answer.ApprovedAtUtc)
            .HasColumnName("approved_at_utc");

        builder.Property(answer => answer.ModerationNotes)
            .HasColumnName("moderation_notes")
            .HasMaxLength(2000);

        builder.HasIndex(answer => new { answer.QuestionId, answer.Status });
    }
}
