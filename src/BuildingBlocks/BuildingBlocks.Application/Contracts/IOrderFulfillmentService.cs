using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IOrderFulfillmentService
{
    Task<OrderFulfillmentSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Result> MarkFulfillmentPendingAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken);

    Task<Result> MarkFulfilledAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken);

    Task<Result> MarkReturnedAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken);
}
