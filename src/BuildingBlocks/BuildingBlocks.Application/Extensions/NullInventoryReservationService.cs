using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullInventoryReservationService : IInventoryReservationService
{
    public Task<Result> SyncCartReservationAsync(
        string cartId,
        Guid productId,
        string? sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ReleaseAllCartReservationsAsync(string cartId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<InventoryReservationValidationResult>> ValidateCartReservationsAsync(
        string cartId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<InventoryReservationValidationResult>.Success(
            new InventoryReservationValidationResult(true, [])));
    }

    public Task<Result> ConsumeCartReservationsAsync(
        string cartId,
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}
