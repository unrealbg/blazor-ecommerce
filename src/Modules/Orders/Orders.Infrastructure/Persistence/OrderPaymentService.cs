using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderPaymentService(OrdersDbContext dbContext) : IOrderPaymentService
{
    public async Task<OrderPaymentSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(entity => entity.Lines)
            .SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderPaymentSnapshot(
            order.Id,
            order.CustomerId,
            order.CheckoutSessionId,
            order.Total.Amount,
            order.Total.Currency,
            order.Status.ToString(),
            order.Lines
                .Select(line => new OrderPaymentLineSnapshot(line.ProductId, Sku: null, line.Quantity))
                .ToList());
    }

    public async Task<Result> MarkPaidAsync(
        Guid orderId,
        Guid paymentIntentId,
        DateTime paidAtUtc,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var markResult = order.MarkPaid(paymentIntentId, paidAtUtc);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkPaymentFailedAsync(
        Guid orderId,
        Guid paymentIntentId,
        string? failureMessage,
        DateTime failedAtUtc,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var markResult = order.MarkPaymentFailed(paymentIntentId, failureMessage);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkCancelledAsync(
        Guid orderId,
        Guid paymentIntentId,
        string? reason,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var markResult = order.MarkCancelled(paymentIntentId, reason);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MarkRefundedAsync(
        Guid orderId,
        Guid paymentIntentId,
        bool partial,
        DateTime refundedAtUtc,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(new Error("orders.not_found", "Order was not found."));
        }

        var markResult = order.MarkRefunded(paymentIntentId, partial);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
