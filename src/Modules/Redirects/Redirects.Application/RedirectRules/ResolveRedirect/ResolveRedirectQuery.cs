using BuildingBlocks.Application.Abstractions;

namespace Redirects.Application.RedirectRules.ResolveRedirect;

public sealed record ResolveRedirectQuery(string Path) : IQuery<RedirectMatch?>;
