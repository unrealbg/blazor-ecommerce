using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullCustomerOrderExportReader : ICustomerOrderExportReader
{
    public Task<IReadOnlyCollection<CustomerOrderExportRecord>> ListByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CustomerOrderExportRecord>>([]);
    }
}