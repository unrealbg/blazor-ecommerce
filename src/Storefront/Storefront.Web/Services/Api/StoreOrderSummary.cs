namespace Storefront.Web.Services.Api;

public sealed record StoreOrderSummary(
    Guid Id,
    string CustomerId,
    string Currency,
    decimal SubtotalAmount,
    decimal TotalAmount,
    string Status,
    DateTime PlacedAtUtc,
    StoreOrderAddress ShippingAddress,
    StoreOrderAddress BillingAddress,
    IReadOnlyCollection<StoreOrderLine> Lines);
