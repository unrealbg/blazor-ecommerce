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
        var stockItems = await stockItemRepository.ListByProductIdsAsync([productId], cancellationToken);
        var stockItem = stockItems
            .OrderByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefault();
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

    public async Task<InventoryAvailabilitySnapshot?> GetByVariantIdAsync(
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var stockItem = await stockItemRepository.GetByVariantIdAsync(variantId, cancellationToken);
        return stockItem is null ? null : Map(stockItem);
    }

    public async Task<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>> GetByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        var stockItems = await stockItemRepository.ListByVariantIdsAsync(variantIds, cancellationToken);

        return stockItems.ToDictionary(item => item.VariantId, Map);
    }

    private static InventoryAvailabilitySnapshot Map(Inventory.Domain.Stock.StockItem stockItem)
    {
        return new InventoryAvailabilitySnapshot(
            stockItem.ProductId,
            stockItem.VariantId,
            stockItem.Sku,
            stockItem.IsTracked,
            stockItem.AllowBackorder,
            stockItem.OnHandQuantity,
            stockItem.ReservedQuantity,
            stockItem.AvailableQuantity,
            stockItem.IsInStock);
    }
}
