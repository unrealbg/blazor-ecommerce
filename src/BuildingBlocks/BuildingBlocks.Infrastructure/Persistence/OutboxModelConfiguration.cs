using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence;

internal static class OutboxModelConfiguration
{
    public static void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(message => message.Payload)
            .IsRequired();

        builder.Property(message => message.OccurredOnUtc)
            .IsRequired();

        builder.Property(message => message.Error)
            .HasMaxLength(4000);

        builder.HasIndex(message => message.ProcessedOnUtc);
    }
}
