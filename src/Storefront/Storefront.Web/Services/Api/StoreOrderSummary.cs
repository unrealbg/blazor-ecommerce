namespace Storefront.Web.Services.Api;

public sealed record StoreOrderSummary(
    Guid Id,
    string CustomerId,
    string Currency,
    decimal SubtotalAmount,
    decimal ShippingPriceAmount,
    string ShippingCurrency,
    string ShippingMethodCode,
    string ShippingMethodName,
    decimal TotalAmount,
    string Status,
    string FulfillmentStatus,
    DateTime PlacedAtUtc,
    StoreOrderAddress ShippingAddress,
    StoreOrderAddress BillingAddress,
    IReadOnlyCollection<StoreOrderLine> Lines);
