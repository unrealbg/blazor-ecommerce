using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.UserId)
            .HasColumnName("user_id");

        builder.Property(customer => customer.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(customer => customer.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(customer => customer.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(120);

        builder.Property(customer => customer.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(120);

        builder.Property(customer => customer.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(64);

        builder.Property(customer => customer.IsEmailVerified)
            .HasColumnName("is_email_verified")
            .IsRequired();

        builder.Property(customer => customer.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(customer => customer.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(customer => customer.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(customer => customer.Email)
            .IsUnique()
            .HasDatabaseName("ux_customers_email");

        builder.HasIndex(customer => customer.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ux_customers_normalized_email");

        builder.HasIndex(customer => customer.UserId)
            .IsUnique()
            .HasDatabaseName("ux_customers_user_id");

        builder.OwnsMany(customer => customer.Addresses, addresses =>
        {
            addresses.ToTable("addresses");
            addresses.WithOwner().HasForeignKey(address => address.CustomerId);
            addresses.HasKey(address => address.Id);

            addresses.Property(address => address.Id)
                .HasColumnName("id");

            addresses.Property(address => address.CustomerId)
                .HasColumnName("customer_id")
                .IsRequired();

            addresses.Property(address => address.Label)
                .HasColumnName("label")
                .HasMaxLength(80)
                .IsRequired();

            addresses.Property(address => address.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(120)
                .IsRequired();

            addresses.Property(address => address.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(120)
                .IsRequired();

            addresses.Property(address => address.Company)
                .HasColumnName("company")
                .HasMaxLength(180);

            addresses.Property(address => address.Street1)
                .HasColumnName("street1")
                .HasMaxLength(200)
                .IsRequired();

            addresses.Property(address => address.Street2)
                .HasColumnName("street2")
                .HasMaxLength(200);

            addresses.Property(address => address.City)
                .HasColumnName("city")
                .HasMaxLength(120)
                .IsRequired();

            addresses.Property(address => address.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(40)
                .IsRequired();

            addresses.Property(address => address.CountryCode)
                .HasColumnName("country_code")
                .HasMaxLength(2)
                .IsRequired();

            addresses.Property(address => address.Phone)
                .HasColumnName("phone")
                .HasMaxLength(64);

            addresses.Property(address => address.IsDefaultShipping)
                .HasColumnName("is_default_shipping")
                .IsRequired();

            addresses.Property(address => address.IsDefaultBilling)
                .HasColumnName("is_default_billing")
                .IsRequired();

            addresses.Property(address => address.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            addresses.Property(address => address.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .IsRequired();

            addresses.HasIndex(address => new { address.CustomerId, address.IsDefaultShipping })
                .HasDatabaseName("ux_addresses_default_shipping")
                .IsUnique()
                .HasFilter("is_default_shipping = true");

            addresses.HasIndex(address => new { address.CustomerId, address.IsDefaultBilling })
                .HasDatabaseName("ux_addresses_default_billing")
                .IsUnique()
                .HasFilter("is_default_billing = true");
        });

        builder.Navigation(customer => customer.Addresses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
