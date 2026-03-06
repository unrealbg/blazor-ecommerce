namespace Payments.Application.Payments;

public sealed class PaymentsModuleOptions
{
    public const string SectionName = "Payments";

    public string DefaultProvider { get; set; } = "Demo";

    public int PendingPaymentReservationHoldMinutes { get; set; } = 15;

    public int WebhookProcessingRetryCount { get; set; } = 3;

    public bool RequireWebhookVerification { get; set; }
}
