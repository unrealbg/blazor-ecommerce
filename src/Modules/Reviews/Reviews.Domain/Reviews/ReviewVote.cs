using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Reviews.Domain.Reviews;

public sealed class ReviewVote : Entity<Guid>
{
    private ReviewVote()
    {
    }

    public Guid ReviewId { get; private set; }

    public Guid CustomerId { get; private set; }

    public ReviewVoteType VoteType { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<ReviewVote> Create(
        Guid reviewId,
        Guid customerId,
        ReviewVoteType voteType,
        DateTime createdAtUtc)
    {
        if (reviewId == Guid.Empty)
        {
            return Result<ReviewVote>.Failure(new Error("reviews.review.not_found", "Review was not found."));
        }

        if (customerId == Guid.Empty)
        {
            return Result<ReviewVote>.Failure(new Error("reviews.vote.customer_required", "Customer is required."));
        }

        return Result<ReviewVote>.Success(new ReviewVote
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            CustomerId = customerId,
            VoteType = voteType,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        });
    }

    public Result ChangeVote(ReviewVoteType voteType, DateTime updatedAtUtc)
    {
        VoteType = voteType;
        UpdatedAtUtc = updatedAtUtc;
        return Result.Success();
    }
}
