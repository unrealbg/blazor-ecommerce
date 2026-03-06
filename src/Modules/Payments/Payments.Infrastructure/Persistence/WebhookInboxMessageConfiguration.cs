using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class WebhookInboxMessageConfiguration : IEntityTypeConfiguration<WebhookInboxMessage>
{
    public void Configure(EntityTypeBuilder<WebhookInboxMessage> builder)
    {
        builder.ToTable("webhook_inbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Provider)
            .HasColumnName("provider")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(message => message.ExternalEventId)
            .HasColumnName("external_event_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(message => message.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(message => message.ReceivedAtUtc)
            .HasColumnName("received_at_utc")
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.Property(message => message.ProcessingStatus)
            .HasColumnName("processing_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(message => message.Error)
            .HasColumnName("error")
            .HasColumnType("text");

        builder.HasIndex(message => new { message.Provider, message.ExternalEventId }).IsUnique();
    }
}
