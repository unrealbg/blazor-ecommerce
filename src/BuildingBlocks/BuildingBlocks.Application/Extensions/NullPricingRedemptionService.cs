using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullPricingRedemptionService : IPricingRedemptionService
{
    public Task<Result> RegisterOrderRedemptionsAsync(
        Guid orderId,
        string? customerId,
        IReadOnlyCollection<PricingDiscountApplication> applications,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}
