namespace Storefront.Web.Services.Redirects;

public sealed class StorefrontRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IStorefrontRedirectLookup redirectLookup)
    {
        var requestPath = context.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            await next(context);
            return;
        }

        var normalizedRequestPath = NormalizeForComparison(requestPath);
        var match = await redirectLookup.ResolveAsync(normalizedRequestPath, context.RequestAborted);

        if (match is null)
        {
            await next(context);
            return;
        }

        if (string.Equals(
                normalizedRequestPath,
                NormalizeForComparison(match.ToPath),
                StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        var location = StorefrontRedirectLocationBuilder.BuildLocation(match.ToPath, context.Request.QueryString.Value);
        context.Response.StatusCode = match.StatusCode;
        context.Response.Headers.Location = location;
    }

    private static string NormalizeForComparison(string path)
    {
        var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
        var withoutQuery = queryIndex >= 0 ? path[..queryIndex] : path;

        var normalized = withoutQuery.Trim().ToLowerInvariant();
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = $"/{normalized}";
        }

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = $"/{normalized}";
            }
        }

        return normalized;
    }
}
