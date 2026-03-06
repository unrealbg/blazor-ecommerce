using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullOrderFulfillmentService : IOrderFulfillmentService
{
    public Task<OrderFulfillmentSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Task.FromResult<OrderFulfillmentSnapshot?>(null);
    }

    public Task<Result> MarkFulfillmentPendingAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(new Error(
            "orders.fulfillment.unavailable",
            "Order fulfillment operations are unavailable.")));
    }

    public Task<Result> MarkFulfilledAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(new Error(
            "orders.fulfillment.unavailable",
            "Order fulfillment operations are unavailable.")));
    }

    public Task<Result> MarkReturnedAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(new Error(
            "orders.fulfillment.unavailable",
            "Order fulfillment operations are unavailable.")));
    }
}
