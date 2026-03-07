using BuildingBlocks.Domain.Results;
using BuildingBlocks.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Search.Application.DependencyInjection;
using Search.Application.Search;

namespace Search.Api;

public static class SearchModuleExtensions
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services)
    {
        services.AddSearchApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/search").WithTags("Search");
        group.AllowAnonymous();

        group.MapGet("/products", async (
            [FromQuery(Name = "q")] string? query,
            [FromQuery(Name = "categorySlug")] string? categorySlug,
            [FromQuery(Name = "brand")] string[]? brands,
            [FromQuery(Name = "minPrice")] decimal? minPrice,
            [FromQuery(Name = "maxPrice")] decimal? maxPrice,
            [FromQuery(Name = "inStock")] bool? inStock,
            [FromQuery(Name = "sort")] string? sort,
            [FromQuery(Name = "page")] int? page,
            [FromQuery(Name = "pageSize")] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SearchProductsQuery(
                    query,
                    categorySlug,
                    brands,
                    minPrice,
                    maxPrice,
                    inStock,
                    sort,
                    page ?? 1,
                    pageSize ?? 24),
                cancellationToken);

            return Results.Ok(result);
        }).RequireRateLimiting(RateLimitingPolicyNames.SearchSuggest);

        group.MapGet("/suggest", async (
            [FromQuery(Name = "q")] string query,
            [FromQuery(Name = "limit")] int? limit,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SuggestProductsQuery(query, limit ?? 8),
                cancellationToken);

            return Results.Ok(result);
        });

        group.MapPost("/rebuild", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RebuildSearchIndexCommand(), cancellationToken);
            return result.IsSuccess
                ? Results.Ok(new { indexedDocuments = result.Value })
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
}
