using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        group.MapPost("/checkout/{customerId}", async (
            string customerId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CheckoutCommand(customerId), cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { code = result.Error.Code, message = result.Error.Message });
        });

        group.MapGet("/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
        {
            var order = await sender.Send(new GetOrderQuery(orderId), cancellationToken);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        });

        return endpoints;
    }
}
