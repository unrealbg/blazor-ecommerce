namespace BuildingBlocks.Application.Contracts;

public interface IOrderPricingReader
{
    Task<OrderPricingSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
}
