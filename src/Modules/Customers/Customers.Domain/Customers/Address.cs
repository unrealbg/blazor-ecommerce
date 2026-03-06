using BuildingBlocks.Domain.Primitives;

namespace Customers.Domain.Customers;

public sealed class Address : Entity<Guid>
{
    private Address()
    {
    }

    private Address(Guid id, Guid customerId, AddressData data)
    {
        Id = id;
        CustomerId = customerId;
        Apply(data);
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }

    public string Label { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? Company { get; private set; }

    public string Street1 { get; private set; } = string.Empty;

    public string? Street2 { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string PostalCode { get; private set; } = string.Empty;

    public string CountryCode { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public bool IsDefaultShipping { get; private set; }

    public bool IsDefaultBilling { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Address Create(Guid customerId, AddressData data)
    {
        return new Address(Guid.NewGuid(), customerId, data);
    }

    public void Update(AddressData data)
    {
        Apply(data);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDefaultShipping(bool isDefault)
    {
        IsDefaultShipping = isDefault;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDefaultBilling(bool isDefault)
    {
        IsDefaultBilling = isDefault;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void Apply(AddressData data)
    {
        Label = data.Label.Trim();
        FirstName = data.FirstName.Trim();
        LastName = data.LastName.Trim();
        Company = string.IsNullOrWhiteSpace(data.Company) ? null : data.Company.Trim();
        Street1 = data.Street1.Trim();
        Street2 = string.IsNullOrWhiteSpace(data.Street2) ? null : data.Street2.Trim();
        City = data.City.Trim();
        PostalCode = data.PostalCode.Trim();
        CountryCode = data.CountryCode.Trim().ToUpperInvariant();
        Phone = string.IsNullOrWhiteSpace(data.Phone) ? null : data.Phone.Trim();
        IsDefaultShipping = data.IsDefaultShipping;
        IsDefaultBilling = data.IsDefaultBilling;
    }
}
