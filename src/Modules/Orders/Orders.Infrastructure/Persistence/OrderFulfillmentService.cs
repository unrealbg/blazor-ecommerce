using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderFulfillmentService(OrdersDbContext dbContext) : IOrderFulfillmentService
{
    public async Task<OrderFulfillmentSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        return new OrderFulfillmentSnapshot(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.FulfillmentStatus.ToString(),
            order.ShippingMethodCode,
            order.ShippingMethodName,
            order.ShippingPrice.Amount,
            order.ShippingPrice.Currency,
            order.Total.Amount,
            order.Total.Currency,
            new OrderFulfillmentAddressSnapshot(
                order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName,
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country,
                order.ShippingAddress.Phone));
    }

    public async Task<Result> MarkFulfillmentPendingAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var markResult = order.MarkFulfillmentPending(shipmentId);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkFulfilledAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var markResult = order.MarkFulfilled(shipmentId);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkReturnedAsync(
        Guid orderId,
        Guid shipmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var markResult = order.MarkReturned(shipmentId);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
