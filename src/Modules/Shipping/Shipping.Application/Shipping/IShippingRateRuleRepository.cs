using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping;

public interface IShippingRateRuleRepository
{
    Task<ShippingRateRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShippingRateRule>> ListByZoneAsync(
        Guid zoneId,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShippingRateRule>> ListAsync(bool activeOnly, CancellationToken cancellationToken);

    Task AddAsync(ShippingRateRule shippingRateRule, CancellationToken cancellationToken);
}
