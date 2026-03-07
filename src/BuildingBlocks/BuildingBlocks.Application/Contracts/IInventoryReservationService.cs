using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IInventoryReservationService
{
    Task<Result> SyncCartReservationAsync(
        string cartId,
        Guid productId,
        Guid variantId,
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

    Task<Result> PromoteCartReservationsToOrderAsync(
        string cartId,
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken);

    Task<Result> ConsumeOrderReservationsAsync(
        Guid orderId,
        IReadOnlyCollection<InventoryCartLineRequest> lines,
        CancellationToken cancellationToken);

    Task<Result> ReleaseOrderReservationsAsync(Guid orderId, CancellationToken cancellationToken);
}
