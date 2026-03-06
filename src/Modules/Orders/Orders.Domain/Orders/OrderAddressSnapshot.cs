using BuildingBlocks.Domain.Results;

namespace Orders.Domain.Orders;

public sealed class OrderAddressSnapshot
{
    private OrderAddressSnapshot()
    {
    }

    private OrderAddressSnapshot(
        string firstName,
        string lastName,
        string street,
        string city,
        string postalCode,
        string country,
        string? phone)
    {
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
        Phone = phone;
    }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string PostalCode { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public static Result<OrderAddressSnapshot> Create(
        string firstName,
        string lastName,
        string street,
        string city,
        string postalCode,
        string country,
        string? phone)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return Result<OrderAddressSnapshot>.Failure(
                new Error("orders.address.name.required", "Address first and last names are required."));
        }

        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(city))
        {
            return Result<OrderAddressSnapshot>.Failure(
                new Error("orders.address.location.required", "Address street and city are required."));
        }

        if (string.IsNullOrWhiteSpace(postalCode) || string.IsNullOrWhiteSpace(country))
        {
            return Result<OrderAddressSnapshot>.Failure(
                new Error("orders.address.postal_country.required", "Address postal code and country are required."));
        }

        return Result<OrderAddressSnapshot>.Success(new OrderAddressSnapshot(
            firstName.Trim(),
            lastName.Trim(),
            street.Trim(),
            city.Trim(),
            postalCode.Trim(),
            country.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()));
    }

    public static OrderAddressSnapshot Empty()
    {
        return new OrderAddressSnapshot(
            firstName: "N/A",
            lastName: "N/A",
            street: "N/A",
            city: "N/A",
            postalCode: "N/A",
            country: "NA",
            phone: null);
    }
}
