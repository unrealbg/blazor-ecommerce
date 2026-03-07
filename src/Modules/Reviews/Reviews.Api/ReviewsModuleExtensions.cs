using System.Security.Claims;
using BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.DependencyInjection;
using Reviews.Application.Reviews;
using Reviews.Domain.Questions;
using Reviews.Domain.Reviews;
using Reviews.Domain.Reports;

namespace Reviews.Api;

public static class ReviewsModuleExtensions
{
    public static IServiceCollection AddReviewsModule(this IServiceCollection services)
    {
        services.AddReviewsApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapReviewsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/reviews").WithTags("Reviews");

        group.MapGet("/products/{productId:guid}/summary", async (
            Guid productId,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetProductSummaryAsync(productId, cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();

        group.MapGet("/products/{productId:guid}", async (
            Guid productId,
            int? page,
            int? pageSize,
            string? sort,
            int? rating,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetProductReviewsAsync(
                productId,
                page ?? 1,
                pageSize ?? 10,
                sort,
                rating,
                cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();

        group.MapGet("/products/{productId:guid}/questions", async (
            Guid productId,
            int? page,
            int? pageSize,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetProductQuestionsAsync(
                productId,
                page ?? 1,
                pageSize ?? 10,
                cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();

        var authenticatedGroup = group.MapGroup(string.Empty).RequireAuthorization();

        authenticatedGroup.MapPost("/products/{productId:guid}", async (
            HttpContext context,
            Guid productId,
            SubmitReviewRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await service.SubmitReviewAsync(userId.Value, productId, request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/reviews/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        authenticatedGroup.MapPut("/me/{reviewId:guid}", async (
            HttpContext context,
            Guid reviewId,
            SubmitReviewRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await service.UpdateMyReviewAsync(userId.Value, reviewId, request, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        });

        authenticatedGroup.MapPost("/products/{productId:guid}/questions", async (
            HttpContext context,
            Guid productId,
            SubmitQuestionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await service.SubmitQuestionAsync(userId.Value, productId, request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/reviews/questions/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        authenticatedGroup.MapPost("/questions/{questionId:guid}/answers", async (
            HttpContext context,
            Guid questionId,
            SubmitAnswerRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await service.SubmitAnswerAsync(userId.Value, questionId, request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/reviews/answers/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        authenticatedGroup.MapPost("/{reviewId:guid}/vote", async (
            HttpContext context,
            Guid reviewId,
            VoteReviewRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (!TryParseVoteType(request.VoteType, out var voteType))
            {
                return BusinessError(new Error("reviews.vote.invalid_type", "Vote type is invalid."));
            }

            var result = await service.VoteReviewAsync(userId.Value, reviewId, voteType, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : BusinessError(result.Error);
        });

        authenticatedGroup.MapPost("/{reviewId:guid}/report", async (
            HttpContext context,
            Guid reviewId,
            ReportReviewRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (!TryParseReasonType(request.ReasonType, out var reasonType))
            {
                return BusinessError(new Error("reviews.report.invalid_reason", "Report reason is invalid."));
            }

            var result = await service.ReportReviewAsync(
                userId.Value,
                reviewId,
                reasonType,
                request.Message,
                cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/reviews/admin/reports/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        authenticatedGroup.MapGet("/me", async (
            HttpContext context,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await service.GetMyReviewsAsync(userId.Value, cancellationToken);
            return Results.Ok(result);
        });

        authenticatedGroup.MapGet("/me/questions", async (
            HttpContext context,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await service.GetMyQuestionsAsync(userId.Value, cancellationToken);
            return Results.Ok(result);
        });

        var adminGroup = group.MapGroup("/admin").RequireAuthorization();

        adminGroup.MapGet("/reviews", async (
            string? status,
            int? page,
            int? pageSize,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAdminReviewsAsync(
                ParseModerationStatus(status),
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return Results.Ok(result);
        });

        adminGroup.MapPost("/reviews/{reviewId:guid}/approve", async (
            Guid reviewId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ApproveReviewAsync(reviewId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapPost("/reviews/{reviewId:guid}/reject", async (
            Guid reviewId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RejectReviewAsync(reviewId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapPost("/reviews/{reviewId:guid}/hide", async (
            Guid reviewId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.HideReviewAsync(reviewId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapGet("/questions", async (
            string? status,
            int? page,
            int? pageSize,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAdminQuestionsAsync(
                ParseModerationStatus(status),
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return Results.Ok(result);
        });

        adminGroup.MapPost("/questions/{questionId:guid}/approve", async (
            Guid questionId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ApproveQuestionAsync(questionId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapPost("/questions/{questionId:guid}/reject", async (
            Guid questionId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RejectQuestionAsync(questionId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapPost("/questions/{questionId:guid}/hide", async (
            Guid questionId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.HideQuestionAsync(questionId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapPost("/questions/{questionId:guid}/official-answer", async (
            Guid questionId,
            OfficialAnswerRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AddOfficialAnswerAsync(questionId, request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/reviews/answers/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        adminGroup.MapGet("/answers", async (
            string? status,
            int? page,
            int? pageSize,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAdminAnswersAsync(
                ParseModerationStatus(status),
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return Results.Ok(result);
        });

        adminGroup.MapPost("/answers/{answerId:guid}/approve", async (
            Guid answerId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ApproveAnswerAsync(answerId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapPost("/answers/{answerId:guid}/reject", async (
            Guid answerId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RejectAnswerAsync(answerId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapPost("/answers/{answerId:guid}/hide", async (
            Guid answerId,
            ModerationActionRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.HideAnswerAsync(answerId, request.Notes, cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        adminGroup.MapGet("/reports", async (
            string? status,
            int? page,
            int? pageSize,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetReviewReportsAsync(
                ParseReportStatus(status),
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return Results.Ok(result);
        });

        adminGroup.MapPost("/reports/{reportId:guid}/resolve", async (
            Guid reportId,
            ResolveReportRequest request,
            IReviewsService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ResolveReportAsync(
                reportId,
                request.Dismiss,
                request.Notes,
                cancellationToken);
            return result.IsSuccess ? Results.Ok() : BusinessError(result.Error);
        });

        return endpoints;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static ModerationStatus? ParseModerationStatus(string? value)
    {
        return Enum.TryParse<ModerationStatus>(value, ignoreCase: true, out var status) ? status : null;
    }

    private static ReviewReportStatus? ParseReportStatus(string? value)
    {
        return Enum.TryParse<ReviewReportStatus>(value, ignoreCase: true, out var status) ? status : null;
    }

    private static bool TryParseVoteType(string? value, out ReviewVoteType voteType)
    {
        return Enum.TryParse(value, ignoreCase: true, out voteType);
    }

    private static bool TryParseReasonType(string? value, out ReviewReportReasonType reasonType)
    {
        return Enum.TryParse(value, ignoreCase: true, out reasonType);
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code.EndsWith(".not_found", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            _ when error.Code.EndsWith(".conflict", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ when error.Code.Contains("unauthorized", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            statusCode: statusCode,
            title: "Business rule violation",
            detail: error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
            });
    }

    public sealed record VoteReviewRequest(string VoteType);

    public sealed record ReportReviewRequest(string ReasonType, string? Message);

    public sealed record ModerationActionRequest(string? Notes);

    public sealed record ResolveReportRequest(bool Dismiss, string? Notes);
}
