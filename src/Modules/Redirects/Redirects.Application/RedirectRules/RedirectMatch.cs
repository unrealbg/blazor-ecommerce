namespace Redirects.Application.RedirectRules;

public sealed record RedirectMatch(
    string FromPath,
    string ToPath,
    int StatusCode);
