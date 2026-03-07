using Microsoft.EntityFrameworkCore;
using Pricing.Application.Pricing;
using Pricing.Domain.VariantPrices;

namespace Pricing.Infrastructure.Persistence;

internal sealed class VariantPriceRepository(PricingDbContext dbContext) : IVariantPriceRepository
{
    public Task AddAsync(VariantPrice variantPrice, CancellationToken cancellationToken)
    {
        return dbContext.VariantPrices.AddAsync(variantPrice, cancellationToken).AsTask();
    }

    public Task<VariantPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.VariantPrices.SingleOrDefaultAsync(variantPrice => variantPrice.Id == id, cancellationToken);
    }

    public Task<VariantPrice?> GetActiveForVariantAsync(
        Guid priceListId,
        Guid variantId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return QueryActive(priceListId, utcNow)
            .Where(variantPrice => variantPrice.VariantId == variantId)
            .OrderByDescending(variantPrice => variantPrice.ValidFromUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, VariantPrice>> GetActiveForVariantsAsync(
        Guid priceListId,
        IReadOnlyCollection<Guid> variantIds,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, VariantPrice>();
        }

        var prices = await QueryActive(priceListId, utcNow)
            .Where(variantPrice => variantIds.Contains(variantPrice.VariantId))
            .OrderByDescending(variantPrice => variantPrice.ValidFromUtc)
            .ToListAsync(cancellationToken);

        return prices
            .GroupBy(variantPrice => variantPrice.VariantId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private IQueryable<VariantPrice> QueryActive(Guid priceListId, DateTime utcNow)
    {
        return dbContext.VariantPrices
            .Where(variantPrice =>
                variantPrice.PriceListId == priceListId &&
                variantPrice.IsActive &&
                (variantPrice.ValidFromUtc == null || variantPrice.ValidFromUtc <= utcNow) &&
                (variantPrice.ValidToUtc == null || variantPrice.ValidToUtc >= utcNow));
    }
}
