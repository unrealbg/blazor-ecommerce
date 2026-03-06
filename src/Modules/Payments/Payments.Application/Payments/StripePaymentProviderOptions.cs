namespace Payments.Application.Payments;

public sealed class StripePaymentProviderOptions
{
    public string SecretKey { get; set; } = string.Empty;

    public string PublishableKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;
}
