using Catalog.Application.DependencyInjection;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.GetProducts;
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

        group.MapPost("/products", async (CreateProductRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new CreateProductCommand(request.Name, request.Currency, request.Amount);
            var result = await sender.Send(command, cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/catalog/products/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { code = result.Error.Code, message = result.Error.Message });
        });

        group.MapGet("/products", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var products = await sender.Send(new GetProductsQuery(), cancellationToken);
            return Results.Ok(products);
        });

        return endpoints;
    }

    public sealed record CreateProductRequest(string Name, string Currency, decimal Amount);
}
