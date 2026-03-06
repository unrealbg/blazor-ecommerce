using Microsoft.EntityFrameworkCore;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShipmentRepository(ShippingDbContext dbContext) : IShipmentRepository
{
    public Task<Shipment?> GetByIdAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        return dbContext.Shipments
            .SingleOrDefaultAsync(entity => entity.Id == shipmentId, cancellationToken);
    }

    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return dbContext.Shipments
            .SingleOrDefaultAsync(entity => entity.OrderId == orderId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Shipment>> ListAsync(
        string? status,
        Guid? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(status, orderId);
        var skip = Math.Max(0, (page - 1) * pageSize);
        return await query
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? status, Guid? orderId, CancellationToken cancellationToken)
    {
        return ApplyFilters(status, orderId).CountAsync(cancellationToken);
    }

    public Task AddAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        return dbContext.Shipments.AddAsync(shipment, cancellationToken).AsTask();
    }

    private IQueryable<Shipment> ApplyFilters(string? status, Guid? orderId)
    {
        var query = dbContext.Shipments.AsQueryable();
        if (orderId is not null && orderId.Value != Guid.Empty)
        {
            query = query.Where(entity => entity.OrderId == orderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ShipmentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(entity => entity.Status == parsedStatus);
        }

        return query;
    }
}
