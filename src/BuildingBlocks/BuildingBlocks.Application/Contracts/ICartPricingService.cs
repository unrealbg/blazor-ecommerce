using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface ICartPricingService
{
    Task<Result<CartPricingResult>> PriceAsync(CartPricingRequest request, CancellationToken cancellationToken);

    Task<Result<CouponValidationResult>> ValidateCouponAsync(
        CouponValidationRequest request,
        CancellationToken cancellationToken);
}
