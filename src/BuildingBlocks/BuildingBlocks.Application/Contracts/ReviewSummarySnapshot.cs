namespace BuildingBlocks.Application.Contracts;

public sealed record ReviewSummarySnapshot(
    Guid ProductId,
    int ApprovedReviewCount,
    decimal AverageRating,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount,
    DateTime LastUpdatedAtUtc);
