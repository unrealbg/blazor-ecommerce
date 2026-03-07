using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Services;

internal sealed class ReviewSummaryReader(ReviewsDbContext dbContext) : IReviewSummaryReader
{
    public async Task<ReviewSummarySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.ReviewAggregateSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        return snapshot is null ? null : Map(snapshot);
    }

    public async Task<IReadOnlyDictionary<Guid, ReviewSummarySnapshot>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, ReviewSummarySnapshot>();
        }

        var snapshots = await dbContext.ReviewAggregateSnapshots
            .AsNoTracking()
            .Where(item => productIds.Contains(item.ProductId))
            .ToListAsync(cancellationToken);

        return snapshots.ToDictionary(item => item.ProductId, Map);
    }

    private static ReviewSummarySnapshot Map(Reviews.Domain.Reviews.ReviewAggregateSnapshot snapshot)
    {
        return new ReviewSummarySnapshot(
            snapshot.ProductId,
            snapshot.ApprovedReviewCount,
            snapshot.AverageRating,
            snapshot.FiveStarCount,
            snapshot.FourStarCount,
            snapshot.ThreeStarCount,
            snapshot.TwoStarCount,
            snapshot.OneStarCount,
            snapshot.LastUpdatedAtUtc);
    }
}
