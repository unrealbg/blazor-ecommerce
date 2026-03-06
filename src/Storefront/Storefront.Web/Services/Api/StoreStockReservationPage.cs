namespace Storefront.Web.Services.Api;

public sealed record StoreStockReservationPage(
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyCollection<StoreStockReservation> Items);
