using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderAuditConfiguration : IEntityTypeConfiguration<OrderAudit>
{
    public void Configure(EntityTypeBuilder<OrderAudit> builder)
    {
        builder.ToTable("order_audits");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.EventId)
            .IsRequired();

        builder.Property(audit => audit.OrderId)
            .IsRequired();

        builder.Property(audit => audit.CustomerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(audit => audit.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(audit => audit.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(audit => audit.LoggedOnUtc)
            .IsRequired();

        builder.HasIndex(audit => audit.EventId)
            .IsUnique();
    }
}
