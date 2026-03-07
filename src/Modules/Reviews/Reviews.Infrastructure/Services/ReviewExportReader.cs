using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Services;

internal sealed class ReviewExportReader(ReviewsDbContext dbContext) : ICustomerReviewExportReader
{
    public Task<IReadOnlyCollection<CustomerReviewExportRecord>> ListReviewsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return dbContext.ProductReviews
            .AsNoTracking()
            .Where(review => review.CustomerId == customerId)
            .OrderByDescending(review => review.CreatedAtUtc)
            .Select(review => new CustomerReviewExportRecord(
                review.Id,
                review.ProductId,
                review.Rating,
                review.Title,
                review.Body,
                review.Status.ToString(),
                review.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyCollection<CustomerReviewExportRecord>)task.Result, cancellationToken);
    }

    public Task<IReadOnlyCollection<CustomerQuestionExportRecord>> ListQuestionsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return dbContext.ProductQuestions
            .AsNoTracking()
            .Where(question => question.CustomerId == customerId)
            .OrderByDescending(question => question.CreatedAtUtc)
            .Select(question => new CustomerQuestionExportRecord(
                question.Id,
                question.ProductId,
                question.QuestionText,
                question.Status.ToString(),
                question.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyCollection<CustomerQuestionExportRecord>)task.Result, cancellationToken);
    }
}