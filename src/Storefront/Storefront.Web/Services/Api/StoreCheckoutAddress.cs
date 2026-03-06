namespace Storefront.Web.Services.Api;

public sealed record StoreCheckoutAddress(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone);
