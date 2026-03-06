namespace Storefront.Web.Services.Api;

public sealed record StoreInventoryProductDetails(
    StoreStockItemSummary StockItem,
    int ActiveReservationCount,
    int ActiveReservedQuantity);
