using Reviews.Domain.Reviews;

namespace Reviews.Tests;

public sealed class ReviewAggregateSnapshotTests
{
    [Fact]
    public void Recalculate_Should_IgnorePendingReviews()
    {
        var approved = CreateReview(5, autoApprove: true);
        var pending = CreateReview(1, autoApprove: false);
        var snapshot = ReviewAggregateSnapshot.Create(Guid.NewGuid());

        snapshot.Recalculate([approved, pending], DateTime.UtcNow);

        Assert.Equal(1, snapshot.ApprovedReviewCount);
        Assert.Equal(5m, snapshot.AverageRating);
        Assert.Equal(1, snapshot.FiveStarCount);
        Assert.Equal(0, snapshot.OneStarCount);
    }

    [Fact]
    public void Recalculate_Should_RemoveHiddenReview_FromAggregate()
    {
        var first = CreateReview(5, autoApprove: true);
        var second = CreateReview(3, autoApprove: true);
        var snapshot = ReviewAggregateSnapshot.Create(Guid.NewGuid());
        snapshot.Recalculate([first, second], DateTime.UtcNow);

        second.Hide("Hidden by moderator", DateTime.UtcNow);
        snapshot.Recalculate([first, second], DateTime.UtcNow);

        Assert.Equal(1, snapshot.ApprovedReviewCount);
        Assert.Equal(5m, snapshot.AverageRating);
        Assert.Equal(0, snapshot.ThreeStarCount);
    }

    [Fact]
    public void Recalculate_Should_CalculateAverage_WithTwoDecimals()
    {
        var first = CreateReview(4, autoApprove: true);
        var second = CreateReview(5, autoApprove: true);
        var third = CreateReview(4, autoApprove: true);
        var snapshot = ReviewAggregateSnapshot.Create(Guid.NewGuid());

        snapshot.Recalculate([first, second, third], DateTime.UtcNow);

        Assert.Equal(3, snapshot.ApprovedReviewCount);
        Assert.Equal(4.33m, snapshot.AverageRating);
    }

    private static ProductReview CreateReview(int rating, bool autoApprove)
    {
        return ProductReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            $"Review {rating}",
            "Helpful detailed body for aggregate calculation.",
            rating,
            false,
            null,
            autoApprove,
            "Storefront",
            DateTime.UtcNow).Value;
    }
}
