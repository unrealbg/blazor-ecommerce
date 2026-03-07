using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IPricingRedemptionService
{
    Task<Result> RegisterOrderRedemptionsAsync(
        Guid orderId,
        string? customerId,
        IReadOnlyCollection<PricingDiscountApplication> applications,
        CancellationToken cancellationToken);
}
