using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullOrderPaymentService : IOrderPaymentService
{
    public Task<OrderPaymentSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Task.FromResult<OrderPaymentSnapshot?>(null);
    }

    public Task<Result> MarkPaidAsync(
        Guid orderId,
        Guid paymentIntentId,
        DateTime paidAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(new Error(
            "orders.payment.unavailable",
            "Order payment operations are unavailable.")));
    }

    public Task<Result> MarkPaymentFailedAsync(
        Guid orderId,
        Guid paymentIntentId,
        string? failureMessage,
        DateTime failedAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(new Error(
            "orders.payment.unavailable",
            "Order payment operations are unavailable.")));
    }

    public Task<Result> MarkCancelledAsync(
        Guid orderId,
        Guid paymentIntentId,
        string? reason,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(new Error(
            "orders.payment.unavailable",
            "Order payment operations are unavailable.")));
    }

    public Task<Result> MarkRefundedAsync(
        Guid orderId,
        Guid paymentIntentId,
        bool partial,
        DateTime refundedAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(new Error(
            "orders.payment.unavailable",
            "Order payment operations are unavailable.")));
    }
}
