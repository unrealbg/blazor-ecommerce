namespace BuildingBlocks.Application.Contracts;

public sealed record CartPricingResult(
    string Currency,
    decimal SubtotalBeforeDiscountAmount,
    decimal SubtotalAmount,
    decimal LineDiscountTotalAmount,
    decimal CartDiscountTotalAmount,
    decimal ShippingBeforeDiscountAmount,
    decimal ShippingAmount,
    decimal ShippingDiscountTotalAmount,
    decimal GrandTotalAmount,
    string? AppliedCouponCode,
    IReadOnlyCollection<CartPricingLineResult> Lines,
    IReadOnlyCollection<PricingDiscountApplication> AppliedDiscounts,
    IReadOnlyCollection<string> Messages);
