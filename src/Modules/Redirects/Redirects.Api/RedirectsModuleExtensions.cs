using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Redirects.Application.DependencyInjection;
using Redirects.Application.RedirectRules.CreateRedirectRule;
using Redirects.Application.RedirectRules.DeactivateRedirectRule;
using Redirects.Application.RedirectRules.ListRedirectRules;
using Redirects.Application.RedirectRules.ResolveRedirect;
using Redirects.Domain.RedirectRules;

namespace Redirects.Api;

public static class RedirectsModuleExtensions
{
    public static IServiceCollection AddRedirectsModule(this IServiceCollection services)
    {
        services.AddRedirectsApplication();
        return services;
    }

    public static IApplicationBuilder UseRedirectRules(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RedirectRequestMiddleware>();
    }

    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/redirects").WithTags("Redirects");
        group.AllowAnonymous();

        group.MapPost("/", async (
            CreateRedirectRuleRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateRedirectRuleCommand(
                    request.FromPath,
                    request.ToPath,
                    request.StatusCode ?? RedirectStatusCodes.PermanentRedirect),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/redirects/{result.Value}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        group.MapPut("/{redirectRuleId:guid}/deactivate", async (
            Guid redirectRuleId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeactivateRedirectRuleCommand(redirectRuleId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        });

        group.MapGet("/", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : pageSize;
            var redirectRules = await sender.Send(
                new ListRedirectRulesQuery(normalizedPage, normalizedPageSize),
                cancellationToken);

            return Results.Ok(redirectRules);
        });

        group.MapGet("/resolve", async (
            [FromQuery] string path,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var redirectMatch = await sender.Send(new ResolveRedirectQuery(path), cancellationToken);
            return redirectMatch is null ? Results.NotFound() : Results.Ok(redirectMatch);
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapDirectusWebhookEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/webhooks/directus", async (
            DirectusSlugWebhookRequest request,
            IRedirectRuleWriter redirectRuleWriter,
            CancellationToken cancellationToken) =>
        {
            if (!IsUpdateEvent(request.Event))
            {
                return Results.Ok(new { processed = false, reason = "unsupported_event" });
            }

            if (!TryResolveCollectionPrefix(request.Collection, out var prefix))
            {
                return Results.Ok(new { processed = false, reason = "unsupported_collection" });
            }

            var oldSlug = request.OldSlug ?? request.Previous?.Slug;
            var newSlug = request.NewSlug ?? request.Data?.Slug;

            if (string.IsNullOrWhiteSpace(oldSlug) || string.IsNullOrWhiteSpace(newSlug))
            {
                return Results.Ok(new { processed = false, reason = "missing_slug" });
            }

            if (string.Equals(oldSlug.Trim(), newSlug.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Results.Ok(new { processed = false, reason = "slug_unchanged" });
            }

            await redirectRuleWriter.UpsertAsync(
                $"{prefix}/{oldSlug}",
                $"{prefix}/{newSlug}",
                RedirectStatusCodes.PermanentRedirect,
                cancellationToken);

            return Results.Accepted();
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitingPolicyNames.PublicWebhook)
        .WithTags("Redirects");

        return endpoints;
    }

    private static bool IsUpdateEvent(string? eventName)
    {
        return string.Equals(eventName, "items.update", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(eventName, "update", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveCollectionPrefix(string? collection, out string prefix)
    {
        if (string.Equals(collection, "blog_posts", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "/blog";
            return true;
        }

        if (string.Equals(collection, "pages", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "/p";
            return true;
        }

        prefix = string.Empty;
        return false;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code.EndsWith(".not_found", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
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

    public sealed record CreateRedirectRuleRequest(string FromPath, string ToPath, int? StatusCode = null);

    public sealed record DirectusSlugWebhookRequest(
        string? Collection,
        string? Event,
        string? OldSlug,
        string? NewSlug,
        DirectusSlugPayload? Data,
        DirectusSlugPayload? Previous);

    public sealed record DirectusSlugPayload(string? Slug);
}
