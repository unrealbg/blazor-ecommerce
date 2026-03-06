using BuildingBlocks.Domain.Results;

namespace Inventory.Application.Stock;

public interface IInventoryUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);

    Task<Result<TResult>> ExecuteWithConcurrencyRetryAsync<TResult>(
        Func<CancellationToken, Task<Result<TResult>>> operation,
        int retryCount,
        CancellationToken cancellationToken);
}
