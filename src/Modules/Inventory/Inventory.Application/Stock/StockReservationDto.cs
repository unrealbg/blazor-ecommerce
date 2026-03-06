namespace Inventory.Application.Stock;

public sealed record StockReservationDto(
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
