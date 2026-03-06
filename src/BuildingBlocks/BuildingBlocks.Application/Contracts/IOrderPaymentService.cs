using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IOrderPaymentService
{
    Task<OrderPaymentSnapshot?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Result> MarkPaidAsync(
        Guid orderId,
        Guid paymentIntentId,
        DateTime paidAtUtc,
        CancellationToken cancellationToken);

    Task<Result> MarkPaymentFailedAsync(
        Guid orderId,
        Guid paymentIntentId,
        string? failureMessage,
        DateTime failedAtUtc,
        CancellationToken cancellationToken);

    Task<Result> MarkCancelledAsync(
        Guid orderId,
        Guid paymentIntentId,
        string? reason,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken);

    Task<Result> MarkRefundedAsync(
        Guid orderId,
        Guid paymentIntentId,
        bool partial,
        DateTime refundedAtUtc,
        CancellationToken cancellationToken);
}
