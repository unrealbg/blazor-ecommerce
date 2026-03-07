using BuildingBlocks.Application.Operations;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Operations;
using BuildingBlocks.Infrastructure.Persistence;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payments.Domain.Payments;
using Reviews.Domain.Reviews;
using Shipping.Domain.Shipping;

namespace Backoffice.Infrastructure.Services;

internal sealed class OperationalSnapshotBackgroundService(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobMonitor backgroundJobMonitor,
    IOperationalStateRegistry operationalStateRegistry,
    IOperationalAlertSink operationalAlertSink,
    IConfiguration configuration,
    ILogger<OperationalSnapshotBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var execution = backgroundJobMonitor.Start("operational-snapshot-refresh");
            try
            {
                var snapshot = await BuildSnapshotAsync(stoppingToken);
                operationalStateRegistry.UpdateSnapshot(snapshot);

                await PublishWarningsAsync(snapshot, stoppingToken);
                execution.Complete(note: "snapshot-updated");
            }
            catch (Exception exception)
            {
                execution.Fail(exception);
                logger.LogError(exception, "Failed to refresh operational snapshot.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<OperationalSnapshot> BuildSnapshotAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxDbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var paymentsDbContext = scope.ServiceProvider.GetRequiredService<Payments.Infrastructure.Persistence.PaymentsDbContext>();
        var shippingDbContext = scope.ServiceProvider.GetRequiredService<Shipping.Infrastructure.Persistence.ShippingDbContext>();
        var inventoryDbContext = scope.ServiceProvider.GetRequiredService<Inventory.Infrastructure.Persistence.InventoryDbContext>();
        var reviewsDbContext = scope.ServiceProvider.GetRequiredService<Reviews.Infrastructure.Persistence.ReviewsDbContext>();

        var failedOutboxMessages = await outboxDbContext.OutboxMessages
            .Where(message => message.Error != null)
            .Select(message => message.Error)
            .ToListAsync(cancellationToken);

        var deadLetteredOutbox = failedOutboxMessages.Count(error => OutboxFailureState.Parse(error).DeadLettered);
        var oldestPending = await outboxDbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Select(message => (DateTime?)message.OccurredOnUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationalSnapshot(
            PendingOutboxMessages: await outboxDbContext.OutboxMessages.CountAsync(message => message.ProcessedOnUtc == null, cancellationToken),
            FailedOutboxMessages: failedOutboxMessages.Count,
            DeadLetteredOutboxMessages: deadLetteredOutbox,
            OldestPendingOutboxAgeSeconds: oldestPending is null ? null : (DateTime.UtcNow - oldestPending.Value).TotalSeconds,
            PendingPaymentWebhooks: await paymentsDbContext.WebhookInboxMessages.CountAsync(message => message.ProcessingStatus == WebhookInboxProcessingStatus.Received, cancellationToken),
            FailedPaymentWebhooks: await paymentsDbContext.WebhookInboxMessages.CountAsync(message => message.ProcessingStatus == WebhookInboxProcessingStatus.Failed, cancellationToken),
            PendingShippingWebhooks: await shippingDbContext.CarrierWebhookInboxMessages.CountAsync(message => message.ProcessingStatus == CarrierWebhookInboxProcessingStatus.Received, cancellationToken),
            FailedShippingWebhooks: await shippingDbContext.CarrierWebhookInboxMessages.CountAsync(message => message.ProcessingStatus == CarrierWebhookInboxProcessingStatus.Failed, cancellationToken),
            LowStockVariants: await inventoryDbContext.StockItems.CountAsync(item => item.IsTracked && item.AvailableQuantity > 0 && item.AvailableQuantity <= 5, cancellationToken),
            ActiveInventoryReservations: await inventoryDbContext.StockReservations.CountAsync(reservation => reservation.Status == StockReservationStatus.Active, cancellationToken),
            PendingReviewModeration: await reviewsDbContext.ProductReviews.CountAsync(review => review.Status == ModerationStatus.Pending, cancellationToken),
            LastUpdatedAtUtc: DateTime.UtcNow);
    }

    private async Task PublishWarningsAsync(OperationalSnapshot snapshot, CancellationToken cancellationToken)
    {
        var outboxThreshold = configuration.GetValue<int?>("Readiness:OutboxWarningThreshold") ?? 250;
        var failedWebhookThreshold = configuration.GetValue<int?>("Readiness:FailedWebhookWarningThreshold") ?? 25;

        if (snapshot.PendingOutboxMessages >= outboxThreshold)
        {
            await operationalAlertSink.PublishAsync(
                new OperationalAlert(
                    "outbox.backlog.threshold_exceeded",
                    "warning",
                    "Outbox backlog threshold exceeded.",
                    null,
                    new Dictionary<string, string?>
                    {
                        ["pendingOutboxMessages"] = snapshot.PendingOutboxMessages.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    },
                    DateTime.UtcNow),
                cancellationToken);
        }

        if (snapshot.FailedPaymentWebhooks >= failedWebhookThreshold)
        {
            await operationalAlertSink.PublishAsync(
                new OperationalAlert(
                    "payments.webhook.failures.threshold_exceeded",
                    "warning",
                    "Payment webhook failures exceeded threshold.",
                    null,
                    new Dictionary<string, string?>
                    {
                        ["failedPaymentWebhooks"] = snapshot.FailedPaymentWebhooks.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    },
                    DateTime.UtcNow),
                cancellationToken);
        }
    }
}