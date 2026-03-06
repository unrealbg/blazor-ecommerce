using System.Security.Claims;
using BuildingBlocks.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.DependencyInjection;
using Orders.Application.Orders.Checkout;
using Orders.Application.Orders.GetOrder;
using Orders.Application.Orders.GetMyOrders;

namespace Orders.Api;

public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddOrdersApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/orders").WithTags("Orders");
        group.AllowAnonymous();

        group.MapPost("/checkout/{customerId}", async (
            string customerId,
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
                        ["code"] = "orders.checkout.idempotency_key.required",
                    });
            }

            var result = await sender.Send(new CheckoutCommand(customerId, idempotencyKey), cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.Value}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        group.MapPost("/checkout", async (
            CheckoutRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            HttpContext context,
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
                        ["code"] = "orders.checkout.idempotency_key.required",
                    });
            }

            var userId = GetUserId(context.User);
            var result = await sender.Send(
                new CheckoutWithProfileCommand(
                    request.CartSessionId,
                    request.Email,
                    new CheckoutAddressInput(
                        request.ShippingAddress.FirstName,
                        request.ShippingAddress.LastName,
                        request.ShippingAddress.Street,
                        request.ShippingAddress.City,
                        request.ShippingAddress.PostalCode,
                        request.ShippingAddress.Country,
                        request.ShippingAddress.Phone),
                    new CheckoutAddressInput(
                        request.BillingAddress.FirstName,
                        request.BillingAddress.LastName,
                        request.BillingAddress.Street,
                        request.BillingAddress.City,
                        request.BillingAddress.PostalCode,
                        request.BillingAddress.Country,
                        request.BillingAddress.Phone),
                    idempotencyKey.Trim(),
                    userId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.Value}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        group.MapGet("/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
        {
            var order = await sender.Send(new GetOrderQuery(orderId), cancellationToken);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        });

        group.MapGet("/my", async (HttpContext context, ISender sender, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var orders = await sender.Send(new GetMyOrdersQuery(userId.Value), cancellationToken);
            return Results.Ok(orders);
        }).RequireAuthorization();

        return endpoints;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            "inventory.stock.insufficient" => StatusCodes.Status409Conflict,
            "inventory.reservation.expired" => StatusCodes.Status409Conflict,
            "inventory.reservation.not_found" => StatusCodes.Status409Conflict,
            "inventory.stock.concurrency_conflict" => StatusCodes.Status409Conflict,
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

    public sealed record CheckoutRequest(
        string CartSessionId,
        string Email,
        CheckoutAddressRequest ShippingAddress,
        CheckoutAddressRequest BillingAddress);

    public sealed record CheckoutAddressRequest(
        string FirstName,
        string LastName,
        string Street,
        string City,
        string PostalCode,
        string Country,
        string? Phone);
}
