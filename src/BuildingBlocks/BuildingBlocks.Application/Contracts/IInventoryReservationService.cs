using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IInventoryReservationService
{
    Task<Result> SyncCartReservationAsync(
        string cartId,
        Guid productId,
        string? sku,
        int quantity,
        CancellationToken cancellationToken);

    Task<Result> ReleaseAllCartReservationsAsync(string cartId, CancellationToken cancellationToken);

    Task<Result<InventoryReservationValidationResult>> ValidateCartReservationsAsync(
        string cartId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken);

    Task<Result> ConsumeCartReservationsAsync(
        string cartId,
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken);
}
