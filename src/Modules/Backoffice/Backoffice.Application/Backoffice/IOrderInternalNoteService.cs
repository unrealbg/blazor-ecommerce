using BuildingBlocks.Domain.Results;

namespace Backoffice.Application.Backoffice;

public interface IOrderInternalNoteService
{
    Task<Result<Guid>> AddOrderNoteAsync(
        Guid orderId,
        string note,
        string? authorUserId,
        string? authorEmail,
        string? authorDisplayName,
        CancellationToken cancellationToken);
}
