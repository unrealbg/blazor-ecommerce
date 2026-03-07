namespace BuildingBlocks.Application.Contracts;

public sealed record CartPricingRequest(
    string? CustomerId,
    bool IsAuthenticated,
    IReadOnlyCollection<CartPricingLineRequest> Lines,
    string? CouponCode,
    ShippingPriceSelection? Shipping,
    bool BypassCache = false,
    bool StrictCouponValidation = false);
