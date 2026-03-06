using Microsoft.EntityFrameworkCore;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShipmentEventRepository(ShippingDbContext dbContext) : IShipmentEventRepository
{
    public async Task<IReadOnlyCollection<ShipmentEvent>> ListByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ShipmentEvents
            .Where(entity => entity.ShipmentId == shipmentId)
            .OrderBy(entity => entity.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ShipmentEvent shipmentEvent, CancellationToken cancellationToken)
    {
        return dbContext.ShipmentEvents.AddAsync(shipmentEvent, cancellationToken).AsTask();
    }
}
