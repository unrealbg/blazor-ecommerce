using BuildingBlocks.Domain.Primitives;
using Reviews.Domain.Events;

namespace Reviews.Domain.Reviews;

public sealed class ReviewAggregateSnapshot : Entity<Guid>
{
    private ReviewAggregateSnapshot()
    {
    }

    public Guid ProductId { get; private set; }

    public int ApprovedReviewCount { get; private set; }

    public decimal AverageRating { get; private set; }

    public int FiveStarCount { get; private set; }

    public int FourStarCount { get; private set; }

    public int ThreeStarCount { get; private set; }

    public int TwoStarCount { get; private set; }

    public int OneStarCount { get; private set; }

    public DateTime LastUpdatedAtUtc { get; private set; }

    public static ReviewAggregateSnapshot Create(Guid productId)
    {
        return new ReviewAggregateSnapshot
        {
            Id = productId,
            ProductId = productId,
            LastUpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Recalculate(IEnumerable<ProductReview> reviews, DateTime updatedAtUtc)
    {
        var approved = reviews
            .Where(review => review.Status == ModerationStatus.Approved)
            .ToArray();

        ApprovedReviewCount = approved.Length;
        AverageRating = approved.Length == 0
            ? 0m
            : decimal.Round(approved.Average(review => (decimal)review.Rating), 2, MidpointRounding.AwayFromZero);
        FiveStarCount = approved.Count(review => review.Rating == 5);
        FourStarCount = approved.Count(review => review.Rating == 4);
        ThreeStarCount = approved.Count(review => review.Rating == 3);
        TwoStarCount = approved.Count(review => review.Rating == 2);
        OneStarCount = approved.Count(review => review.Rating == 1);
        LastUpdatedAtUtc = updatedAtUtc;
        RaiseDomainEvent(new ReviewAggregateUpdated(ProductId, ApprovedReviewCount, AverageRating));
    }
}
