using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullReviewSummaryReader : IReviewSummaryReader
{
    public Task<ReviewSummarySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Task.FromResult<ReviewSummarySnapshot?>(null);
    }

    public Task<IReadOnlyDictionary<Guid, ReviewSummarySnapshot>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, ReviewSummarySnapshot>>(
            new Dictionary<Guid, ReviewSummarySnapshot>());
    }
}
