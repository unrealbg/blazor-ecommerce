using BuildingBlocks.Domain.Results;
using BuildingBlocks.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.DependencyInjection;
using Payments.Application.Payments;
using Payments.Application.Payments.CancelPaymentIntent;
using Payments.Application.Payments.ConfirmPaymentIntent;
using Payments.Application.Payments.CreatePaymentIntent;
using Payments.Application.Payments.GetPaymentIntent;
using Payments.Application.Payments.GetPaymentIntentByOrder;
using Payments.Application.Payments.ListPaymentIntents;
using Payments.Application.Payments.RefundPaymentIntent;
using Payments.Application.Providers;
using Payments.Application.Webhooks.ProcessWebhook;
using Payments.Domain.Payments;

namespace Payments.Api;

public static class PaymentsModuleExtensions
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddPaymentsApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/payments").WithTags("Payments");

        group.MapPost("/intents", async (
            CreatePaymentIntentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid request",
                    detail: "Idempotency-Key header is required.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "payments.idempotency_key.required",
                    });
            }

            var result = await sender.Send(
                new CreatePaymentIntentCommand(
                    request.OrderId,
                    request.Provider,
                    idempotencyKey.Trim(),
                    request.CustomerEmail),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : BusinessError(result.Error);
        }).AllowAnonymous().RequireRateLimiting(RateLimitingPolicyNames.PaymentMutations);

        group.MapGet("/intents/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var paymentIntent = await sender.Send(new GetPaymentIntentQuery(id), cancellationToken);
            return paymentIntent is not null ? Results.Ok(paymentIntent) : Results.NotFound();
        }).AllowAnonymous().RequireRateLimiting(RateLimitingPolicyNames.PaymentMutations);

        group.MapGet("/intents/by-order/{orderId:guid}", async (
            Guid orderId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var paymentIntent = await sender.Send(new GetPaymentIntentByOrderQuery(orderId), cancellationToken);
            return paymentIntent is not null ? Results.Ok(paymentIntent) : Results.NotFound();
        }).AllowAnonymous().RequireRateLimiting(RateLimitingPolicyNames.PublicWebhook);

        group.MapPost("/intents/{id:guid}/confirm", async (
            Guid id,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid request",
                    detail: "Idempotency-Key header is required.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "payments.idempotency_key.required",
                    });
            }

            var result = await sender.Send(
                new ConfirmPaymentIntentCommand(id, idempotencyKey.Trim()),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : BusinessError(result.Error);
        }).AllowAnonymous();

        group.MapPost("/intents/{id:guid}/cancel", async (
            Guid id,
            CancelPaymentIntentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CancelPaymentIntentCommand(id, request.Reason), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : BusinessError(result.Error);
        }).AllowAnonymous();

        group.MapPost("/intents/{id:guid}/refund", async (
            Guid id,
            RefundPaymentIntentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RefundPaymentIntentCommand(id, request.Amount, request.Reason),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapGet("/intents", async (
            string? provider,
            string? status,
            DateTime? createdFromUtc,
            DateTime? createdToUtc,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            PaymentIntentStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<PaymentIntentStatus>(status, ignoreCase: true, out var resolvedStatus))
            {
                parsedStatus = resolvedStatus;
            }

            var response = await sender.Send(
                new ListPaymentIntentsQuery(provider, parsedStatus, createdFromUtc, createdToUtc, page, pageSize),
                cancellationToken);

            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapPost("/webhooks/{provider}", async (
            string provider,
            HttpContext context,
            IPaymentWebhookVerifier webhookVerifier,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var headers = context.Request.Headers
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);

            var isValidWebhook = await webhookVerifier.VerifyAsync(
                provider,
                headers,
                payload,
                cancellationToken);
            if (!isValidWebhook)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Webhook verification failed",
                    detail: "Webhook signature verification failed.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "payments.webhook.verification_failed",
                    });
            }

            var result = await sender.Send(new ProcessWebhookCommand(provider, payload), cancellationToken);
            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            return Results.Ok(new { received = true, processed = result.Value });
        }).AllowAnonymous();

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            "payments.intent.not_found" => StatusCodes.Status404NotFound,
            "payments.order.not_payable" => StatusCodes.Status409Conflict,
            "payments.amount.mismatch" => StatusCodes.Status409Conflict,
            "payments.currency.mismatch" => StatusCodes.Status409Conflict,
            "payments.intent.already_completed" => StatusCodes.Status409Conflict,
            "payments.refund.not_allowed" => StatusCodes.Status409Conflict,
            "payments.provider.unavailable" => StatusCodes.Status503ServiceUnavailable,
            "payments.webhook.verification_failed" => StatusCodes.Status401Unauthorized,
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

    public sealed record CreatePaymentIntentRequest(Guid OrderId, string? Provider, string? CustomerEmail);

    public sealed record CancelPaymentIntentRequest(string? Reason);

    public sealed record RefundPaymentIntentRequest(decimal? Amount, string? Reason);
}
