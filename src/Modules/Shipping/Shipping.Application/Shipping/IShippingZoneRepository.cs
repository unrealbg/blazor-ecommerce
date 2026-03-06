using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping;

public interface IShippingZoneRepository
{
    Task<ShippingZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ShippingZone?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShippingZone>> ListAsync(bool activeOnly, CancellationToken cancellationToken);

    Task AddAsync(ShippingZone shippingZone, CancellationToken cancellationToken);
}
