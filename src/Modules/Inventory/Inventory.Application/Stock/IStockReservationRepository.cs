using Inventory.Domain.Stock;

namespace Inventory.Application.Stock;

public interface IStockReservationRepository
{
    Task AddAsync(StockReservation reservation, CancellationToken cancellationToken);

    Task<StockReservation?> GetActiveByCartVariantIdAsync(
        string cartId,
        Guid variantId,
        CancellationToken cancellationToken);

    Task<StockReservation?> GetActiveByOrderVariantIdAsync(
        Guid orderId,
        Guid variantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StockReservation>> ListActiveByCartIdAsync(
        string cartId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StockReservation>> ListActiveByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StockReservation>> ListExpiredActiveAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken);

    Task<(IReadOnlyCollection<StockReservation> Items, long TotalCount)> ListActivePageAsync(
        Guid? productId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountActiveForProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<int> SumActiveQuantityForProductAsync(Guid productId, CancellationToken cancellationToken);
}
