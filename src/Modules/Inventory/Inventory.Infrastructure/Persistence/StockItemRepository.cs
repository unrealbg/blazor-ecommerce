using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class StockItemRepository(InventoryDbContext dbContext) : IStockItemRepository
{
    public Task AddAsync(StockItem stockItem, CancellationToken cancellationToken)
    {
        return dbContext.StockItems.AddAsync(stockItem, cancellationToken).AsTask();
    }

    public Task<StockItem?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return dbContext.StockItems
            .OrderByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefaultAsync(item => item.VariantId == variantId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockItem>> ListByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return [];
        }

        return await dbContext.StockItems
            .Where(item => variantIds.Contains(item.VariantId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockItem>> ListByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        return await dbContext.StockItems
            .Where(item => productIds.Contains(item.ProductId))
            .ToListAsync(cancellationToken);
    }
}
