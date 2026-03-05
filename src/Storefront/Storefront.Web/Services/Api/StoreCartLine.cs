namespace Storefront.Web.Services.Api;

public sealed record StoreCartLine(
    Guid ProductId,
    string ProductName,
    string Currency,
    decimal Amount,
    int Quantity);
