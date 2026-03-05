namespace Orders.Application.Orders;

public interface ICheckoutIdempotencyRepository
{
    Task<CheckoutIdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddAsync(
        string idempotencyKey,
        string customerId,
        Guid orderId,
        DateTime createdOnUtc,
        CancellationToken cancellationToken);
}
