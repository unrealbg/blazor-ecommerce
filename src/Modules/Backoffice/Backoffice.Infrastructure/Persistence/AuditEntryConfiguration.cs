using Backoffice.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backoffice.Infrastructure.Persistence;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries", "audit");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(entry => entry.ActorUserId)
            .HasColumnName("actor_user_id")
            .HasMaxLength(64);

        builder.Property(entry => entry.ActorEmail)
            .HasColumnName("actor_email")
            .HasMaxLength(320);

        builder.Property(entry => entry.ActorDisplayName)
            .HasColumnName("actor_display_name")
            .HasMaxLength(160);

        builder.Property(entry => entry.ActionType)
            .HasColumnName("action_type")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(entry => entry.TargetType)
            .HasColumnName("target_type")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(entry => entry.TargetId)
            .HasColumnName("target_id")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(entry => entry.Summary)
            .HasColumnName("summary")
            .HasMaxLength(600)
            .IsRequired();

        builder.Property(entry => entry.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb");

        builder.Property(entry => entry.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(128);

        builder.Property(entry => entry.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(128);

        builder.HasIndex(entry => entry.OccurredAtUtc)
            .HasDatabaseName("ix_audit_entries_occurred_at_utc");

        builder.HasIndex(entry => entry.ActorUserId)
            .HasDatabaseName("ix_audit_entries_actor_user_id");

        builder.HasIndex(entry => new { entry.TargetType, entry.TargetId })
            .HasDatabaseName("ix_audit_entries_target");
    }
}
