using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Reviews.Domain.Events;

namespace Reviews.Domain.Reviews;

public sealed class ProductReview : AggregateRoot<Guid>
{
    private ProductReview()
    {
    }

    public Guid ProductId { get; private set; }

    public Guid? VariantId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? Title { get; private set; }

    public string? Body { get; private set; }

    public int Rating { get; private set; }

    public ModerationStatus Status { get; private set; }

    public bool IsVerifiedPurchase { get; private set; }

    public Guid? VerifiedPurchaseOrderId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public DateTime? RejectedAtUtc { get; private set; }

    public string? ModerationNotes { get; private set; }

    public int HelpfulCount { get; private set; }

    public int NotHelpfulCount { get; private set; }

    public int ReportCount { get; private set; }

    public string Source { get; private set; } = "Storefront";

    public static Result<ProductReview> Create(
        Guid productId,
        Guid? variantId,
        Guid customerId,
        string displayName,
        string? title,
        string? body,
        int rating,
        bool isVerifiedPurchase,
        Guid? verifiedPurchaseOrderId,
        bool autoApprove,
        string source,
        DateTime createdAtUtc)
    {
        if (productId == Guid.Empty)
        {
            return Result<ProductReview>.Failure(new Error("reviews.review.product_required", "Product is required."));
        }

        if (customerId == Guid.Empty)
        {
            return Result<ProductReview>.Failure(new Error("reviews.review.customer_required", "Customer is required."));
        }

        var displayNameResult = ValidateDisplayName(displayName);
        if (displayNameResult.IsFailure)
        {
            return Result<ProductReview>.Failure(displayNameResult.Error);
        }

        var contentResult = ValidateContent(title, body, "reviews.review.too_short");
        if (contentResult.IsFailure)
        {
            return Result<ProductReview>.Failure(contentResult.Error);
        }

        if (rating is < 1 or > 5)
        {
            return Result<ProductReview>.Failure(new Error("reviews.invalid_rating_value", "Rating must be between 1 and 5."));
        }

        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VariantId = variantId == Guid.Empty ? null : variantId,
            CustomerId = customerId,
            DisplayName = displayNameResult.Value,
            Title = NormalizeOptional(title),
            Body = NormalizeOptional(body),
            Rating = rating,
            Status = autoApprove ? ModerationStatus.Approved : ModerationStatus.Pending,
            IsVerifiedPurchase = isVerifiedPurchase,
            VerifiedPurchaseOrderId = isVerifiedPurchase ? verifiedPurchaseOrderId : null,
            CreatedAtUtc = AsUtc(createdAtUtc),
            UpdatedAtUtc = AsUtc(createdAtUtc),
            ApprovedAtUtc = autoApprove ? AsUtc(createdAtUtc) : null,
            Source = string.IsNullOrWhiteSpace(source) ? "Storefront" : source.Trim(),
        };

        review.RaiseDomainEvent(new ReviewSubmitted(review.Id, review.ProductId, review.CustomerId));
        if (review.Status == ModerationStatus.Approved)
        {
            review.RaiseDomainEvent(new ReviewApproved(review.Id, review.ProductId));
        }

        return Result<ProductReview>.Success(review);
    }

    public Result Edit(
        Guid? variantId,
        string displayName,
        string? title,
        string? body,
        int rating,
        bool moveBackToPending,
        DateTime updatedAtUtc)
    {
        var displayNameResult = ValidateDisplayName(displayName);
        if (displayNameResult.IsFailure)
        {
            return displayNameResult;
        }

        var contentResult = ValidateContent(title, body, "reviews.review.too_short");
        if (contentResult.IsFailure)
        {
            return contentResult;
        }

        if (rating is < 1 or > 5)
        {
            return Result.Failure(new Error("reviews.invalid_rating_value", "Rating must be between 1 and 5."));
        }

        VariantId = variantId == Guid.Empty ? null : variantId;
        DisplayName = displayNameResult.Value;
        Title = NormalizeOptional(title);
        Body = NormalizeOptional(body);
        Rating = rating;
        UpdatedAtUtc = AsUtc(updatedAtUtc);

        if (moveBackToPending && Status == ModerationStatus.Approved)
        {
            Status = ModerationStatus.Pending;
            ApprovedAtUtc = null;
            RejectedAtUtc = null;
            ModerationNotes = null;
        }

        return Result.Success();
    }

    public Result Approve(string? moderationNotes, DateTime approvedAtUtc)
    {
        if (Status == ModerationStatus.Approved)
        {
            return Result.Failure(new Error("reviews.review.already_moderated", "Review is already approved."));
        }

        Status = ModerationStatus.Approved;
        ApprovedAtUtc = AsUtc(approvedAtUtc);
        RejectedAtUtc = null;
        ModerationNotes = NormalizeOptional(moderationNotes);
        UpdatedAtUtc = AsUtc(approvedAtUtc);
        RaiseDomainEvent(new ReviewApproved(Id, ProductId));
        return Result.Success();
    }

    public Result Reject(string? moderationNotes, DateTime rejectedAtUtc)
    {
        if (Status == ModerationStatus.Rejected)
        {
            return Result.Failure(new Error("reviews.review.already_moderated", "Review is already rejected."));
        }

        Status = ModerationStatus.Rejected;
        RejectedAtUtc = AsUtc(rejectedAtUtc);
        ApprovedAtUtc = null;
        ModerationNotes = NormalizeOptional(moderationNotes);
        UpdatedAtUtc = AsUtc(rejectedAtUtc);
        RaiseDomainEvent(new ReviewRejected(Id, ProductId));
        return Result.Success();
    }

    public Result Hide(string? moderationNotes, DateTime hiddenAtUtc)
    {
        if (Status == ModerationStatus.Hidden)
        {
            return Result.Failure(new Error("reviews.review.already_moderated", "Review is already hidden."));
        }

        Status = ModerationStatus.Hidden;
        ApprovedAtUtc = null;
        ModerationNotes = NormalizeOptional(moderationNotes);
        UpdatedAtUtc = AsUtc(hiddenAtUtc);
        RaiseDomainEvent(new ReviewHidden(Id, ProductId));
        return Result.Success();
    }

    public Result ApplyVoteTotals(int helpfulCount, int notHelpfulCount, DateTime updatedAtUtc)
    {
        if (helpfulCount < 0 || notHelpfulCount < 0)
        {
            return Result.Failure(new Error("reviews.vote.invalid_totals", "Vote totals cannot be negative."));
        }

        HelpfulCount = helpfulCount;
        NotHelpfulCount = notHelpfulCount;
        UpdatedAtUtc = AsUtc(updatedAtUtc);
        RaiseDomainEvent(new ReviewVoteChanged(Id, ProductId));
        return Result.Success();
    }

    public void IncrementReportCount(DateTime updatedAtUtc)
    {
        ReportCount++;
        UpdatedAtUtc = AsUtc(updatedAtUtc);
        RaiseDomainEvent(new ReviewReported(Id, ProductId));
    }

    private static Result<string> ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result<string>.Failure(new Error("reviews.review.display_name_required", "Display name is required."));
        }

        var normalized = displayName.Trim();
        if (normalized.Length > 120)
        {
            return Result<string>.Failure(new Error("reviews.review.display_name_too_long", "Display name is too long."));
        }

        return Result<string>.Success(normalized);
    }

    private static Result ValidateContent(string? title, string? body, string errorCode)
    {
        var normalizedTitle = NormalizeOptional(title);
        var normalizedBody = NormalizeOptional(body);

        if (string.IsNullOrWhiteSpace(normalizedTitle) && string.IsNullOrWhiteSpace(normalizedBody))
        {
            return Result.Failure(new Error(errorCode, "Review content is too short."));
        }

        if (!string.IsNullOrWhiteSpace(normalizedTitle) && normalizedTitle.Length < 3)
        {
            return Result.Failure(new Error(errorCode, "Review title is too short."));
        }

        if (!string.IsNullOrWhiteSpace(normalizedBody) && normalizedBody.Length < 10)
        {
            return Result.Failure(new Error(errorCode, "Review body is too short."));
        }

        return Result.Success();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
