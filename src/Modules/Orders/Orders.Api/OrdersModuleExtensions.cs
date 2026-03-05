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

        group.MapGet("/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
        {
            var order = await sender.Send(new GetOrderQuery(orderId), cancellationToken);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        });

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code.EndsWith(".conflict", StringComparison.Ordinal)
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

        return Results.Problem(
            statusCode: statusCode,
            title: "Business rule violation",
            detail: error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
            });
    }
}
