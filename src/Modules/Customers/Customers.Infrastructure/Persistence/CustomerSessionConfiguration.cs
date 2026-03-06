using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence;

internal sealed class CustomerSessionConfiguration : IEntityTypeConfiguration<CustomerSession>
{
    public void Configure(EntityTypeBuilder<CustomerSession> builder)
    {
        builder.ToTable("customer_sessions");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(session => session.CustomerId)
            .HasColumnName("customer_id");

        builder.Property(session => session.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(session => session.LastSeenUtc)
            .HasColumnName("last_seen_utc")
            .IsRequired();

        builder.HasIndex(session => session.SessionId)
            .IsUnique()
            .HasDatabaseName("ux_customer_sessions_session_id");

        builder.HasIndex(session => session.CustomerId)
            .HasDatabaseName("ix_customer_sessions_customer_id");
    }
}
