using BuildingBlocks.Application.Contracts;

namespace Cart.Application.Carts;

public sealed record CartDto(
    Guid Id,
    string CustomerId,
    string? AppliedCouponCode,
    string Currency,
    decimal SubtotalBeforeDiscountAmount,
    decimal SubtotalAmount,
    decimal LineDiscountTotalAmount,
    decimal CartDiscountTotalAmount,
    decimal GrandTotalAmount,
    IReadOnlyCollection<CartLineDto> Lines,
    IReadOnlyCollection<PricingDiscountApplication> AppliedDiscounts,
    IReadOnlyCollection<string> Messages);
