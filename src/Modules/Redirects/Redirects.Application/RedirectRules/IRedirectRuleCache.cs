namespace Redirects.Application.RedirectRules;

public interface IRedirectRuleCache
{
    Task<RedirectMatch?> GetAsync(string normalizedFromPath, CancellationToken cancellationToken);

    Task InvalidateAsync(CancellationToken cancellationToken);
}
