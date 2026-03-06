namespace Storefront.Web.Services.Api;

public sealed record StoreShipmentPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<StoreShipment> Items);
