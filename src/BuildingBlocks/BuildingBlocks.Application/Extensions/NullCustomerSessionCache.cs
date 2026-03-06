using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullCustomerSessionCache : ICustomerSessionCache
{
    public Task TouchCartSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task TouchCustomerSessionAsync(
        Guid customerId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
