namespace Cart.Application.Carts;

public sealed record CartDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime CreatedOnUtc,
    DateTime? CheckedOutOnUtc,
    string? Currency,
    decimal? TotalAmount);
