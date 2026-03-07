using Inventory.Domain.Stock;

namespace Inventory.Application.Stock;

public interface IStockItemRepository
{
    Task AddAsync(StockItem stockItem, CancellationToken cancellationToken);

    Task<StockItem?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StockItem>> ListByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StockItem>> ListByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
