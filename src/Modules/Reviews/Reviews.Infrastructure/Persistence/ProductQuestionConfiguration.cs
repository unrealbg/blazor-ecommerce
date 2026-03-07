using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reviews.Domain.Questions;
using Reviews.Domain.Reviews;

namespace Reviews.Infrastructure.Persistence;

internal sealed class ProductQuestionConfiguration : IEntityTypeConfiguration<ProductQuestion>
{
    public void Configure(EntityTypeBuilder<ProductQuestion> builder)
    {
        builder.ToTable("product_questions");
        builder.HasKey(question => question.Id);

        builder.Property(question => question.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(question => question.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(question => question.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(question => question.QuestionText)
            .HasColumnName("question_text")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(question => question.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(question => question.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(question => question.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(question => question.ApprovedAtUtc)
            .HasColumnName("approved_at_utc");

        builder.Property(question => question.ModerationNotes)
            .HasColumnName("moderation_notes")
            .HasMaxLength(2000);

        builder.Property(question => question.AnswerCount)
            .HasColumnName("answer_count")
            .IsRequired();

        builder.Property(question => question.ReportCount)
            .HasColumnName("report_count")
            .IsRequired();

        builder.HasMany(question => question.Answers)
            .WithOne()
            .HasForeignKey(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(question => question.Answers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(question => new { question.ProductId, question.Status, question.CreatedAtUtc });
        builder.HasIndex(question => question.CustomerId);
    }
}
