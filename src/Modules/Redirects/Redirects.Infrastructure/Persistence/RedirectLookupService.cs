using Redirects.Application.RedirectRules;
using Redirects.Domain.RedirectRules;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectLookupService(IRedirectRuleCache redirectRuleCache) : IRedirectLookupService
{
    public async Task<RedirectMatch?> ResolveAsync(string requestPath, CancellationToken cancellationToken)
    {
        var normalizedFromResult = RedirectPathNormalizer.NormalizeFromPath(requestPath);
        if (normalizedFromResult.IsFailure)
        {
            return null;
        }

        var redirectMatch = await redirectRuleCache.GetAsync(normalizedFromResult.Value, cancellationToken);
        if (redirectMatch is null)
        {
            return null;
        }

        var normalizedToResult = RedirectPathNormalizer.NormalizeToPath(redirectMatch.ToPath);
        if (normalizedToResult.IsFailure)
        {
            return null;
        }

        if (string.Equals(
                normalizedFromResult.Value,
                RedirectPathNormalizer.NormalizeForComparison(normalizedToResult.Value),
                StringComparison.Ordinal))
        {
            return null;
        }

        return new RedirectMatch(normalizedFromResult.Value, normalizedToResult.Value, redirectMatch.StatusCode);
    }
}
