namespace Storefront.Web.Services.Redirects;

public interface IStorefrontRedirectLookup
{
    Task<StorefrontRedirectMatch?> ResolveAsync(string requestPath, CancellationToken cancellationToken);
}

public sealed record StorefrontRedirectMatch(string FromPath, string ToPath, int StatusCode);
