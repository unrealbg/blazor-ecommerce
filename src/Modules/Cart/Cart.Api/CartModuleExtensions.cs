using Cart.Application.Carts.AddItem;
using Cart.Application.Carts.GetCart;
using Cart.Application.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Api;

public static class CartModuleExtensions
{
    public static IServiceCollection AddCartModule(this IServiceCollection services)
    {
        services.AddCartApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/cart").WithTags("Cart");

        group.MapPost("/{customerId}/items", async (
            string customerId,
            AddItemRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new AddItemToCartCommand(customerId, request.ProductId, request.Quantity),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/cart/{customerId}", new { id = result.Value })
                : Results.BadRequest(new { code = result.Error.Code, message = result.Error.Message });
        });

        group.MapGet("/{customerId}", async (string customerId, ISender sender, CancellationToken cancellationToken) =>
        {
            var cart = await sender.Send(new GetCartQuery(customerId), cancellationToken);
            return cart is not null ? Results.Ok(cart) : Results.NotFound();
        });

        return endpoints;
    }

    public sealed record AddItemRequest(Guid ProductId, int Quantity);
}
