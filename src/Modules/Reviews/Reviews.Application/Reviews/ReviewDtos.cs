using Reviews.Domain.Questions;

namespace Reviews.Application.Reviews;

public sealed record ProductReviewSummaryDto(
    Guid ProductId,
    int ApprovedReviewCount,
    decimal AverageRating,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount,
    DateTime LastUpdatedAtUtc);

public sealed record ProductReviewDto(
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

public sealed record ReviewPageDto(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<ProductReviewDto> Items);

public sealed record ReviewVoteResultDto(
    Guid ReviewId,
    int HelpfulCount,
    int NotHelpfulCount,
    string VoteType);

public sealed record ProductAnswerDto(
    Guid Id,
    Guid QuestionId,
    Guid? CustomerId,
    string DisplayName,
    string AnswerText,
    string Status,
    bool IsOfficialAnswer,
    string AnsweredByType,
    DateTime CreatedAtUtc);

public sealed record ProductQuestionDto(
    Guid Id,
    Guid ProductId,
    Guid CustomerId,
    string DisplayName,
    string QuestionText,
    string Status,
    DateTime CreatedAtUtc,
    int AnswerCount,
    int ReportCount,
    IReadOnlyCollection<ProductAnswerDto> Answers);

public sealed record QuestionPageDto(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<ProductQuestionDto> Items);

public sealed record MyReviewDto(
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

public sealed record MyQuestionDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string QuestionText,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<ProductAnswerDto> Answers);

public sealed record ModerationReviewDto(
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

public sealed record ReviewModerationPageDto(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<ModerationReviewDto> Items);

public sealed record ModerationQuestionDto(
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

public sealed record QuestionModerationPageDto(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<ModerationQuestionDto> Items);

public sealed record ModerationAnswerDto(
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
    AnsweredByType AnsweredByType,
    DateTime CreatedAtUtc);

public sealed record AnswerModerationPageDto(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<ModerationAnswerDto> Items);

public sealed record ReviewReportDto(
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

public sealed record ReviewReportPageDto(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyCollection<ReviewReportDto> Items);
