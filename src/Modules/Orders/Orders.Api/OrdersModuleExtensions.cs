using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.DependencyInjection;
using Orders.Application.Orders.GetOrders;

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

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var orders = await sender.Send(new GetOrdersQuery(), cancellationToken);
            return Results.Ok(orders);
        });

        return endpoints;
    }
}
