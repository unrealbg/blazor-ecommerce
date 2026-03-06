namespace Shipping.Application.Providers;

public interface IShippingWebhookVerifier
{
    Task<bool> VerifyAsync(
        string provider,
        IReadOnlyDictionary<string, string> headers,
        string payload,
        CancellationToken cancellationToken);
}
