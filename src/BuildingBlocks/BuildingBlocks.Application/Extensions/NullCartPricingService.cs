using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullCartPricingService : ICartPricingService
{
    public Task<Result<CartPricingResult>> PriceAsync(CartPricingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<CartPricingResult>.Failure(
            new Error("pricing.module.not_available", "Pricing module is not available.")));
    }

    public Task<Result<CouponValidationResult>> ValidateCouponAsync(
        CouponValidationRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<CouponValidationResult>.Failure(
            new Error("pricing.module.not_available", "Pricing module is not available.")));
    }
}
