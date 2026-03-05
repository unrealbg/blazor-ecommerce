using BuildingBlocks.Domain.Results;
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
        group.AllowAnonymous();

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
                : BusinessError(result.Error);
        });

        group.MapGet("/{customerId}", async (string customerId, ISender sender, CancellationToken cancellationToken) =>
        {
            var cart = await sender.Send(new GetCartQuery(customerId), cancellationToken);
            return cart is not null ? Results.Ok(cart) : Results.NotFound();
        });

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Business rule violation",
            detail: error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
            });
    }

    public sealed record AddItemRequest(Guid ProductId, int Quantity);
}
