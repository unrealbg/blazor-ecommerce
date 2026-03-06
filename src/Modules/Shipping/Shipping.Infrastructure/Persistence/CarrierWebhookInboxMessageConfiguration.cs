using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class CarrierWebhookInboxMessageConfiguration : IEntityTypeConfiguration<CarrierWebhookInboxMessage>
{
    public void Configure(EntityTypeBuilder<CarrierWebhookInboxMessage> builder)
    {
        builder.ToTable("carrier_webhook_inbox_messages");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Provider)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.ExternalEventId)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.EventType)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.Payload)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(entity => entity.ProcessingStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(entity => entity.Error)
            .HasMaxLength(2000);

        builder.HasIndex(entity => new { entity.Provider, entity.ExternalEventId })
            .IsUnique();
    }
}
