namespace Orders.Application.Orders;

public sealed record OrderAddressDto(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone);
