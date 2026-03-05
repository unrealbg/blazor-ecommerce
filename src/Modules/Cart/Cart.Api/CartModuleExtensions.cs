using Cart.Application.Carts.CheckoutCart;
using Cart.Application.Carts.CreateCart;
using Cart.Application.Carts.GetCartById;
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

        group.MapPost("/", async (CreateCartRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateCartCommand(request.CustomerId), cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/cart/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { code = result.Error.Code, message = result.Error.Message });
        });

        group.MapGet("/{cartId:guid}", async (Guid cartId, ISender sender, CancellationToken cancellationToken) =>
        {
            var cart = await sender.Send(new GetCartByIdQuery(cartId), cancellationToken);
            return cart is not null ? Results.Ok(cart) : Results.NotFound();
        });

        group.MapPost("/{cartId:guid}/checkout", async (
            Guid cartId,
            CheckoutCartRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CheckoutCartCommand(cartId, request.Currency, request.TotalAmount),
                cancellationToken);

            return result.IsSuccess
                ? Results.Accepted($"/api/v1/cart/{cartId}")
                : Results.BadRequest(new { code = result.Error.Code, message = result.Error.Message });
        });

        return endpoints;
    }

    public sealed record CreateCartRequest(Guid CustomerId);

    public sealed record CheckoutCartRequest(string Currency, decimal TotalAmount);
}
