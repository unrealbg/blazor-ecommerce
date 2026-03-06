using BuildingBlocks.Domain.Abstractions;
using Microsoft.AspNetCore.Http;
using Redirects.Application.RedirectRules;
using Redirects.Domain.RedirectRules;

namespace Redirects.Api;

internal sealed class RedirectRequestMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IRedirectLookupService redirectLookupService,
        IRedirectHitRecorder redirectHitRecorder,
        IClock clock)
    {
        var requestPath = context.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            await next(context);
            return;
        }

        var redirectMatch = await redirectLookupService.ResolveAsync(requestPath, context.RequestAborted);
        if (redirectMatch is null)
        {
            await next(context);
            return;
        }

        var normalizedToPathResult = RedirectPathNormalizer.NormalizeToPath(redirectMatch.ToPath);
        if (normalizedToPathResult.IsFailure)
        {
            await next(context);
            return;
        }

        if (string.Equals(
                redirectMatch.FromPath,
                RedirectPathNormalizer.NormalizeForComparison(normalizedToPathResult.Value),
                StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        var location = RedirectLocationBuilder.BuildLocation(
            normalizedToPathResult.Value,
            context.Request.QueryString.Value);

        context.Response.StatusCode = redirectMatch.StatusCode;
        context.Response.Headers.Location = location;

        redirectHitRecorder.RecordHit(redirectMatch.FromPath, clock.UtcNow);
    }
}
