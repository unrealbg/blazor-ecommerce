namespace Storefront.Web.Services.Api;

public sealed record StoreCouponRequest(
    string Code,
    string? Description,
    Guid PromotionId,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer);
