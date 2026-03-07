namespace BuildingBlocks.Application.Contracts;

public interface IOrderReviewVerifier
{
    Task<OrderReviewVerificationResult> VerifyPurchaseAsync(
        Guid customerId,
        Guid productId,
        Guid? variantId,
        CancellationToken cancellationToken);
}
