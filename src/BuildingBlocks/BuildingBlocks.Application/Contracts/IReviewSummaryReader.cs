namespace BuildingBlocks.Application.Contracts;

public interface IReviewSummaryReader
{
    Task<ReviewSummarySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, ReviewSummarySnapshot>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
