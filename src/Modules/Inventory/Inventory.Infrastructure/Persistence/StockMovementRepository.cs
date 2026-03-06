using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class StockMovementRepository(InventoryDbContext dbContext) : IStockMovementRepository
{
    public Task AddAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        return dbContext.StockMovements.AddAsync(movement, cancellationToken).AsTask();
    }

    public async Task<(IReadOnlyCollection<StockMovement> Items, long TotalCount)> ListByProductPageAsync(
        Guid productId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockMovements
            .Where(movement => movement.ProductId == productId);

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
