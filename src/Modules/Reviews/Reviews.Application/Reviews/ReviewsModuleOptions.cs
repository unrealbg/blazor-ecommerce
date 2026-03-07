namespace Reviews.Application.Reviews;

public sealed class ReviewsModuleOptions
{
    public const string SectionName = "Reviews";

    public bool AutoApproveVerifiedPurchaseReviews { get; set; }

    public bool AutoApproveQuestions { get; set; }

    public bool AutoApproveOfficialAnswers { get; set; } = true;

    public bool RestrictReviewsToPurchasersOnly { get; set; }

    public int AutoHideReportThreshold { get; set; } = 999;

    public int PublicCacheSeconds { get; set; } = 60;
}
