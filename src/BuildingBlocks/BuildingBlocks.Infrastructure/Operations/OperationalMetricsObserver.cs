using BuildingBlocks.Application.Diagnostics;

namespace BuildingBlocks.Infrastructure.Operations;

internal sealed class OperationalMetricsObserver
{
    public OperationalMetricsObserver(IOperationalStateRegistry registry)
    {
        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.outbox.pending",
            () => registry.GetSnapshot().PendingOutboxMessages,
            unit: "messages");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.outbox.failed",
            () => registry.GetSnapshot().FailedOutboxMessages,
            unit: "messages");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.webhooks.payment.pending",
            () => registry.GetSnapshot().PendingPaymentWebhooks,
            unit: "messages");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.webhooks.payment.failed",
            () => registry.GetSnapshot().FailedPaymentWebhooks,
            unit: "messages");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.webhooks.shipping.pending",
            () => registry.GetSnapshot().PendingShippingWebhooks,
            unit: "messages");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.webhooks.shipping.failed",
            () => registry.GetSnapshot().FailedShippingWebhooks,
            unit: "messages");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.inventory.low_stock",
            () => registry.GetSnapshot().LowStockVariants,
            unit: "variants");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.inventory.reservation.active",
            () => registry.GetSnapshot().ActiveInventoryReservations,
            unit: "reservations");

        CommerceDiagnostics.Meter.CreateObservableGauge<long>(
            "commerce.reviews.moderation.pending",
            () => registry.GetSnapshot().PendingReviewModeration,
            unit: "reviews");
    }
}