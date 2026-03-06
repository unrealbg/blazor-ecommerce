namespace Storefront.Web.Services.Api;

public sealed record StoreStockMovementPage(
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyCollection<StoreStockMovement> Items);
