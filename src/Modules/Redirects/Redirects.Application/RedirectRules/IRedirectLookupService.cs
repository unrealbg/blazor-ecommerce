namespace Redirects.Application.RedirectRules;

public interface IRedirectLookupService
{
    Task<RedirectMatch?> ResolveAsync(string requestPath, CancellationToken cancellationToken);
}
