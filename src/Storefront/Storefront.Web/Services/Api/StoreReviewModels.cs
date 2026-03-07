namespace Storefront.Web.Services.Api;

public sealed record StoreReviewSummary(
    Guid ProductId,
    int ApprovedReviewCount,
    decimal AverageRating,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount,
    DateTime LastUpdatedAtUtc);

public sealed record StoreProductReview(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string DisplayName,
    string? Title,
    string? Body,
    int Rating,
    string Status,
    bool IsVerifiedPurchase,
    Guid? VerifiedPurchaseOrderId,
    DateTime CreatedAtUtc,
    int HelpfulCount,
    int NotHelpfulCount,
    int ReportCount,
    string? VariantName);

public sealed record StoreReviewPage(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<StoreProductReview> Items);

public sealed record StoreReviewVoteResult(
    Guid ReviewId,
    int HelpfulCount,
    int NotHelpfulCount,
    string VoteType);

public sealed record StoreProductAnswer(
    Guid Id,
    Guid QuestionId,
    Guid? CustomerId,
    string DisplayName,
    string AnswerText,
    string Status,
    bool IsOfficialAnswer,
    string AnsweredByType,
    DateTime CreatedAtUtc);

public sealed record StoreProductQuestion(
    Guid Id,
    Guid ProductId,
    Guid CustomerId,
    string DisplayName,
    string QuestionText,
    string Status,
    DateTime CreatedAtUtc,
    int AnswerCount,
    int ReportCount,
    IReadOnlyCollection<StoreProductAnswer> Answers);

public sealed record StoreQuestionPage(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<StoreProductQuestion> Items);

public sealed record StoreMyReview(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string ProductSlug,
    string DisplayName,
    string? Title,
    string? Body,
    int Rating,
    string Status,
    bool IsVerifiedPurchase,
    DateTime CreatedAtUtc);

public sealed record StoreMyQuestion(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string QuestionText,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<StoreProductAnswer> Answers);

public sealed record StoreModerationReview(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    Guid CustomerId,
    string DisplayName,
    int Rating,
    string? Title,
    string? Body,
    string Status,
    bool IsVerifiedPurchase,
    int ReportCount,
    DateTime CreatedAtUtc);

public sealed record StoreReviewModerationPage(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<StoreModerationReview> Items);

public sealed record StoreModerationQuestion(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    Guid CustomerId,
    string DisplayName,
    string QuestionText,
    string Status,
    int AnswerCount,
    int ReportCount,
    DateTime CreatedAtUtc);

public sealed record StoreQuestionModerationPage(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<StoreModerationQuestion> Items);

public sealed record StoreModerationAnswer(
    Guid Id,
    Guid QuestionId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string QuestionText,
    Guid? CustomerId,
    string DisplayName,
    string AnswerText,
    string Status,
    bool IsOfficialAnswer,
    string AnsweredByType,
    DateTime CreatedAtUtc);

public sealed record StoreAnswerModerationPage(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<StoreModerationAnswer> Items);

public sealed record StoreReviewReport(
    Guid Id,
    Guid ReviewId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    Guid? CustomerId,
    string ReasonType,
    string? Message,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotes);

public sealed record StoreReviewReportPage(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<StoreReviewReport> Items);
