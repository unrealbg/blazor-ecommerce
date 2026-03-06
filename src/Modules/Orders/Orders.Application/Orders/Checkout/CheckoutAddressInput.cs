namespace Orders.Application.Orders.Checkout;

public sealed record CheckoutAddressInput(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone);
