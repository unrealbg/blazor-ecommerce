namespace Payments.Application.Providers;

public interface IPaymentWebhookVerifier
{
    Task<bool> VerifyAsync(
        string provider,
        IReadOnlyDictionary<string, string> headers,
        string payload,
        CancellationToken cancellationToken);
}
