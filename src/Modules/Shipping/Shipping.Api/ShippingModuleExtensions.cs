using System.Security.Claims;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shipping.Application.DependencyInjection;
using Shipping.Application.Providers;
using Shipping.Application.Shipping.CancelShipment;
using Shipping.Application.Shipping.CreateShipment;
using Shipping.Application.Shipping.CreateShipmentLabel;
using Shipping.Application.Shipping.CreateShippingMethod;
using Shipping.Application.Shipping.CreateShippingRateRule;
using Shipping.Application.Shipping.CreateShippingZone;
using Shipping.Application.Shipping.GetShipment;
using Shipping.Application.Shipping.GetShipmentByOrder;
using Shipping.Application.Shipping.GetShippingMethod;
using Shipping.Application.Shipping.ListShipments;
using Shipping.Application.Shipping.ListShippingMethods;
using Shipping.Application.Shipping.ListShippingRateRules;
using Shipping.Application.Shipping.ListShippingZones;
using Shipping.Application.Shipping.MarkShipmentShipped;
using Shipping.Application.Shipping.QuoteShipping;
using Shipping.Application.Shipping.UpdateShippingMethod;
using Shipping.Application.Shipping.UpdateShippingRateRule;
using Shipping.Application.Shipping.UpdateShippingZone;
using Shipping.Application.Webhooks.ProcessCarrierWebhook;

namespace Shipping.Api;

public static class ShippingModuleExtensions
{
    public static IServiceCollection AddShippingModule(this IServiceCollection services)
    {
        services.AddShippingApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/shipping").WithTags("Shipping");

        group.MapPost("/quotes", async (
            QuoteShippingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new QuoteShippingCommand(request.CountryCode, request.SubtotalAmount, request.Currency),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(result.Value) : BusinessError(result.Error);
        }).AllowAnonymous().RequireRateLimiting(RateLimitingPolicyNames.PublicWebhook);

        group.MapGet("/methods", async (
            bool activeOnly,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var methods = await sender.Send(new ListShippingMethodsQuery(activeOnly), cancellationToken);
            return Results.Ok(methods);
        }).AllowAnonymous();

        group.MapGet("/methods/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var method = await sender.Send(new GetShippingMethodQuery(id), cancellationToken);
            return method is not null ? Results.Ok(method) : Results.NotFound();
        }).AllowAnonymous();

        group.MapGet("/zones", async (
            bool activeOnly,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var zones = await sender.Send(new ListShippingZonesQuery(activeOnly), cancellationToken);
            return Results.Ok(zones);
        }).RequireAuthorization();

        group.MapGet("/rules", async (
            bool activeOnly,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var rules = await sender.Send(new ListShippingRateRulesQuery(activeOnly), cancellationToken);
            return Results.Ok(rules);
        }).RequireAuthorization();

        var adminGroup = group.MapGroup(string.Empty).RequireAuthorization();

        adminGroup.MapPost("/methods", async (
            CreateShippingMethodRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateShippingMethodCommand(
                    request.Code,
                    request.Name,
                    request.Description,
                    request.Provider,
                    request.Type,
                    request.BasePriceAmount,
                    request.Currency,
                    request.SupportsTracking,
                    request.SupportsPickupPoint,
                    request.EstimatedMinDays,
                    request.EstimatedMaxDays,
                    request.Priority),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/shipping/methods/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        adminGroup.MapPut("/methods/{id:guid}", async (
            Guid id,
            UpdateShippingMethodRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateShippingMethodCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.Provider,
                    request.Type,
                    request.BasePriceAmount,
                    request.Currency,
                    request.SupportsTracking,
                    request.SupportsPickupPoint,
                    request.EstimatedMinDays,
                    request.EstimatedMaxDays,
                    request.Priority,
                    request.IsActive),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(new { updated = true }) : BusinessError(result.Error);
        });

        adminGroup.MapPost("/zones", async (
            CreateShippingZoneRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateShippingZoneCommand(request.Code, request.Name, request.CountryCodes),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/shipping/zones/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        adminGroup.MapPut("/zones/{id:guid}", async (
            Guid id,
            UpdateShippingZoneRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateShippingZoneCommand(id, request.Name, request.CountryCodes, request.IsActive),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(new { updated = true }) : BusinessError(result.Error);
        });

        adminGroup.MapPost("/rules", async (
            CreateShippingRateRuleRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateShippingRateRuleCommand(
                    request.ShippingMethodId,
                    request.ShippingZoneId,
                    request.MinOrderAmount,
                    request.MaxOrderAmount,
                    request.MinWeightKg,
                    request.MaxWeightKg,
                    request.PriceAmount,
                    request.FreeShippingThresholdAmount,
                    request.Currency),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/shipping/rules/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        adminGroup.MapPut("/rules/{id:guid}", async (
            Guid id,
            UpdateShippingRateRuleRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateShippingRateRuleCommand(
                    id,
                    request.MinOrderAmount,
                    request.MaxOrderAmount,
                    request.MinWeightKg,
                    request.MaxWeightKg,
                    request.PriceAmount,
                    request.FreeShippingThresholdAmount,
                    request.Currency,
                    request.IsActive),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(new { updated = true }) : BusinessError(result.Error);
        });

        adminGroup.MapPost("/shipments", async (
            CreateShipmentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateShipmentCommand(request.OrderId, request.ShippingMethodCode),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/shipping/shipments/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        adminGroup.MapGet("/shipments", async (
            string? status,
            Guid? orderId,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var response = await sender.Send(
                new ListShipmentsQuery(status, orderId, page, pageSize),
                cancellationToken);
            return Results.Ok(response);
        });

        adminGroup.MapGet("/shipments/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var shipment = await sender.Send(new GetShipmentQuery(id), cancellationToken);
            return shipment is not null ? Results.Ok(shipment) : Results.NotFound();
        });

        adminGroup.MapPost("/shipments/{id:guid}/create-label", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateShipmentLabelCommand(id), cancellationToken);
            return result.IsSuccess ? Results.Ok(new { updated = true }) : BusinessError(result.Error);
        });

        adminGroup.MapPost("/shipments/{id:guid}/mark-shipped", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new MarkShipmentShippedCommand(id), cancellationToken);
            return result.IsSuccess ? Results.Ok(new { updated = true }) : BusinessError(result.Error);
        });

        adminGroup.MapPost("/shipments/{id:guid}/cancel", async (
            Guid id,
            CancelShipmentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CancelShipmentCommand(id, request.Reason), cancellationToken);
            return result.IsSuccess ? Results.Ok(new { updated = true }) : BusinessError(result.Error);
        });

        group.MapGet("/shipments/by-order/{orderId:guid}", async (
            Guid orderId,
            ClaimsPrincipal user,
            ICustomerCheckoutAccessor customerCheckoutAccessor,
            IOrderFulfillmentService orderFulfillmentService,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var customer = await customerCheckoutAccessor.GetByUserIdAsync(userId, cancellationToken);
            if (customer is null)
            {
                return Results.Forbid();
            }

            var order = await orderFulfillmentService.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                return Results.NotFound();
            }

            if (!string.Equals(order.CustomerId, customer.CustomerId.ToString("N"), StringComparison.Ordinal))
            {
                return Results.Forbid();
            }

            var shipment = await sender.Send(new GetShipmentByOrderQuery(orderId), cancellationToken);
            return shipment is not null ? Results.Ok(shipment) : Results.NotFound();
        }).RequireAuthorization();

        group.MapPost("/webhooks/{provider}", async (
            string provider,
            HttpContext context,
            IShippingWebhookVerifier webhookVerifier,
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

            var verified = await webhookVerifier.VerifyAsync(provider, headers, payload, cancellationToken);
            if (!verified)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Webhook verification failed",
                    detail: "Carrier webhook signature verification failed.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "shipping.webhook.verification_failed",
                    });
            }

            var result = await sender.Send(new ProcessCarrierWebhookCommand(provider, payload), cancellationToken);
            return result.IsSuccess
                ? Results.Ok(new { received = true, processed = result.Value })
                : BusinessError(result.Error);
        }).AllowAnonymous();

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            "shipping.method.not_found" => StatusCodes.Status404NotFound,
            "shipping.zone.not_found" => StatusCodes.Status404NotFound,
            "shipping.rule.not_found" => StatusCodes.Status404NotFound,
            "shipping.shipment.not_found" => StatusCodes.Status404NotFound,
            "shipping.no_methods_available" => StatusCodes.Status409Conflict,
            "shipping.method.not_applicable" => StatusCodes.Status409Conflict,
            "shipping.shipment.already_created" => StatusCodes.Status409Conflict,
            "shipping.order.not_fulfillable" => StatusCodes.Status409Conflict,
            "shipping.shipment.cancel.not_allowed" => StatusCodes.Status409Conflict,
            "shipping.carrier.unavailable" => StatusCodes.Status503ServiceUnavailable,
            "shipping.webhook.verification_failed" => StatusCodes.Status401Unauthorized,
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

    public sealed record QuoteShippingRequest(
        string CountryCode,
        decimal SubtotalAmount,
        string Currency);

    public sealed record CreateShippingMethodRequest(
        string Code,
        string Name,
        string? Description,
        string Provider,
        string Type,
        decimal BasePriceAmount,
        string Currency,
        bool SupportsTracking,
        bool SupportsPickupPoint,
        int? EstimatedMinDays,
        int? EstimatedMaxDays,
        int Priority);

    public sealed record UpdateShippingMethodRequest(
        string Name,
        string? Description,
        string Provider,
        string Type,
        decimal BasePriceAmount,
        string Currency,
        bool SupportsTracking,
        bool SupportsPickupPoint,
        int? EstimatedMinDays,
        int? EstimatedMaxDays,
        int Priority,
        bool IsActive);

    public sealed record CreateShippingZoneRequest(
        string Code,
        string Name,
        IReadOnlyCollection<string> CountryCodes);

    public sealed record UpdateShippingZoneRequest(
        string Name,
        IReadOnlyCollection<string> CountryCodes,
        bool IsActive);

    public sealed record CreateShippingRateRuleRequest(
        Guid ShippingMethodId,
        Guid ShippingZoneId,
        decimal? MinOrderAmount,
        decimal? MaxOrderAmount,
        decimal? MinWeightKg,
        decimal? MaxWeightKg,
        decimal PriceAmount,
        decimal? FreeShippingThresholdAmount,
        string Currency);

    public sealed record UpdateShippingRateRuleRequest(
        decimal? MinOrderAmount,
        decimal? MaxOrderAmount,
        decimal? MinWeightKg,
        decimal? MaxWeightKg,
        decimal PriceAmount,
        decimal? FreeShippingThresholdAmount,
        string Currency,
        bool IsActive);

    public sealed record CreateShipmentRequest(Guid OrderId, string? ShippingMethodCode);

    public sealed record CancelShipmentRequest(string? Reason);
}
