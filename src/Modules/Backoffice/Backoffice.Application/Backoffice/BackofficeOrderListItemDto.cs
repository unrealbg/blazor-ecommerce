namespace Backoffice.Application.Backoffice;

public sealed record BackofficeOrderListItemDto(
    Guid Id,
    string CustomerId,
    string? CustomerEmail,
    string? CustomerName,
    string Status,
    string PaymentStatus,
    string FulfillmentStatus,
    decimal TotalAmount,
    string Currency,
    string ShippingMethodName,
    int LineCount,
    DateTime PlacedAtUtc);
