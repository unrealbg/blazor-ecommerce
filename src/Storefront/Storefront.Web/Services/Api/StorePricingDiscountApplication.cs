namespace Storefront.Web.Services.Api;

public sealed class StorePricingDiscountApplication
{
    public string SourceType { get; init; } = string.Empty;

    public Guid SourceId { get; init; }

    public Guid PromotionId { get; init; }

    public Guid? CouponId { get; init; }

    public string ScopeType { get; init; } = string.Empty;

    public Guid? TargetLineVariantId { get; init; }

    public string Description { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "EUR";

    public string? Code { get; init; }
}
