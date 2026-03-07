namespace BuildingBlocks.Application.Contracts;

public sealed record CouponValidationRequest(
    string Code,
    string? CustomerId,
    bool IsAuthenticated,
    IReadOnlyCollection<CartPricingLineRequest> Lines);
