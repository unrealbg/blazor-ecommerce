using Shipping.Application.Providers;

namespace Shipping.Infrastructure.Webhooks;

internal sealed class ShippingWebhookVerifier : IShippingWebhookVerifier
{
    public Task<bool> VerifyAsync(
        string provider,
        IReadOnlyDictionary<string, string> headers,
        string payload,
        CancellationToken cancellationToken)
    {
        _ = provider;
        _ = headers;
        _ = payload;

        // Demo provider accepts all webhooks in local development.
        return Task.FromResult(true);
    }
}
