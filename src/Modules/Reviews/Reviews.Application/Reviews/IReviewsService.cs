using BuildingBlocks.Domain.Results;
using Reviews.Domain.Questions;
using Reviews.Domain.Reviews;
using Reviews.Domain.Reports;

namespace Reviews.Application.Reviews;

public interface IReviewsService
{
    Task<ProductReviewSummaryDto> GetProductSummaryAsync(Guid productId, CancellationToken cancellationToken);

    Task<ReviewPageDto> GetProductReviewsAsync(
        Guid productId,
        int page,
        int pageSize,
        string? sort,
        int? rating,
        CancellationToken cancellationToken);

    Task<QuestionPageDto> GetProductQuestionsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Result<Guid>> SubmitReviewAsync(
        Guid userId,
        Guid productId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken);

    Task<Result> UpdateMyReviewAsync(
        Guid userId,
        Guid reviewId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken);

    Task<Result<ReviewVoteResultDto>> VoteReviewAsync(
        Guid userId,
        Guid reviewId,
        ReviewVoteType voteType,
        CancellationToken cancellationToken);

    Task<Result<Guid>> ReportReviewAsync(
        Guid userId,
        Guid reviewId,
        ReviewReportReasonType reasonType,
        string? message,
        CancellationToken cancellationToken);

    Task<Result<Guid>> SubmitQuestionAsync(
        Guid userId,
        Guid productId,
        SubmitQuestionRequest request,
        CancellationToken cancellationToken);

    Task<Result<Guid>> SubmitAnswerAsync(
        Guid userId,
        Guid questionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MyReviewDto>> GetMyReviewsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MyQuestionDto>> GetMyQuestionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<ReviewModerationPageDto> GetAdminReviewsAsync(
        ModerationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<QuestionModerationPageDto> GetAdminQuestionsAsync(
        ModerationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<AnswerModerationPageDto> GetAdminAnswersAsync(
        ModerationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ReviewReportPageDto> GetReviewReportsAsync(
        ReviewReportStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Result> ApproveReviewAsync(Guid reviewId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> RejectReviewAsync(Guid reviewId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> HideReviewAsync(Guid reviewId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> ApproveQuestionAsync(Guid questionId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> RejectQuestionAsync(Guid questionId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> HideQuestionAsync(Guid questionId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> ApproveAnswerAsync(Guid answerId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> RejectAnswerAsync(Guid answerId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result> HideAnswerAsync(Guid answerId, string? moderationNotes, CancellationToken cancellationToken);

    Task<Result<Guid>> AddOfficialAnswerAsync(
        Guid questionId,
        OfficialAnswerRequest request,
        CancellationToken cancellationToken);

    Task<Result> ResolveReportAsync(
        Guid reportId,
        bool dismiss,
        string? resolutionNotes,
        CancellationToken cancellationToken);
}
