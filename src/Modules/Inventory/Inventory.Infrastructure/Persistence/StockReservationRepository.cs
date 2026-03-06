using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal sealed class StockReservationRepository(InventoryDbContext dbContext) : IStockReservationRepository
{
    public Task AddAsync(StockReservation reservation, CancellationToken cancellationToken)
    {
        return dbContext.StockReservations.AddAsync(reservation, cancellationToken).AsTask();
    }

    public Task<StockReservation?> GetActiveByCartProductSkuAsync(
        string cartId,
        Guid productId,
        string? sku,
        CancellationToken cancellationToken)
    {
        var normalizedCartId = NormalizeCartId(cartId);
        var normalizedSku = NormalizeSku(sku);
        var query = dbContext.StockReservations
            .Where(reservation => reservation.CartId == normalizedCartId &&
                                  reservation.ProductId == productId &&
                                  reservation.Status == StockReservationStatus.Active);

        if (normalizedSku is not null)
        {
            query = query.Where(reservation => reservation.Sku == normalizedSku);
        }

        return query
            .OrderByDescending(reservation => reservation.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<StockReservation?> GetActiveByOrderProductSkuAsync(
        Guid orderId,
        Guid productId,
        string? sku,
        CancellationToken cancellationToken)
    {
        var normalizedSku = NormalizeSku(sku);
        var query = dbContext.StockReservations
            .Where(reservation => reservation.OrderId == orderId &&
                                  reservation.ProductId == productId &&
                                  reservation.Status == StockReservationStatus.Active);

        if (normalizedSku is not null)
        {
            query = query.Where(reservation => reservation.Sku == normalizedSku);
        }

        return query
            .OrderByDescending(reservation => reservation.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockReservation>> ListActiveByCartIdAsync(
        string cartId,
        CancellationToken cancellationToken)
    {
        var normalizedCartId = NormalizeCartId(cartId);

        return await dbContext.StockReservations
            .Where(reservation => reservation.CartId == normalizedCartId &&
                                  reservation.Status == StockReservationStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockReservation>> ListActiveByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.StockReservations
            .Where(reservation => reservation.OrderId == orderId &&
                                  reservation.Status == StockReservationStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockReservation>> ListExpiredActiveAsync(
        DateTime utcNow,
        int take,
        CancellationToken cancellationToken)
    {
        var normalizedTake = take <= 0 ? 100 : Math.Min(1000, take);

        return await dbContext.StockReservations
            .Where(reservation => reservation.Status == StockReservationStatus.Active &&
                                  reservation.ExpiresAtUtc <= utcNow)
            .OrderBy(reservation => reservation.ExpiresAtUtc)
            .Take(normalizedTake)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyCollection<StockReservation> Items, long TotalCount)> ListActivePageAsync(
        Guid? productId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockReservations
            .Where(reservation => reservation.Status == StockReservationStatus.Active);

        if (productId is not null)
        {
            query = query.Where(reservation => reservation.ProductId == productId.Value);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderBy(reservation => reservation.ExpiresAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountActiveForProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        return dbContext.StockReservations.CountAsync(
            reservation => reservation.ProductId == productId &&
                           reservation.Status == StockReservationStatus.Active,
            cancellationToken);
    }

    public async Task<int> SumActiveQuantityForProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await dbContext.StockReservations
            .Where(reservation => reservation.ProductId == productId &&
                                  reservation.Status == StockReservationStatus.Active)
            .Select(reservation => (int?)reservation.Quantity)
            .SumAsync(cancellationToken) ?? 0;
    }

    private static string NormalizeCartId(string cartId)
    {
        return cartId.Trim();
    }

    private static string? NormalizeSku(string? sku)
    {
        return string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
    }
}
