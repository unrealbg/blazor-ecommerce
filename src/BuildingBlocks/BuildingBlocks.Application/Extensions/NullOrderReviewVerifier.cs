using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullOrderReviewVerifier : IOrderReviewVerifier
{
    public Task<OrderReviewVerificationResult> VerifyPurchaseAsync(
        Guid customerId,
        Guid productId,
        Guid? variantId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new OrderReviewVerificationResult(false, null));
    }
}
