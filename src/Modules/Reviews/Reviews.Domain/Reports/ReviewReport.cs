using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Reviews.Domain.Reports;

public sealed class ReviewReport : AggregateRoot<Guid>
{
    private ReviewReport()
    {
    }

    public Guid ReviewId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public ReviewReportReasonType ReasonType { get; private set; }

    public string? Message { get; private set; }

    public ReviewReportStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public string? ResolutionNotes { get; private set; }

    public static Result<ReviewReport> Create(
        Guid reviewId,
        Guid? customerId,
        ReviewReportReasonType reasonType,
        string? message,
        DateTime createdAtUtc)
    {
        if (reviewId == Guid.Empty)
        {
            return Result<ReviewReport>.Failure(new Error("reviews.review.not_found", "Review was not found."));
        }

        return Result<ReviewReport>.Success(new ReviewReport
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            CustomerId = customerId == Guid.Empty ? null : customerId,
            ReasonType = reasonType,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            Status = ReviewReportStatus.Open,
            CreatedAtUtc = createdAtUtc,
        });
    }

    public void Resolve(string? resolutionNotes, DateTime resolvedAtUtc)
    {
        Status = ReviewReportStatus.Resolved;
        ResolvedAtUtc = resolvedAtUtc;
        ResolutionNotes = string.IsNullOrWhiteSpace(resolutionNotes) ? null : resolutionNotes.Trim();
    }

    public void Dismiss(string? resolutionNotes, DateTime resolvedAtUtc)
    {
        Status = ReviewReportStatus.Dismissed;
        ResolvedAtUtc = resolvedAtUtc;
        ResolutionNotes = string.IsNullOrWhiteSpace(resolutionNotes) ? null : resolutionNotes.Trim();
    }
}
