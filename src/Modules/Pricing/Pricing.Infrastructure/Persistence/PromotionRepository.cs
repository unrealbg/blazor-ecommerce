using Microsoft.EntityFrameworkCore;
using Pricing.Application.Pricing;
using Pricing.Domain.Promotions;

namespace Pricing.Infrastructure.Persistence;

internal sealed class PromotionRepository(PricingDbContext dbContext) : IPromotionRepository
{
    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        return dbContext.Promotions.AddAsync(promotion, cancellationToken).AsTask();
    }

    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return QueryWithChildren().SingleOrDefaultAsync(promotion => promotion.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Promotion>> ListAsync(CancellationToken cancellationToken)
    {
        return await QueryWithChildren()
            .OrderByDescending(promotion => promotion.Priority)
            .ThenBy(promotion => promotion.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Promotion>> ListActiveAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return await QueryWithChildren()
            .Where(promotion =>
                promotion.Status == PromotionStatus.Active &&
                (promotion.StartAtUtc == null || promotion.StartAtUtc <= utcNow) &&
                (promotion.EndAtUtc == null || promotion.EndAtUtc >= utcNow))
            .OrderByDescending(promotion => promotion.Priority)
            .ThenBy(promotion => promotion.Name)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Promotion> QueryWithChildren()
    {
        return dbContext.Promotions
            .Include(promotion => promotion.Scopes)
            .Include(promotion => promotion.Conditions)
            .Include(promotion => promotion.Benefits)
            .AsSplitQuery();
    }
}
