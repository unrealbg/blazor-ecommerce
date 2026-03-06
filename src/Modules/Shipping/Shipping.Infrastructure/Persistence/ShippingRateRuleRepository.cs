using Microsoft.EntityFrameworkCore;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShippingRateRuleRepository(ShippingDbContext dbContext) : IShippingRateRuleRepository
{
    public Task<ShippingRateRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.ShippingRateRules
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShippingRateRule>> ListByZoneAsync(
        Guid zoneId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ShippingRateRules
            .Where(entity => entity.ShippingZoneId == zoneId);

        if (activeOnly)
        {
            query = query.Where(entity => entity.IsActive);
        }

        return await query
            .OrderBy(entity => entity.ShippingMethodId)
            .ThenBy(entity => entity.MinOrderAmount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShippingRateRule>> ListAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.ShippingRateRules.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(entity => entity.IsActive);
        }

        return await query
            .OrderBy(entity => entity.ShippingMethodId)
            .ThenBy(entity => entity.ShippingZoneId)
            .ThenBy(entity => entity.MinOrderAmount)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ShippingRateRule shippingRateRule, CancellationToken cancellationToken)
    {
        return dbContext.ShippingRateRules.AddAsync(shippingRateRule, cancellationToken).AsTask();
    }
}
