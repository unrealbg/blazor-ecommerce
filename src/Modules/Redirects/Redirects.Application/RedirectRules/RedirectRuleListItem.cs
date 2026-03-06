namespace Redirects.Application.RedirectRules;

public sealed record RedirectRuleListItem(
    Guid Id,
    string FromPath,
    string ToPath,
    int StatusCode,
    bool IsActive,
    long HitCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastHitAtUtc);
