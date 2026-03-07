using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BuildingBlocks.Application.Diagnostics;

public static class CommerceDiagnostics
{
    public const string ActivitySourceName = "BlazorEcommerce";
    public const string MeterName = "BlazorEcommerce";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> CheckoutCounter = Meter.CreateCounter<long>("commerce.checkout.total");
    private static readonly Counter<long> PaymentIntentCounter = Meter.CreateCounter<long>("commerce.payment_intent.total");
    private static readonly Counter<long> PaymentWebhookFailureCounter = Meter.CreateCounter<long>("commerce.payment_webhook.failures");
    private static readonly Counter<long> ShipmentCounter = Meter.CreateCounter<long>("commerce.shipment.total");
    private static readonly Counter<long> ShippingWebhookFailureCounter = Meter.CreateCounter<long>("commerce.shipping_webhook.failures");
    private static readonly Counter<long> ReservationExpirationCounter = Meter.CreateCounter<long>("commerce.inventory_reservation.expired");
    private static readonly Counter<long> AdminActionCounter = Meter.CreateCounter<long>("commerce.admin_action.total");
    private static readonly Histogram<double> BackgroundJobDuration = Meter.CreateHistogram<double>("commerce.background_job.duration.ms");

    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }

    public static void RecordCheckout(bool success, string flow)
    {
        CheckoutCounter.Add(1, new KeyValuePair<string, object?>("result", success ? "success" : "failure"), new KeyValuePair<string, object?>("flow", flow));
    }

    public static void RecordPaymentIntentCreation(bool success, string provider)
    {
        PaymentIntentCounter.Add(1, new KeyValuePair<string, object?>("result", success ? "success" : "failure"), new KeyValuePair<string, object?>("provider", provider));
    }

    public static void RecordPaymentWebhookFailure(string provider)
    {
        PaymentWebhookFailureCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
    }

    public static void RecordShipment(string operation, string provider, string result)
    {
        ShipmentCounter.Add(1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("result", result));
    }

    public static void RecordShippingWebhookFailure(string provider)
    {
        ShippingWebhookFailureCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
    }

    public static void RecordReservationExpiration(int count)
    {
        ReservationExpirationCounter.Add(count);
    }

    public static void RecordAdminAction(string actionType)
    {
        AdminActionCounter.Add(1, new KeyValuePair<string, object?>("action_type", actionType));
    }

    public static void RecordBackgroundJobDuration(string jobName, double durationMs, string result)
    {
        BackgroundJobDuration.Record(durationMs,
            new KeyValuePair<string, object?>("job", jobName),
            new KeyValuePair<string, object?>("result", result));
    }
}