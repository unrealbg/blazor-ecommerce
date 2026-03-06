using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping;

public interface IShippingMethodRepository
{
    Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ShippingMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShippingMethod>> ListAsync(bool activeOnly, CancellationToken cancellationToken);

    Task AddAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken);
}
