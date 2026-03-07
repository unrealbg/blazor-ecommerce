using BuildingBlocks.Application.Abstractions;

namespace Inventory.Application.Stock.GetProductInventory;

public sealed class GetProductInventoryQueryHandler(
    IStockItemRepository stockItemRepository,
    IStockReservationRepository stockReservationRepository)
    : IQueryHandler<GetProductInventoryQuery, InventoryProductDetailsDto?>
{
    public async Task<InventoryProductDetailsDto?> Handle(
        GetProductInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var stockItems = await stockItemRepository.ListByProductIdsAsync([request.ProductId], cancellationToken);
        var stockItem = stockItems
            .OrderByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefault();
        if (stockItem is null)
        {
            return null;
        }

        var activeCount = await stockReservationRepository.CountActiveForProductAsync(
            request.ProductId,
            cancellationToken);
        var activeQuantity = await stockReservationRepository.SumActiveQuantityForProductAsync(
            request.ProductId,
            cancellationToken);

        return new InventoryProductDetailsDto(
            new StockItemSummaryDto(
                stockItem.Id,
                stockItem.ProductId,
                stockItem.Sku,
                stockItem.OnHandQuantity,
                stockItem.ReservedQuantity,
                stockItem.AvailableQuantity,
                stockItem.IsTracked,
                stockItem.AllowBackorder,
                stockItem.IsInStock,
                stockItem.CreatedAtUtc,
                stockItem.UpdatedAtUtc),
            activeCount,
            activeQuantity);
    }
}
