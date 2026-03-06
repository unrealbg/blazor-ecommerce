using Inventory.Domain.Stock;

namespace Inventory.Application.Stock;

public interface IStockItemRepository
{
    Task AddAsync(StockItem stockItem, CancellationToken cancellationToken);

    Task<StockItem?> GetByProductAndSkuAsync(Guid productId, string? sku, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StockItem>> ListByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
