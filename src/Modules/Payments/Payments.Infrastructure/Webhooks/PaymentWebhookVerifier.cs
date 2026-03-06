using Microsoft.Extensions.Options;
using Payments.Application.Payments;
using Payments.Application.Providers;

namespace Payments.Infrastructure.Webhooks;

internal sealed class PaymentWebhookVerifier(IOptions<PaymentsModuleOptions> options) : IPaymentWebhookVerifier
{
    private readonly PaymentsModuleOptions options = options.Value;

    public Task<bool> VerifyAsync(
        string provider,
        IReadOnlyDictionary<string, string> headers,
        string payload,
        CancellationToken cancellationToken)
    {
        if (!options.RequireWebhookVerification)
        {
            return Task.FromResult(true);
        }

        if (provider.Equals("Demo", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(true);
        }

        if (provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
        {
            var hasSignature = headers.TryGetValue("Stripe-Signature", out var signature) &&
                               !string.IsNullOrWhiteSpace(signature);
            return Task.FromResult(hasSignature);
        }

        return Task.FromResult(false);
    }
}
