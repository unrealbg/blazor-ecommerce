using System.Security.Claims;
using System.Text.Json;
using Backoffice.Application.Backoffice;
using Backoffice.Application.DependencyInjection;
using BuildingBlocks.Application.Auditing;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Backoffice.Api;

public static class BackofficeModuleExtensions
{
    public static IServiceCollection AddBackofficeModule(this IServiceCollection services)
    {
        services.AddBackofficeApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapBackofficeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/backoffice")
            .WithTags("Backoffice")
            .RequireAuthorization(BackofficePolicyNames.StaffAccess);

        group.MapGet("/session", async (
            HttpContext context,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var session = await queryService.GetSessionAsync(context.User, cancellationToken);
            return session is null ? Results.Forbid() : Results.Ok(session);
        });

        group.MapGet("/dashboard", async (
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var dashboard = await queryService.GetDashboardAsync(cancellationToken);
            return Results.Ok(dashboard);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.DashboardView));

        group.MapGet("/orders", async (
            string? orderId,
            DateTime? fromUtc,
            DateTime? toUtc,
            string? status,
            string? paymentStatus,
            string? fulfillmentStatus,
            string? customerEmail,
            int page,
            int pageSize,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var result = await queryService.GetOrdersAsync(
                orderId,
                fromUtc,
                toUtc,
                status,
                paymentStatus,
                fulfillmentStatus,
                customerEmail,
                page,
                pageSize,
                cancellationToken);

            return Results.Ok(result);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.OrdersView));

        group.MapGet("/orders/{orderId:guid}", async (
            Guid orderId,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var order = await queryService.GetOrderAsync(orderId, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.OrdersView));

        group.MapGet("/orders/{orderId:guid}/notes", async (
            Guid orderId,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var notes = await queryService.GetOrderNotesAsync(orderId, cancellationToken);
            return Results.Ok(notes);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.OrdersView));

        group.MapPost("/orders/{orderId:guid}/notes", async (
            Guid orderId,
            AddOrderInternalNoteRequest request,
            HttpContext context,
            [FromServices] IOrderInternalNoteService noteService,
            CancellationToken cancellationToken) =>
        {
            var result = await noteService.AddOrderNoteAsync(
                orderId,
                request.Note,
                GetActorUserId(context),
                GetActorEmail(context),
                GetActorDisplayName(context),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/backoffice/orders/{orderId:D}/notes/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.OrdersEdit));

        group.MapGet("/customers", async (
            string? query,
            int page,
            int pageSize,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var result = await queryService.GetCustomersAsync(query, page, pageSize, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.CustomersView));

        group.MapGet("/customers/{customerId:guid}", async (
            Guid customerId,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var customer = await queryService.GetCustomerAsync(customerId, cancellationToken);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.CustomersView));

        group.MapGet("/staff/roles", async (
            [FromServices] IStaffManagementService service,
            CancellationToken cancellationToken) =>
        {
            var roles = await service.GetRoleCatalogAsync(cancellationToken);
            return Results.Ok(roles);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.StaffView));

        group.MapGet("/staff", async (
            string? query,
            bool? isActive,
            int page,
            int pageSize,
            [FromServices] IStaffManagementService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetStaffAsync(query, isActive, page, pageSize, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.StaffView));

        group.MapGet("/staff/{userId:guid}", async (
            Guid userId,
            [FromServices] IStaffManagementService service,
            CancellationToken cancellationToken) =>
        {
            var user = await service.GetStaffUserAsync(userId, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.StaffView));

        group.MapPost("/staff", async (
            CreateStaffUserRequest request,
            HttpContext context,
            [FromServices] IStaffManagementService service,
            [FromServices] IAuditTrail auditTrail,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateStaffUserAsync(
                request.Email,
                request.Password,
                request.DisplayName,
                request.Department,
                request.Roles,
                cancellationToken);

            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            await auditTrail.WriteAsync(
                CreateAuditInput(
                    context,
                    "StaffUserCreated",
                    "StaffUser",
                    result.Value.ToString("D"),
                    $"Created staff user {request.Email.Trim().ToLowerInvariant()}.",
                    new
                    {
                        request.Email,
                        request.DisplayName,
                        request.Department,
                        request.Roles,
                    }),
                cancellationToken);

            return Results.Created($"/api/v1/backoffice/staff/{result.Value:D}", new { id = result.Value });
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.StaffEdit));

        group.MapPost("/staff/{userId:guid}/active", async (
            Guid userId,
            SetStaffActiveRequest request,
            HttpContext context,
            [FromServices] IStaffManagementService service,
            [FromServices] IAuditTrail auditTrail,
            CancellationToken cancellationToken) =>
        {
            var result = await service.SetStaffActiveAsync(userId, request.IsActive, cancellationToken);
            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            await auditTrail.WriteAsync(
                CreateAuditInput(
                    context,
                    request.IsActive ? "StaffAccessEnabled" : "StaffAccessDisabled",
                    "StaffUser",
                    userId.ToString("D"),
                    request.IsActive ? "Enabled staff access." : "Disabled staff access.",
                    new { request.IsActive }),
                cancellationToken);

            return Results.Ok(new { id = userId, request.IsActive });
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.StaffEdit));

        group.MapPost("/staff/{userId:guid}/roles/{roleName}", async (
            Guid userId,
            string roleName,
            HttpContext context,
            [FromServices] IStaffManagementService service,
            [FromServices] IAuditTrail auditTrail,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AssignRoleAsync(userId, roleName, cancellationToken);
            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            await auditTrail.WriteAsync(
                CreateAuditInput(
                    context,
                    "StaffRoleAssigned",
                    "StaffUser",
                    userId.ToString("D"),
                    $"Assigned role {roleName} to staff user.",
                    new { roleName }),
                cancellationToken);

            return Results.Ok(new { id = userId, roleName });
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.StaffEdit));

        group.MapDelete("/staff/{userId:guid}/roles/{roleName}", async (
            Guid userId,
            string roleName,
            HttpContext context,
            [FromServices] IStaffManagementService service,
            [FromServices] IAuditTrail auditTrail,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RemoveRoleAsync(userId, roleName, cancellationToken);
            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            await auditTrail.WriteAsync(
                CreateAuditInput(
                    context,
                    "StaffRoleRemoved",
                    "StaffUser",
                    userId.ToString("D"),
                    $"Removed role {roleName} from staff user.",
                    new { roleName }),
                cancellationToken);

            return Results.Ok(new { id = userId, roleName });
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.StaffEdit));

        group.MapGet("/audit", async (
            string? actor,
            string? actionType,
            string? targetType,
            string? targetId,
            DateTime? fromUtc,
            DateTime? toUtc,
            int page,
            int pageSize,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var result = await queryService.GetAuditEntriesAsync(
                actor,
                actionType,
                targetType,
                targetId,
                fromUtc,
                toUtc,
                page,
                pageSize,
                cancellationToken);

            return Results.Ok(result);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.AuditView));

        group.MapGet("/audit/{auditEntryId:guid}", async (
            Guid auditEntryId,
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var entry = await queryService.GetAuditEntryAsync(auditEntryId, cancellationToken);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.AuditView));

        group.MapGet("/system", async (
            [FromServices] IBackofficeQueryService queryService,
            CancellationToken cancellationToken) =>
        {
            var result = await queryService.GetSystemSummaryAsync(cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.SystemView));

        group.MapPost("/system/outbox/{outboxMessageId:guid}/retry", async (
            Guid outboxMessageId,
            ConfirmationRequest request,
            HttpContext context,
            [FromServices] ISystemOperationsService service,
            [FromServices] IAuditTrail auditTrail,
            CancellationToken cancellationToken) =>
        {
            if (!request.IsConfirmed)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Confirmation required",
                    detail: "This recovery action requires explicit confirmation.",
                    extensions: new Dictionary<string, object?> { ["code"] = "backoffice.confirmation.required" });
            }

            var result = await service.RetryOutboxMessageAsync(outboxMessageId, cancellationToken);
            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            await auditTrail.WriteAsync(
                CreateAuditInput(context, "OutboxRetryRequested", "OutboxMessage", outboxMessageId.ToString("D"), "Requested outbox message retry.", null),
                cancellationToken);

            return Results.Ok(new { retried = true, id = outboxMessageId });
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.SystemView));

        group.MapPost("/system/payment-webhooks/{webhookMessageId:guid}/reprocess", async (
            Guid webhookMessageId,
            ConfirmationRequest request,
            HttpContext context,
            [FromServices] ISystemOperationsService service,
            [FromServices] IAuditTrail auditTrail,
            CancellationToken cancellationToken) =>
        {
            if (!request.IsConfirmed)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Confirmation required",
                    detail: "This recovery action requires explicit confirmation.",
                    extensions: new Dictionary<string, object?> { ["code"] = "backoffice.confirmation.required" });
            }

            var result = await service.ReprocessPaymentWebhookAsync(webhookMessageId, cancellationToken);
            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            await auditTrail.WriteAsync(
                CreateAuditInput(context, "PaymentWebhookReprocessRequested", "PaymentWebhookInboxMessage", webhookMessageId.ToString("D"), "Requested payment webhook reprocess.", null),
                cancellationToken);

            return Results.Ok(new { reprocessed = result.Value, id = webhookMessageId });
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.SystemView));

        group.MapPost("/system/shipping-webhooks/{webhookMessageId:guid}/reprocess", async (
            Guid webhookMessageId,
            ConfirmationRequest request,
            HttpContext context,
            [FromServices] ISystemOperationsService service,
            [FromServices] IAuditTrail auditTrail,
            CancellationToken cancellationToken) =>
        {
            if (!request.IsConfirmed)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Confirmation required",
                    detail: "This recovery action requires explicit confirmation.",
                    extensions: new Dictionary<string, object?> { ["code"] = "backoffice.confirmation.required" });
            }

            var result = await service.ReprocessShippingWebhookAsync(webhookMessageId, cancellationToken);
            if (result.IsFailure)
            {
                return BusinessError(result.Error);
            }

            await auditTrail.WriteAsync(
                CreateAuditInput(context, "ShippingWebhookReprocessRequested", "ShippingWebhookInboxMessage", webhookMessageId.ToString("D"), "Requested shipping webhook reprocess.", null),
                cancellationToken);

            return Results.Ok(new { reprocessed = result.Value, id = webhookMessageId });
        }).RequireAuthorization(BackofficePolicyNames.Permission(BackofficePermissions.SystemView));

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code.EndsWith(".not_found", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            "backoffice.protected_role_modification.denied" => StatusCodes.Status409Conflict,
            "backoffice.role_assignment.not_allowed" => StatusCodes.Status400BadRequest,
            _ when error.Code.EndsWith(".conflict", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
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

    private static AuditEntryInput CreateAuditInput(
        HttpContext context,
        string actionType,
        string targetType,
        string targetId,
        string summary,
        object? metadata)
    {
        return new AuditEntryInput(
            actionType,
            targetType,
            targetId,
            summary,
            metadata is null ? null : JsonSerializer.Serialize(metadata),
            GetActorUserId(context),
            GetActorEmail(context),
            GetActorDisplayName(context),
            context.Connection.RemoteIpAddress?.ToString(),
            context.TraceIdentifier);
    }

    private static string? GetActorUserId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static string? GetActorEmail(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.Identity?.Name;
    }

    private static string? GetActorDisplayName(HttpContext context)
    {
        return context.User.FindFirstValue("name")
               ?? context.User.FindFirstValue(ClaimTypes.GivenName)
               ?? context.User.Identity?.Name;
    }

    public sealed record CreateStaffUserRequest(
        string Email,
        string Password,
        string? DisplayName,
        string? Department,
        IReadOnlyCollection<string> Roles);

    public sealed record SetStaffActiveRequest(bool IsActive);

    public sealed record AddOrderInternalNoteRequest(string Note);

    public sealed record ConfirmationRequest(bool IsConfirmed);
}
