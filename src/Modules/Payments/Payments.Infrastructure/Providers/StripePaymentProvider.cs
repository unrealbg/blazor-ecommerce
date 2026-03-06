using Payments.Application.Providers;

namespace Payments.Infrastructure.Providers;

internal sealed class StripePaymentProvider : IPaymentProvider
{
    public string Name => "Stripe";

    public Task<PaymentProviderCreateResponse> CreatePaymentIntentAsync(
        PaymentProviderCreateRequest request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Stripe provider is not configured. Implement integration before use.");
    }

    public Task<PaymentProviderConfirmResponse> ConfirmPaymentAsync(
        PaymentProviderConfirmRequest request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Stripe provider is not configured. Implement integration before use.");
    }

    public Task<PaymentProviderCancelResponse> CancelPaymentAsync(
        PaymentProviderCancelRequest request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Stripe provider is not configured. Implement integration before use.");
    }

    public Task<PaymentProviderRefundResponse> RefundPaymentAsync(
        PaymentProviderRefundRequest request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Stripe provider is not configured. Implement integration before use.");
    }

    public Task<PaymentProviderWebhookResult> ParseWebhookAsync(string payload, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Stripe provider is not configured. Implement integration before use.");
    }
}
