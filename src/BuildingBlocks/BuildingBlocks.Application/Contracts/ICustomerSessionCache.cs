namespace BuildingBlocks.Application.Contracts;

public interface ICustomerSessionCache
{
    Task TouchCartSessionAsync(string sessionId, CancellationToken cancellationToken);

    Task TouchCustomerSessionAsync(
        Guid customerId,
        string sessionId,
        CancellationToken cancellationToken);
}
