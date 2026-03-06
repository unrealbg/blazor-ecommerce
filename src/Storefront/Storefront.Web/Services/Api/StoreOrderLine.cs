namespace Storefront.Web.Services.Api;

public sealed record StoreOrderLine(
    Guid ProductId,
    string Name,
    string Currency,
    decimal UnitAmount,
    int Quantity);
