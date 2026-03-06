using BuildingBlocks.Application.Abstractions;

namespace Redirects.Application.RedirectRules.ResolveRedirect;

public sealed class ResolveRedirectQueryHandler(IRedirectLookupService redirectLookupService)
    : IQueryHandler<ResolveRedirectQuery, RedirectMatch?>
{
    public Task<RedirectMatch?> Handle(ResolveRedirectQuery request, CancellationToken cancellationToken)
    {
        return redirectLookupService.ResolveAsync(request.Path, cancellationToken);
    }
}
