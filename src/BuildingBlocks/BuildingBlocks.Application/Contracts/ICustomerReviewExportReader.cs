namespace BuildingBlocks.Application.Contracts;

public interface ICustomerReviewExportReader
{
    Task<IReadOnlyCollection<CustomerReviewExportRecord>> ListReviewsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CustomerQuestionExportRecord>> ListQuestionsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}