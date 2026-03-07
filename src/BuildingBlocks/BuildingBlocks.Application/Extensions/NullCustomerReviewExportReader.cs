using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullCustomerReviewExportReader : ICustomerReviewExportReader
{
    public Task<IReadOnlyCollection<CustomerReviewExportRecord>> ListReviewsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CustomerReviewExportRecord>>([]);
    }

    public Task<IReadOnlyCollection<CustomerQuestionExportRecord>> ListQuestionsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CustomerQuestionExportRecord>>([]);
    }
}