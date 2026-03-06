using Microsoft.EntityFrameworkCore;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class ShippingMethodRepository(ShippingDbContext dbContext) : IShippingMethodRepository
{
    public Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.ShippingMethods
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<ShippingMethod?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        return dbContext.ShippingMethods
            .SingleOrDefaultAsync(entity => entity.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShippingMethod>> ListAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.ShippingMethods.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(entity => entity.IsActive);
        }

        return await query
            .OrderBy(entity => entity.Priority)
            .ThenBy(entity => entity.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken)
    {
        return dbContext.ShippingMethods.AddAsync(shippingMethod, cancellationToken).AsTask();
    }
}
