namespace Storefront.Web.Services.Api;

public sealed record StoreOrderAddress(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone);
