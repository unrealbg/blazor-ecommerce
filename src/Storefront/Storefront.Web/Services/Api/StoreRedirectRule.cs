namespace Storefront.Web.Services.Api;

public sealed record StoreRedirectRule(
    Guid Id,
    string FromPath,
    string ToPath,
    int StatusCode,
    bool IsActive,
    long HitCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastHitAtUtc);
