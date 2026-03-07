using System.Text.Json;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reviews.Application.Reviews;
using Reviews.Domain.Questions;
using Reviews.Domain.Reports;
using Reviews.Domain.Reviews;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Services;

internal sealed class ReviewsService(
    ReviewsDbContext dbContext,
    IDistributedCache distributedCache,
    ICustomerCheckoutAccessor customerCheckoutAccessor,
    IProductCatalogReader productCatalogReader,
    IOrderReviewVerifier orderReviewVerifier,
    IClock clock,
    IOptions<ReviewsModuleOptions> options,
    ILogger<ReviewsService> logger)
    : IReviewsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ReviewsModuleOptions moduleOptions = options.Value;

    public async Task<ProductReviewSummaryDto> GetProductSummaryAsync(Guid productId, CancellationToken cancellationToken)
    {
        var cacheVersion = await GetCacheVersionAsync(productId, cancellationToken);
        var cacheKey = $"reviews:summary:{productId:N}:{cacheVersion}";
        var cached = await TryGetCachedAsync<ProductReviewSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var summary = await LoadOrRebuildSummaryAsync(productId, cancellationToken);
        await TrySetCachedAsync(cacheKey, summary, cancellationToken);
        return summary;
    }

    public async Task<ReviewPageDto> GetProductReviewsAsync(
        Guid productId,
        int page,
        int pageSize,
        string? sort,
        int? rating,
        CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var normalizedSort = NormalizeReviewSort(sort);
        var normalizedRating = rating is >= 1 and <= 5 ? rating : null;

        var cacheVersion = await GetCacheVersionAsync(productId, cancellationToken);
        var cacheKey = $"reviews:list:{productId:N}:{cacheVersion}:{normalizedPage}:{normalizedPageSize}:{normalizedSort}:{normalizedRating?.ToString() ?? "all"}";
        var cached = await TryGetCachedAsync<ReviewPageDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var product = await productCatalogReader.GetByIdAsync(productId, cancellationToken);
        var reviewsQuery = dbContext.ProductReviews
            .AsNoTracking()
            .Where(review => review.ProductId == productId && review.Status == ModerationStatus.Approved);

        if (normalizedRating is not null)
        {
            reviewsQuery = reviewsQuery.Where(review => review.Rating == normalizedRating.Value);
        }

        reviewsQuery = normalizedSort switch
        {
            "most_helpful" => reviewsQuery
                .OrderByDescending(review => review.HelpfulCount)
                .ThenByDescending(review => review.CreatedAtUtc),
            "highest" => reviewsQuery
                .OrderByDescending(review => review.Rating)
                .ThenByDescending(review => review.CreatedAtUtc),
            "lowest" => reviewsQuery
                .OrderBy(review => review.Rating)
                .ThenByDescending(review => review.CreatedAtUtc),
            _ => reviewsQuery.OrderByDescending(review => review.CreatedAtUtc),
        };

        var total = await reviewsQuery.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        var items = await reviewsQuery
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var result = new ReviewPageDto(
            normalizedPage,
            normalizedPageSize,
            total,
            totalPages,
            items.Select(review => MapReview(review, product)).ToArray());

        await TrySetCachedAsync(cacheKey, result, cancellationToken);
        return result;
    }

    public async Task<QuestionPageDto> GetProductQuestionsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var cacheVersion = await GetCacheVersionAsync(productId, cancellationToken);
        var cacheKey = $"reviews:questions:{productId:N}:{cacheVersion}:{normalizedPage}:{normalizedPageSize}";
        var cached = await TryGetCachedAsync<QuestionPageDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var questionsQuery = dbContext.ProductQuestions
            .AsNoTracking()
            .Include(question => question.Answers)
            .Where(question => question.ProductId == productId && question.Status == ModerationStatus.Approved)
            .OrderByDescending(question => question.CreatedAtUtc);

        var total = await questionsQuery.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        var items = await questionsQuery
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var result = new QuestionPageDto(
            normalizedPage,
            normalizedPageSize,
            total,
            totalPages,
            items.Select(MapApprovedQuestion).ToArray());

        await TrySetCachedAsync(cacheKey, result, cancellationToken);
        return result;
    }

    public async Task<Result<Guid>> SubmitReviewAsync(
        Guid userId,
        Guid productId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return Result<Guid>.Failure(customer.Error);
        }

        var product = await productCatalogReader.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result<Guid>.Failure(new Error("reviews.review.product_not_found", "Product was not found."));
        }

        var variantId = ResolveVariantId(product, request.VariantId);
        if (variantId.IsFailure)
        {
            return Result<Guid>.Failure(variantId.Error);
        }

        var existing = await dbContext.ProductReviews
            .SingleOrDefaultAsync(
                review => review.ProductId == productId && review.CustomerId == customer.Value.CustomerId,
                cancellationToken);
        if (existing is not null)
        {
            return Result<Guid>.Failure(new Error("reviews.review.conflict", "You have already submitted a review for this product."));
        }

        var verifiedPurchase = await orderReviewVerifier.VerifyPurchaseAsync(
            customer.Value.CustomerId,
            productId,
            variantId.Value,
            cancellationToken);

        if (moduleOptions.RestrictReviewsToPurchasersOnly && !verifiedPurchase.IsVerified)
        {
            return Result<Guid>.Failure(new Error(
                "reviews.purchaser_only_review_required",
                "Only customers who purchased this product can review it."));
        }

        var createResult = ProductReview.Create(
            productId,
            variantId.Value,
            customer.Value.CustomerId,
            BuildDisplayName(customer.Value),
            request.Title,
            request.Body,
            request.Rating,
            verifiedPurchase.IsVerified,
            verifiedPurchase.OrderId,
            moduleOptions.AutoApproveVerifiedPurchaseReviews && verifiedPurchase.IsVerified,
            "Storefront",
            clock.UtcNow);
        if (createResult.IsFailure)
        {
            return Result<Guid>.Failure(createResult.Error);
        }

        await dbContext.ProductReviews.AddAsync(createResult.Value, cancellationToken);
        if (createResult.Value.Status == ModerationStatus.Approved)
        {
            await UpdateAggregateSnapshotAsync(productId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(productId, cancellationToken);
        return Result<Guid>.Success(createResult.Value.Id);
    }

    public async Task<Result> UpdateMyReviewAsync(
        Guid userId,
        Guid reviewId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return customer;
        }

        var review = await dbContext.ProductReviews
            .SingleOrDefaultAsync(
                item => item.Id == reviewId && item.CustomerId == customer.Value.CustomerId,
                cancellationToken);
        if (review is null)
        {
            return Result.Failure(new Error("reviews.review.not_found", "Review was not found."));
        }

        var product = await productCatalogReader.GetByIdAsync(review.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new Error("reviews.review.product_not_found", "Product was not found."));
        }

        var variantId = ResolveVariantId(product, request.VariantId);
        if (variantId.IsFailure)
        {
            return variantId;
        }

        var wasApproved = review.Status == ModerationStatus.Approved;
        var editResult = review.Edit(
            variantId.Value,
            BuildDisplayName(customer.Value),
            request.Title,
            request.Body,
            request.Rating,
            moveBackToPending: wasApproved,
            clock.UtcNow);
        if (editResult.IsFailure)
        {
            return editResult;
        }

        if (wasApproved)
        {
            await UpdateAggregateSnapshotAsync(review.ProductId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(review.ProductId, cancellationToken);
        return Result.Success();
    }
    public async Task<Result<ReviewVoteResultDto>> VoteReviewAsync(
        Guid userId,
        Guid reviewId,
        ReviewVoteType voteType,
        CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return Result<ReviewVoteResultDto>.Failure(customer.Error);
        }

        var review = await dbContext.ProductReviews
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return Result<ReviewVoteResultDto>.Failure(new Error("reviews.review.not_found", "Review was not found."));
        }

        if (review.Status != ModerationStatus.Approved)
        {
            return Result<ReviewVoteResultDto>.Failure(new Error("reviews.review.vote_not_allowed", "Only approved reviews can be voted on."));
        }

        if (review.CustomerId == customer.Value.CustomerId)
        {
            return Result<ReviewVoteResultDto>.Failure(new Error("reviews.self_review_vote_not_allowed", "You cannot vote on your own review."));
        }

        var existingVote = await dbContext.ReviewVotes
            .SingleOrDefaultAsync(
                item => item.ReviewId == reviewId && item.CustomerId == customer.Value.CustomerId,
                cancellationToken);

        var helpfulCount = review.HelpfulCount;
        var notHelpfulCount = review.NotHelpfulCount;

        if (existingVote is null)
        {
            var createVoteResult = ReviewVote.Create(reviewId, customer.Value.CustomerId, voteType, clock.UtcNow);
            if (createVoteResult.IsFailure)
            {
                return Result<ReviewVoteResultDto>.Failure(createVoteResult.Error);
            }

            await dbContext.ReviewVotes.AddAsync(createVoteResult.Value, cancellationToken);
            if (voteType == ReviewVoteType.Helpful)
            {
                helpfulCount++;
            }
            else
            {
                notHelpfulCount++;
            }
        }
        else if (existingVote.VoteType != voteType)
        {
            var changeVoteResult = existingVote.ChangeVote(voteType, clock.UtcNow);
            if (changeVoteResult.IsFailure)
            {
                return Result<ReviewVoteResultDto>.Failure(changeVoteResult.Error);
            }

            if (voteType == ReviewVoteType.Helpful)
            {
                helpfulCount++;
                notHelpfulCount = Math.Max(0, notHelpfulCount - 1);
            }
            else
            {
                notHelpfulCount++;
                helpfulCount = Math.Max(0, helpfulCount - 1);
            }
        }

        var updateResult = review.ApplyVoteTotals(helpfulCount, notHelpfulCount, clock.UtcNow);
        if (updateResult.IsFailure)
        {
            return Result<ReviewVoteResultDto>.Failure(updateResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(review.ProductId, cancellationToken);

        return Result<ReviewVoteResultDto>.Success(new ReviewVoteResultDto(
            review.Id,
            review.HelpfulCount,
            review.NotHelpfulCount,
            voteType.ToString()));
    }

    public async Task<Result<Guid>> ReportReviewAsync(
        Guid userId,
        Guid reviewId,
        ReviewReportReasonType reasonType,
        string? message,
        CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return Result<Guid>.Failure(customer.Error);
        }

        var review = await dbContext.ProductReviews
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return Result<Guid>.Failure(new Error("reviews.review.not_found", "Review was not found."));
        }

        var reportResult = ReviewReport.Create(reviewId, customer.Value.CustomerId, reasonType, message, clock.UtcNow);
        if (reportResult.IsFailure)
        {
            return Result<Guid>.Failure(reportResult.Error);
        }

        await dbContext.ReviewReports.AddAsync(reportResult.Value, cancellationToken);
        review.IncrementReportCount(clock.UtcNow);

        if (moduleOptions.AutoHideReportThreshold > 0 &&
            review.ReportCount >= moduleOptions.AutoHideReportThreshold &&
            review.Status == ModerationStatus.Approved)
        {
            _ = review.Hide("Automatically hidden due to reports threshold.", clock.UtcNow);
            await UpdateAggregateSnapshotAsync(review.ProductId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(review.ProductId, cancellationToken);
        return Result<Guid>.Success(reportResult.Value.Id);
    }

    public async Task<Result<Guid>> SubmitQuestionAsync(
        Guid userId,
        Guid productId,
        SubmitQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return Result<Guid>.Failure(customer.Error);
        }

        var product = await productCatalogReader.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result<Guid>.Failure(new Error("reviews.question.product_not_found", "Product was not found."));
        }

        var questionResult = ProductQuestion.Create(
            productId,
            customer.Value.CustomerId,
            BuildDisplayName(customer.Value),
            request.QuestionText,
            moduleOptions.AutoApproveQuestions,
            clock.UtcNow);
        if (questionResult.IsFailure)
        {
            return Result<Guid>.Failure(questionResult.Error);
        }

        await dbContext.ProductQuestions.AddAsync(questionResult.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(productId, cancellationToken);
        return Result<Guid>.Success(questionResult.Value.Id);
    }

    public async Task<Result<Guid>> SubmitAnswerAsync(
        Guid userId,
        Guid questionId,
        SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return Result<Guid>.Failure(customer.Error);
        }

        var question = await dbContext.ProductQuestions
            .Include(item => item.Answers)
            .SingleOrDefaultAsync(item => item.Id == questionId, cancellationToken);
        if (question is null)
        {
            return Result<Guid>.Failure(new Error("reviews.question.not_found", "Question was not found."));
        }

        var answerResult = question.AddAnswer(
            customer.Value.CustomerId,
            AnsweredByType.Customer,
            BuildDisplayName(customer.Value),
            request.AnswerText,
            isOfficialAnswer: false,
            autoApprove: false,
            clock.UtcNow);
        if (answerResult.IsFailure)
        {
            return Result<Guid>.Failure(answerResult.Error);
        }

        await dbContext.ProductAnswers.AddAsync(answerResult.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(question.ProductId, cancellationToken);
        return Result<Guid>.Success(answerResult.Value.Id);
    }

    public async Task<IReadOnlyCollection<MyReviewDto>> GetMyReviewsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return [];
        }

        var reviews = await dbContext.ProductReviews
            .AsNoTracking()
            .Where(review => review.CustomerId == customer.Value.CustomerId)
            .OrderByDescending(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var products = await LoadProductsAsync(reviews.Select(review => review.ProductId), cancellationToken);
        return reviews.Select(review =>
        {
            products.TryGetValue(review.ProductId, out var product);
            return new MyReviewDto(
                review.Id,
                review.ProductId,
                review.VariantId,
                product?.Name ?? "Product",
                product?.Slug ?? string.Empty,
                review.DisplayName,
                review.Title,
                review.Body,
                review.Rating,
                review.Status.ToString(),
                review.IsVerifiedPurchase,
                review.CreatedAtUtc);
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<MyQuestionDto>> GetMyQuestionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(userId, cancellationToken);
        if (customer.IsFailure)
        {
            return [];
        }

        var questions = await dbContext.ProductQuestions
            .AsNoTracking()
            .Include(question => question.Answers)
            .Where(question => question.CustomerId == customer.Value.CustomerId)
            .OrderByDescending(question => question.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var products = await LoadProductsAsync(questions.Select(question => question.ProductId), cancellationToken);
        return questions.Select(question =>
        {
            products.TryGetValue(question.ProductId, out var product);
            return new MyQuestionDto(
                question.Id,
                question.ProductId,
                product?.Name ?? "Product",
                product?.Slug ?? string.Empty,
                question.QuestionText,
                question.Status.ToString(),
                question.CreatedAtUtc,
                question.Answers
                    .OrderBy(answer => answer.CreatedAtUtc)
                    .Select(MapAnswer)
                    .ToArray());
        }).ToArray();
    }
    public async Task<ReviewModerationPageDto> GetAdminReviewsAsync(
        ModerationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var query = dbContext.ProductReviews
            .AsNoTracking()
            .OrderByDescending(review => review.CreatedAtUtc)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(review => review.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        var reviews = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        var products = await LoadProductsAsync(reviews.Select(review => review.ProductId), cancellationToken);

        return new ReviewModerationPageDto(
            normalizedPage,
            normalizedPageSize,
            total,
            totalPages,
            reviews.Select(review =>
            {
                products.TryGetValue(review.ProductId, out var product);
                return new ModerationReviewDto(
                    review.Id,
                    review.ProductId,
                    product?.Name ?? "Product",
                    product?.Slug ?? string.Empty,
                    review.CustomerId,
                    review.DisplayName,
                    review.Rating,
                    review.Title,
                    review.Body,
                    review.Status.ToString(),
                    review.IsVerifiedPurchase,
                    review.ReportCount,
                    review.CreatedAtUtc);
            }).ToArray());
    }

    public async Task<QuestionModerationPageDto> GetAdminQuestionsAsync(
        ModerationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var query = dbContext.ProductQuestions
            .AsNoTracking()
            .OrderByDescending(question => question.CreatedAtUtc)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(question => question.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        var questions = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        var products = await LoadProductsAsync(questions.Select(question => question.ProductId), cancellationToken);

        return new QuestionModerationPageDto(
            normalizedPage,
            normalizedPageSize,
            total,
            totalPages,
            questions.Select(question =>
            {
                products.TryGetValue(question.ProductId, out var product);
                return new ModerationQuestionDto(
                    question.Id,
                    question.ProductId,
                    product?.Name ?? "Product",
                    product?.Slug ?? string.Empty,
                    question.CustomerId,
                    question.DisplayName,
                    question.QuestionText,
                    question.Status.ToString(),
                    question.AnswerCount,
                    question.ReportCount,
                    question.CreatedAtUtc);
            }).ToArray());
    }

    public async Task<AnswerModerationPageDto> GetAdminAnswersAsync(
        ModerationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var query = dbContext.ProductAnswers
            .AsNoTracking()
            .OrderByDescending(answer => answer.CreatedAtUtc)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(answer => answer.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        var answers = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var questionIds = answers.Select(answer => answer.QuestionId).Distinct().ToArray();
        var questions = await dbContext.ProductQuestions
            .AsNoTracking()
            .Where(question => questionIds.Contains(question.Id))
            .ToListAsync(cancellationToken);
        var products = await LoadProductsAsync(questions.Select(question => question.ProductId), cancellationToken);
        var questionLookup = questions.ToDictionary(question => question.Id);

        return new AnswerModerationPageDto(
            normalizedPage,
            normalizedPageSize,
            total,
            totalPages,
            answers.Select(answer =>
            {
                questionLookup.TryGetValue(answer.QuestionId, out var question);
                var productId = question?.ProductId ?? Guid.Empty;
                products.TryGetValue(productId, out var product);
                return new ModerationAnswerDto(
                    answer.Id,
                    answer.QuestionId,
                    productId,
                    product?.Name ?? "Product",
                    product?.Slug ?? string.Empty,
                    question?.QuestionText ?? string.Empty,
                    answer.CustomerId,
                    answer.DisplayName,
                    answer.AnswerText,
                    answer.Status.ToString(),
                    answer.IsOfficialAnswer,
                    answer.AnsweredByType,
                    answer.CreatedAtUtc);
            }).ToArray());
    }

    public async Task<ReviewReportPageDto> GetReviewReportsAsync(
        ReviewReportStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var query = dbContext.ReviewReports
            .AsNoTracking()
            .OrderByDescending(report => report.CreatedAtUtc)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(report => report.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        var reports = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var reviewIds = reports.Select(report => report.ReviewId).Distinct().ToArray();
        var reviews = await dbContext.ProductReviews
            .AsNoTracking()
            .Where(review => reviewIds.Contains(review.Id))
            .ToListAsync(cancellationToken);
        var reviewLookup = reviews.ToDictionary(review => review.Id);
        var products = await LoadProductsAsync(reviews.Select(review => review.ProductId), cancellationToken);

        return new ReviewReportPageDto(
            normalizedPage,
            normalizedPageSize,
            total,
            totalPages,
            reports.Select(report =>
            {
                reviewLookup.TryGetValue(report.ReviewId, out var review);
                var productId = review?.ProductId ?? Guid.Empty;
                products.TryGetValue(productId, out var product);
                return new ReviewReportDto(
                    report.Id,
                    report.ReviewId,
                    productId,
                    product?.Name ?? "Product",
                    product?.Slug ?? string.Empty,
                    report.CustomerId,
                    report.ReasonType.ToString(),
                    report.Message,
                    report.Status.ToString(),
                    report.CreatedAtUtc,
                    report.ResolvedAtUtc,
                    report.ResolutionNotes);
            }).ToArray());
    }

    public Task<Result> ApproveReviewAsync(Guid reviewId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateReviewAsync(
            reviewId,
            review => review.Approve(moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> RejectReviewAsync(Guid reviewId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateReviewAsync(
            reviewId,
            review => review.Reject(moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> HideReviewAsync(Guid reviewId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateReviewAsync(
            reviewId,
            review => review.Hide(moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> ApproveQuestionAsync(Guid questionId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateQuestionAsync(
            questionId,
            question => question.Approve(moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> RejectQuestionAsync(Guid questionId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateQuestionAsync(
            questionId,
            question => question.Reject(moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> HideQuestionAsync(Guid questionId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateQuestionAsync(
            questionId,
            question => question.Hide(moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> ApproveAnswerAsync(Guid answerId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateAnswerAsync(
            answerId,
            question => question.ApproveAnswer(answerId, moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> RejectAnswerAsync(Guid answerId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateAnswerAsync(
            answerId,
            question => question.RejectAnswer(answerId, moderationNotes, clock.UtcNow),
            cancellationToken);
    }

    public Task<Result> HideAnswerAsync(Guid answerId, string? moderationNotes, CancellationToken cancellationToken)
    {
        return ModerateAnswerAsync(
            answerId,
            question => question.HideAnswer(answerId, moderationNotes, clock.UtcNow),
            cancellationToken);
    }
    public async Task<Result<Guid>> AddOfficialAnswerAsync(
        Guid questionId,
        OfficialAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var question = await dbContext.ProductQuestions
            .Include(item => item.Answers)
            .SingleOrDefaultAsync(item => item.Id == questionId, cancellationToken);
        if (question is null)
        {
            return Result<Guid>.Failure(new Error("reviews.question.not_found", "Question was not found."));
        }

        var addResult = question.AddAnswer(
            customerId: null,
            AnsweredByType.Admin,
            request.DisplayName,
            request.AnswerText,
            isOfficialAnswer: true,
            autoApprove: moduleOptions.AutoApproveOfficialAnswers,
            clock.UtcNow);
        if (addResult.IsFailure)
        {
            return Result<Guid>.Failure(addResult.Error);
        }

        await dbContext.ProductAnswers.AddAsync(addResult.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(question.ProductId, cancellationToken);
        return Result<Guid>.Success(addResult.Value.Id);
    }

    public async Task<Result> ResolveReportAsync(
        Guid reportId,
        bool dismiss,
        string? resolutionNotes,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.ReviewReports
            .SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure(new Error("reviews.report.not_found", "Report was not found."));
        }

        if (dismiss)
        {
            report.Dismiss(resolutionNotes, clock.UtcNow);
        }
        else
        {
            report.Resolve(resolutionNotes, clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> ModerateReviewAsync(
        Guid reviewId,
        Func<ProductReview, Result> action,
        CancellationToken cancellationToken)
    {
        var review = await dbContext.ProductReviews
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return Result.Failure(new Error("reviews.review.not_found", "Review was not found."));
        }

        var result = action(review);
        if (result.IsFailure)
        {
            return result;
        }

        await UpdateAggregateSnapshotAsync(review.ProductId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(review.ProductId, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> ModerateQuestionAsync(
        Guid questionId,
        Func<ProductQuestion, Result> action,
        CancellationToken cancellationToken)
    {
        var question = await dbContext.ProductQuestions
            .SingleOrDefaultAsync(item => item.Id == questionId, cancellationToken);
        if (question is null)
        {
            return Result.Failure(new Error("reviews.question.not_found", "Question was not found."));
        }

        var result = action(question);
        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(question.ProductId, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> ModerateAnswerAsync(
        Guid answerId,
        Func<ProductQuestion, Result> action,
        CancellationToken cancellationToken)
    {
        var question = await dbContext.ProductQuestions
            .Include(item => item.Answers)
            .SingleOrDefaultAsync(item => item.Answers.Any(answer => answer.Id == answerId), cancellationToken);
        if (question is null)
        {
            return Result.Failure(new Error("reviews.answer.not_found", "Answer was not found."));
        }

        var result = action(question);
        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BumpCacheVersionAsync(question.ProductId, cancellationToken);
        return Result.Success();
    }

    private async Task<ProductReviewSummaryDto> LoadOrRebuildSummaryAsync(Guid productId, CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.ReviewAggregateSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        if (snapshot is not null)
        {
            return MapSummary(snapshot);
        }

        await UpdateAggregateSnapshotAsync(productId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await dbContext.ReviewAggregateSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        return reloaded is null
            ? new ProductReviewSummaryDto(productId, 0, 0m, 0, 0, 0, 0, 0, clock.UtcNow)
            : MapSummary(reloaded);
    }

    private async Task UpdateAggregateSnapshotAsync(Guid productId, CancellationToken cancellationToken)
    {
        var reviews = await dbContext.ProductReviews
            .Where(review => review.ProductId == productId)
            .ToListAsync(cancellationToken);

        var snapshot = await dbContext.ReviewAggregateSnapshots
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

        if (snapshot is null)
        {
            snapshot = ReviewAggregateSnapshot.Create(productId);
            await dbContext.ReviewAggregateSnapshots.AddAsync(snapshot, cancellationToken);
        }

        snapshot.Recalculate(reviews, clock.UtcNow);
    }

    private async Task<Result<CustomerCheckoutProfile>> ResolveCustomerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await customerCheckoutAccessor.GetByUserIdAsync(userId, cancellationToken);
        if (customer is null)
        {
            return Result<CustomerCheckoutProfile>.Failure(new Error(
                "reviews.review.submission_not_allowed",
                "Authenticated customer profile is required."));
        }

        return Result<CustomerCheckoutProfile>.Success(customer);
    }

    private static string BuildDisplayName(CustomerCheckoutProfile customer)
    {
        var fullName = $"{customer.FirstName} {customer.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        var email = customer.Email.Trim();
        var separatorIndex = email.IndexOf('@');
        return separatorIndex > 0 ? email[..separatorIndex] : email;
    }

    private static Result<Guid?> ResolveVariantId(ProductSnapshot product, Guid? requestedVariantId)
    {
        if (requestedVariantId is null)
        {
            return Result<Guid?>.Success(product.DefaultVariantId);
        }

        var variant = product.Variants.FirstOrDefault(item => item.Id == requestedVariantId.Value);
        if (variant is null)
        {
            return Result<Guid?>.Failure(new Error("reviews.review.variant_not_found", "Variant does not belong to product."));
        }

        return Result<Guid?>.Success(variant.Id);
    }

    private static ProductReviewSummaryDto MapSummary(ReviewAggregateSnapshot snapshot)
    {
        return new ProductReviewSummaryDto(
            snapshot.ProductId,
            snapshot.ApprovedReviewCount,
            snapshot.AverageRating,
            snapshot.FiveStarCount,
            snapshot.FourStarCount,
            snapshot.ThreeStarCount,
            snapshot.TwoStarCount,
            snapshot.OneStarCount,
            snapshot.LastUpdatedAtUtc);
    }

    private static ProductReviewDto MapReview(ProductReview review, ProductSnapshot? product)
    {
        var variantName = review.VariantId is null
            ? null
            : product?.Variants.FirstOrDefault(item => item.Id == review.VariantId)?.Name;

        return new ProductReviewDto(
            review.Id,
            review.ProductId,
            review.VariantId,
            review.DisplayName,
            review.Title,
            review.Body,
            review.Rating,
            review.Status.ToString(),
            review.IsVerifiedPurchase,
            review.VerifiedPurchaseOrderId,
            review.CreatedAtUtc,
            review.HelpfulCount,
            review.NotHelpfulCount,
            review.ReportCount,
            variantName);
    }

    private static ProductQuestionDto MapApprovedQuestion(ProductQuestion question)
    {
        return new ProductQuestionDto(
            question.Id,
            question.ProductId,
            question.CustomerId,
            question.DisplayName,
            question.QuestionText,
            question.Status.ToString(),
            question.CreatedAtUtc,
            question.AnswerCount,
            question.ReportCount,
            question.Answers
                .Where(answer => answer.Status == ModerationStatus.Approved)
                .OrderByDescending(answer => answer.IsOfficialAnswer)
                .ThenBy(answer => answer.CreatedAtUtc)
                .Select(MapAnswer)
                .ToArray());
    }

    private static ProductAnswerDto MapAnswer(ProductAnswer answer)
    {
        return new ProductAnswerDto(
            answer.Id,
            answer.QuestionId,
            answer.CustomerId,
            answer.DisplayName,
            answer.AnswerText,
            answer.Status.ToString(),
            answer.IsOfficialAnswer,
            answer.AnsweredByType.ToString(),
            answer.CreatedAtUtc);
    }

    private async Task<IReadOnlyDictionary<Guid, ProductSnapshot>> LoadProductsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, ProductSnapshot>();
        }

        var tasks = ids.Select(async productId => new
        {
            ProductId = productId,
            Snapshot = await productCatalogReader.GetByIdAsync(productId, cancellationToken),
        });

        var items = await Task.WhenAll(tasks);
        return items
            .Where(item => item.Snapshot is not null)
            .ToDictionary(item => item.ProductId, item => item.Snapshot!);
    }

    private async Task<string> GetCacheVersionAsync(Guid productId, CancellationToken cancellationToken)
    {
        var versionKey = $"reviews:version:{productId:N}";
        try
        {
            var version = await distributedCache.GetStringAsync(versionKey, cancellationToken);
            return string.IsNullOrWhiteSpace(version) ? "1" : version;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed reading reviews cache version for product {ProductId}", productId);
            return "1";
        }
    }

    private async Task BumpCacheVersionAsync(Guid productId, CancellationToken cancellationToken)
    {
        var versionKey = $"reviews:version:{productId:N}";
        try
        {
            await distributedCache.SetStringAsync(
                versionKey,
                clock.UtcNow.Ticks.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed updating reviews cache version for product {ProductId}", productId);
        }
    }

    private async Task<T?> TryGetCachedAsync<T>(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
            return string.IsNullOrWhiteSpace(payload)
                ? default
                : JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed reading reviews cache key {CacheKey}", cacheKey);
            return default;
        }
    }

    private async Task TrySetCachedAsync<T>(string cacheKey, T value, CancellationToken cancellationToken)
    {
        try
        {
            await distributedCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(value, SerializerOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(10, moduleOptions.PublicCacheSeconds)),
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed writing reviews cache key {CacheKey}", cacheKey);
        }
    }

    private static int NormalizePage(int page)
    {
        return page <= 0 ? 1 : page;
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0 ? 10 : Math.Min(pageSize, 50);
    }

    private static string NormalizeReviewSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return "newest";
        }

        var normalized = sort.Trim().ToLowerInvariant();
        return normalized is "newest" or "most_helpful" or "highest" or "lowest"
            ? normalized
            : "newest";
    }
}
