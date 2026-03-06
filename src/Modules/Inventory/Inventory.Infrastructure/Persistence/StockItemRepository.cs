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

    public Task<StockItem?> GetByProductAndSkuAsync(Guid productId, string? sku, CancellationToken cancellationToken)
    {
        var normalizedSku = NormalizeSku(sku);
        var query = dbContext.StockItems.Where(item => item.ProductId == productId);
        if (normalizedSku is not null)
        {
            query = query.Where(item => item.Sku == normalizedSku);
        }

        return query
            .OrderByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
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

    private static string? NormalizeSku(string? sku)
    {
        return string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
    }
}
