using Inventory.Domain.Stock;

namespace Inventory.Application.Stock;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement movement, CancellationToken cancellationToken);

    Task<(IReadOnlyCollection<StockMovement> Items, long TotalCount)> ListByProductPageAsync(
        Guid productId,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
