namespace Storefront.Web.Services.Api;

public sealed record StoreStockReservation(
    Guid Id,
    Guid ProductId,
    string? Sku,
    string? CartId,
    Guid? CustomerId,
    Guid? OrderId,
    int Quantity,
    string Status,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string ReservationToken);
