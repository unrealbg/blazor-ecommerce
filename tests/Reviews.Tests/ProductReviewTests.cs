using Reviews.Domain.Reviews;

namespace Reviews.Tests;

public sealed class ProductReviewTests
{
    [Fact]
    public void Create_Should_Fail_When_RatingOutsideRange()
    {
        var result = ProductReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            "Title",
            "Detailed review body.",
            6,
            false,
            null,
            false,
            "Storefront",
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("reviews.invalid_rating_value", result.Error.Code);
    }

    [Fact]
    public void Create_Should_Fail_When_ContentMissing()
    {
        var result = ProductReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            null,
            null,
            5,
            false,
            null,
            false,
            "Storefront",
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("reviews.review.too_short", result.Error.Code);
    }

    [Fact]
    public void Create_Should_SetVerifiedPurchase_WhenProvided()
    {
        var orderId = Guid.NewGuid();

        var result = ProductReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            "Solid product",
            "I am happy with the quality and delivery time.",
            5,
            true,
            orderId,
            false,
            "Storefront",
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsVerifiedPurchase);
        Assert.Equal(orderId, result.Value.VerifiedPurchaseOrderId);
        Assert.Equal(ModerationStatus.Pending, result.Value.Status);
    }

    [Fact]
    public void Edit_Should_MoveApprovedReview_BackToPending_WhenConfigured()
    {
        var review = ProductReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alex Mercer",
            "Great",
            "Very comfortable and stable after a week of use.",
            5,
            true,
            Guid.NewGuid(),
            true,
            "Storefront",
            DateTime.UtcNow).Value;

        var result = review.Edit(
            review.VariantId,
            "Alex Mercer",
            "Updated title",
            "Updated body with more detail.",
            4,
            moveBackToPending: true,
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ModerationStatus.Pending, review.Status);
        Assert.Null(review.ApprovedAtUtc);
    }
}
