namespace BuildingBlocks.Application.Contracts;

public interface ICartCheckoutAccessor
{
    Task<CartCheckoutSnapshot?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken);

    Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken);
}
