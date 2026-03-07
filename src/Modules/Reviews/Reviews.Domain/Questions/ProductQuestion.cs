using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Reviews.Domain.Events;
using Reviews.Domain.Reviews;

namespace Reviews.Domain.Questions;

public sealed class ProductQuestion : AggregateRoot<Guid>
{
    private readonly List<ProductAnswer> answers = [];

    private ProductQuestion()
    {
    }

    public Guid ProductId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string QuestionText { get; private set; } = string.Empty;

    public ModerationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? ModerationNotes { get; private set; }

    public int AnswerCount { get; private set; }

    public int ReportCount { get; private set; }

    public IReadOnlyCollection<ProductAnswer> Answers => answers.AsReadOnly();

    public static Result<ProductQuestion> Create(
        Guid productId,
        Guid customerId,
        string displayName,
        string questionText,
        bool autoApprove,
        DateTime createdAtUtc)
    {
        if (productId == Guid.Empty)
        {
            return Result<ProductQuestion>.Failure(new Error("reviews.question.product_required", "Product is required."));
        }

        if (customerId == Guid.Empty)
        {
            return Result<ProductQuestion>.Failure(new Error("reviews.question.customer_required", "Customer is required."));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result<ProductQuestion>.Failure(new Error("reviews.question.display_name_required", "Display name is required."));
        }

        var normalizedQuestion = questionText?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuestion) || normalizedQuestion.Length < 8)
        {
            return Result<ProductQuestion>.Failure(new Error("reviews.question.too_short", "Question is too short."));
        }

        var question = new ProductQuestion
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerId = customerId,
            DisplayName = displayName.Trim(),
            QuestionText = normalizedQuestion,
            Status = autoApprove ? ModerationStatus.Approved : ModerationStatus.Pending,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            ApprovedAtUtc = autoApprove ? createdAtUtc : null,
        };

        question.RaiseDomainEvent(new QuestionSubmitted(question.Id, question.ProductId, question.CustomerId));
        if (question.Status == ModerationStatus.Approved)
        {
            question.RaiseDomainEvent(new QuestionApproved(question.Id, question.ProductId));
        }

        return Result<ProductQuestion>.Success(question);
    }

    public Result<ProductAnswer> AddAnswer(
        Guid? customerId,
        AnsweredByType answeredByType,
        string displayName,
        string answerText,
        bool isOfficialAnswer,
        bool autoApprove,
        DateTime createdAtUtc)
    {
        var answerResult = ProductAnswer.Create(
            Id,
            customerId,
            answeredByType,
            displayName,
            answerText,
            isOfficialAnswer,
            autoApprove,
            createdAtUtc);
        if (answerResult.IsFailure)
        {
            return Result<ProductAnswer>.Failure(answerResult.Error);
        }

        answers.Add(answerResult.Value);
        UpdatedAtUtc = createdAtUtc;
        if (answerResult.Value.Status == ModerationStatus.Approved)
        {
            AnswerCount++;
            RaiseDomainEvent(new AnswerApproved(Id, answerResult.Value.Id, ProductId));
        }
        else
        {
            RaiseDomainEvent(new AnswerSubmitted(Id, answerResult.Value.Id, ProductId));
        }

        return Result<ProductAnswer>.Success(answerResult.Value);
    }

    public Result Approve(string? moderationNotes, DateTime approvedAtUtc)
    {
        Status = ModerationStatus.Approved;
        ApprovedAtUtc = approvedAtUtc;
        ModerationNotes = string.IsNullOrWhiteSpace(moderationNotes) ? null : moderationNotes.Trim();
        UpdatedAtUtc = approvedAtUtc;
        RaiseDomainEvent(new QuestionApproved(Id, ProductId));
        return Result.Success();
    }

    public Result Reject(string? moderationNotes, DateTime rejectedAtUtc)
    {
        Status = ModerationStatus.Rejected;
        ApprovedAtUtc = null;
        ModerationNotes = string.IsNullOrWhiteSpace(moderationNotes) ? null : moderationNotes.Trim();
        UpdatedAtUtc = rejectedAtUtc;
        RaiseDomainEvent(new QuestionRejected(Id, ProductId));
        return Result.Success();
    }

    public Result Hide(string? moderationNotes, DateTime hiddenAtUtc)
    {
        Status = ModerationStatus.Hidden;
        ApprovedAtUtc = null;
        ModerationNotes = string.IsNullOrWhiteSpace(moderationNotes) ? null : moderationNotes.Trim();
        UpdatedAtUtc = hiddenAtUtc;
        return Result.Success();
    }

    public Result ApproveAnswer(Guid answerId, string? moderationNotes, DateTime approvedAtUtc)
    {
        var answer = answers.FirstOrDefault(item => item.Id == answerId);
        if (answer is null)
        {
            return Result.Failure(new Error("reviews.answer.not_found", "Answer was not found."));
        }

        var wasApproved = answer.Status == ModerationStatus.Approved;
        var result = answer.Approve(moderationNotes, approvedAtUtc);
        if (result.IsFailure)
        {
            return result;
        }

        if (!wasApproved)
        {
            AnswerCount++;
        }

        UpdatedAtUtc = approvedAtUtc;
        RaiseDomainEvent(new AnswerApproved(Id, answer.Id, ProductId));
        return Result.Success();
    }

    public Result RejectAnswer(Guid answerId, string? moderationNotes, DateTime rejectedAtUtc)
    {
        var answer = answers.FirstOrDefault(item => item.Id == answerId);
        if (answer is null)
        {
            return Result.Failure(new Error("reviews.answer.not_found", "Answer was not found."));
        }

        var wasApproved = answer.Status == ModerationStatus.Approved;
        var result = answer.Reject(moderationNotes, rejectedAtUtc);
        if (result.IsFailure)
        {
            return result;
        }

        if (wasApproved && AnswerCount > 0)
        {
            AnswerCount--;
        }

        UpdatedAtUtc = rejectedAtUtc;
        RaiseDomainEvent(new AnswerRejected(Id, answer.Id, ProductId));
        return Result.Success();
    }

    public Result HideAnswer(Guid answerId, string? moderationNotes, DateTime hiddenAtUtc)
    {
        var answer = answers.FirstOrDefault(item => item.Id == answerId);
        if (answer is null)
        {
            return Result.Failure(new Error("reviews.answer.not_found", "Answer was not found."));
        }

        var wasApproved = answer.Status == ModerationStatus.Approved;
        var result = answer.Hide(moderationNotes, hiddenAtUtc);
        if (result.IsFailure)
        {
            return result;
        }

        if (wasApproved && AnswerCount > 0)
        {
            AnswerCount--;
        }

        UpdatedAtUtc = hiddenAtUtc;
        return Result.Success();
    }

    public void IncrementReportCount(DateTime updatedAtUtc)
    {
        ReportCount++;
        UpdatedAtUtc = updatedAtUtc;
    }
}
