namespace Storefront.Web.Services.Api;

public sealed record StoreCoupon(
    Guid Id,
    string Code,
    string? Description,
    Guid PromotionId,
    int Status,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer,
    int TimesUsedTotal,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
