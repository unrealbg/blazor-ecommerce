using Backoffice.Domain.Notes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backoffice.Infrastructure.Persistence;

internal sealed class OrderInternalNoteConfiguration : IEntityTypeConfiguration<OrderInternalNote>
{
    public void Configure(EntityTypeBuilder<OrderInternalNote> builder)
    {
        builder.ToTable("order_internal_notes");
        builder.HasKey(note => note.Id);

        builder.Property(note => note.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(note => note.Note)
            .HasColumnName("note")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(note => note.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(note => note.AuthorUserId)
            .HasColumnName("author_user_id")
            .HasMaxLength(64);

        builder.Property(note => note.AuthorEmail)
            .HasColumnName("author_email")
            .HasMaxLength(320);

        builder.Property(note => note.AuthorDisplayName)
            .HasColumnName("author_display_name")
            .HasMaxLength(160);

        builder.HasIndex(note => new { note.OrderId, note.CreatedAtUtc })
            .HasDatabaseName("ix_order_internal_notes_order_created");
    }
}
