using BuildingBlocks.Application.Contracts;
using Inventory.Application.Stock;

namespace Inventory.Infrastructure.Stock;

internal sealed class InventoryAvailabilityReader(IStockItemRepository stockItemRepository)
    : IInventoryAvailabilityReader
{
    public async Task<InventoryAvailabilitySnapshot?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var stockItem = await stockItemRepository.GetByProductAndSkuAsync(
            productId,
            sku: null,
            cancellationToken);
        if (stockItem is null)
        {
            return null;
        }

        return Map(stockItem);
    }

    public async Task<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var stockItems = await stockItemRepository.ListByProductIdsAsync(productIds, cancellationToken);

        return stockItems
            .GroupBy(item => item.ProductId)
            .Select(group => group.First())
            .ToDictionary(item => item.ProductId, Map);
    }

    private static InventoryAvailabilitySnapshot Map(Inventory.Domain.Stock.StockItem stockItem)
    {
        return new InventoryAvailabilitySnapshot(
            stockItem.ProductId,
            stockItem.Sku,
            stockItem.IsTracked,
            stockItem.AllowBackorder,
            stockItem.OnHandQuantity,
            stockItem.ReservedQuantity,
            stockItem.AvailableQuantity,
            stockItem.IsInStock);
    }
}
