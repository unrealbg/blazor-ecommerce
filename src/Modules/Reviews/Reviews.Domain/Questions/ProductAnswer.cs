using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Reviews.Domain.Reviews;

namespace Reviews.Domain.Questions;

public sealed class ProductAnswer : Entity<Guid>
{
    private ProductAnswer()
    {
    }

    public Guid QuestionId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public AnsweredByType AnsweredByType { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string AnswerText { get; private set; } = string.Empty;

    public ModerationStatus Status { get; private set; }

    public bool IsOfficialAnswer { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? ModerationNotes { get; private set; }

    internal static Result<ProductAnswer> Create(
        Guid questionId,
        Guid? customerId,
        AnsweredByType answeredByType,
        string displayName,
        string answerText,
        bool isOfficialAnswer,
        bool autoApprove,
        DateTime createdAtUtc)
    {
        if (questionId == Guid.Empty)
        {
            return Result<ProductAnswer>.Failure(new Error("reviews.question.not_found", "Question was not found."));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result<ProductAnswer>.Failure(new Error("reviews.answer.display_name_required", "Display name is required."));
        }

        var normalizedAnswer = answerText?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAnswer) || normalizedAnswer.Length < 6)
        {
            return Result<ProductAnswer>.Failure(new Error("reviews.answer.too_short", "Answer is too short."));
        }

        if (isOfficialAnswer && answeredByType == AnsweredByType.Customer)
        {
            return Result<ProductAnswer>.Failure(new Error("reviews.answer.official_requires_staff", "Official answers require staff or admin."));
        }

        return Result<ProductAnswer>.Success(new ProductAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            CustomerId = customerId == Guid.Empty ? null : customerId,
            AnsweredByType = answeredByType,
            DisplayName = displayName.Trim(),
            AnswerText = normalizedAnswer,
            Status = autoApprove ? ModerationStatus.Approved : ModerationStatus.Pending,
            IsOfficialAnswer = isOfficialAnswer,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            ApprovedAtUtc = autoApprove ? createdAtUtc : null,
        });
    }

    internal Result Approve(string? moderationNotes, DateTime approvedAtUtc)
    {
        Status = ModerationStatus.Approved;
        ApprovedAtUtc = approvedAtUtc;
        ModerationNotes = string.IsNullOrWhiteSpace(moderationNotes) ? null : moderationNotes.Trim();
        UpdatedAtUtc = approvedAtUtc;
        return Result.Success();
    }

    internal Result Reject(string? moderationNotes, DateTime rejectedAtUtc)
    {
        Status = ModerationStatus.Rejected;
        ApprovedAtUtc = null;
        ModerationNotes = string.IsNullOrWhiteSpace(moderationNotes) ? null : moderationNotes.Trim();
        UpdatedAtUtc = rejectedAtUtc;
        return Result.Success();
    }

    internal Result Hide(string? moderationNotes, DateTime hiddenAtUtc)
    {
        Status = ModerationStatus.Hidden;
        ApprovedAtUtc = null;
        ModerationNotes = string.IsNullOrWhiteSpace(moderationNotes) ? null : moderationNotes.Trim();
        UpdatedAtUtc = hiddenAtUtc;
        return Result.Success();
    }
}
