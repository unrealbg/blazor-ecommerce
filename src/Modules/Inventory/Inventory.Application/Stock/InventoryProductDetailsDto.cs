namespace Inventory.Application.Stock;

public sealed record InventoryProductDetailsDto(
    StockItemSummaryDto StockItem,
    int ActiveReservationCount,
    int ActiveReservedQuantity);
