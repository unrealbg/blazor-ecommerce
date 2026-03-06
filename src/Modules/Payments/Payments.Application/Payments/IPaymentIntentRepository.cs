using Payments.Domain.Payments;

namespace Payments.Application.Payments;

public interface IPaymentIntentRepository
{
    Task AddAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken);

    Task<PaymentIntent?> GetByIdAsync(Guid paymentIntentId, CancellationToken cancellationToken);

    Task<PaymentIntent?> GetByProviderIntentIdAsync(
        string provider,
        string providerPaymentIntentId,
        CancellationToken cancellationToken);

    Task<PaymentIntent?> GetLatestByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PaymentIntent>> ListAsync(
        string? provider,
        PaymentIntentStatus? status,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(
        string? provider,
        PaymentIntentStatus? status,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        CancellationToken cancellationToken);
}
