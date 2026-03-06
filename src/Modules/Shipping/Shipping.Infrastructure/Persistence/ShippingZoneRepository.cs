using Microsoft.EntityFrameworkCore;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShippingZoneRepository(ShippingDbContext dbContext) : IShippingZoneRepository
{
    public Task<ShippingZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.ShippingZones
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<ShippingZone?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        return dbContext.ShippingZones
            .SingleOrDefaultAsync(entity => entity.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShippingZone>> ListAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.ShippingZones.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(entity => entity.IsActive);
        }

        return await query
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ShippingZone shippingZone, CancellationToken cancellationToken)
    {
        return dbContext.ShippingZones.AddAsync(shippingZone, cancellationToken).AsTask();
    }
}
