using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping;

public interface IShipmentEventRepository
{
    Task<IReadOnlyCollection<ShipmentEvent>> ListByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken);

    Task AddAsync(ShipmentEvent shipmentEvent, CancellationToken cancellationToken);
}
