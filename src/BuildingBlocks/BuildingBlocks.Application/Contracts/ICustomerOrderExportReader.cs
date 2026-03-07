namespace BuildingBlocks.Application.Contracts;

public interface ICustomerOrderExportReader
{
    Task<IReadOnlyCollection<CustomerOrderExportRecord>> ListByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}