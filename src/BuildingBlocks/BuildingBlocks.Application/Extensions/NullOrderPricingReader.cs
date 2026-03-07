using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullOrderPricingReader : IOrderPricingReader
{
    public Task<OrderPricingSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Task.FromResult<OrderPricingSnapshot?>(null);
    }
}
