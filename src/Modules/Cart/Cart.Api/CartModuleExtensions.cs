using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using Cart.Application.Carts.AddItem;
using Cart.Application.Carts.GetCart;
using Cart.Application.Carts.RemoveItem;
using Cart.Application.Carts.UpdateItemQuantity;
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
            IProductCatalogReader productCatalogReader,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var variantId = request.VariantId;
            if (variantId == Guid.Empty)
            {
                var product = await productCatalogReader.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null)
                {
                    return Results.NotFound();
                }

                variantId = product.DefaultVariantId;
            }

            var result = await sender.Send(
                new AddItemToCartCommand(customerId, request.ProductId, variantId, request.Quantity),
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

        group.MapPatch("/{customerId}/items/{variantId:guid}", async (
            string customerId,
            Guid variantId,
            UpdateItemQuantityRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateCartItemQuantityCommand(customerId, variantId, request.Quantity),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { id = result.Value })
                : BusinessError(result.Error);
        });

        group.MapDelete("/{customerId}/items/{variantId:guid}", async (
            string customerId,
            Guid variantId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RemoveCartItemCommand(customerId, variantId), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : BusinessError(result.Error);
        });

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            "cart.not_found" => StatusCodes.Status404NotFound,
            "cart.item.not_found" => StatusCodes.Status404NotFound,
            "inventory.stock.insufficient" => StatusCodes.Status409Conflict,
            "inventory.stock.concurrency_conflict" => StatusCodes.Status409Conflict,
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

    public sealed class AddItemRequest
    {
        public AddItemRequest()
        {
        }

        public AddItemRequest(Guid productId, int quantity)
        {
            ProductId = productId;
            VariantId = Guid.Empty;
            Quantity = quantity;
        }

        public AddItemRequest(Guid productId, Guid variantId, int quantity)
        {
            ProductId = productId;
            VariantId = variantId;
            Quantity = quantity;
        }

        public Guid ProductId { get; init; }

        public Guid VariantId { get; init; }

        public int Quantity { get; init; }
    }

    public sealed record UpdateItemQuantityRequest(int Quantity);
}
