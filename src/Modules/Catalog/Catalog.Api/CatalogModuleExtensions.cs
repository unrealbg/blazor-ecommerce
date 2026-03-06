using BuildingBlocks.Domain.Results;
using Catalog.Application.DependencyInjection;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.GetProductBySlug;
using Catalog.Application.Products.GetProducts;
using Catalog.Application.Products.UpdateProductSlug;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Api;

public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddCatalogApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/catalog").WithTags("Catalog");
        group.AllowAnonymous();

        group.MapPost("/products", async (CreateProductRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new CreateProductCommand(
                request.Name,
                request.Description,
                request.Brand,
                request.Sku,
                request.ImageUrl,
                request.IsInStock,
                request.CategorySlug,
                request.CategoryName,
                request.Currency,
                request.Amount,
                request.IsActive);

            var result = await sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/catalog/products/{result.Value}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        group.MapGet("/products", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var products = await sender.Send(new GetProductsQuery(), cancellationToken);
            return Results.Ok(products);
        });

        group.MapGet("/products/by-slug/{slug}", async (string slug, ISender sender, CancellationToken cancellationToken) =>
        {
            var product = await sender.Send(new GetProductBySlugQuery(slug), cancellationToken);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });

        group.MapPatch("/products/{productId:guid}/slug", async (
            Guid productId,
            UpdateProductSlugRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateProductSlugCommand(productId, request.Slug),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { id = productId, slug = result.Value })
                : BusinessError(result.Error);
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

    public sealed record CreateProductRequest(
        string Name,
        string? Description,
        string Currency = "EUR",
        decimal Amount = 0m,
        bool IsActive = true,
        string? Brand = null,
        string? Sku = null,
        string? ImageUrl = null,
        bool IsInStock = true,
        string? CategorySlug = null,
        string? CategoryName = null);

    public sealed record UpdateProductSlugRequest(string Slug);
}
