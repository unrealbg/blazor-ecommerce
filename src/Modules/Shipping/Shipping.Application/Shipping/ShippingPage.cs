namespace Shipping.Application.Shipping;

public sealed record ShippingPage<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<T> Items);
