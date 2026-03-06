using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Shipment>> ListAsync(
        string? status,
        Guid? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(string? status, Guid? orderId, CancellationToken cancellationToken);

    Task AddAsync(Shipment shipment, CancellationToken cancellationToken);
}
