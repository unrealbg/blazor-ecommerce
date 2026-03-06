using Payments.Domain.Payments;

namespace Payments.Application.Providers;

public interface IPaymentProvider
{
    string Name { get; }

    Task<PaymentProviderCreateResponse> CreatePaymentIntentAsync(
        PaymentProviderCreateRequest request,
        CancellationToken cancellationToken);

    Task<PaymentProviderConfirmResponse> ConfirmPaymentAsync(
        PaymentProviderConfirmRequest request,
        CancellationToken cancellationToken);

    Task<PaymentProviderCancelResponse> CancelPaymentAsync(
        PaymentProviderCancelRequest request,
        CancellationToken cancellationToken);

    Task<PaymentProviderRefundResponse> RefundPaymentAsync(
        PaymentProviderRefundRequest request,
        CancellationToken cancellationToken);

    Task<PaymentProviderWebhookResult> ParseWebhookAsync(
        string payload,
        CancellationToken cancellationToken);
}
