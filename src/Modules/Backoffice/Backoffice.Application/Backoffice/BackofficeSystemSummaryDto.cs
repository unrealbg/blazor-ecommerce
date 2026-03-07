namespace Backoffice.Application.Backoffice;

public sealed record BackofficeSystemSummaryDto(
    bool DatabaseHealthy,
    bool RedisHealthy,
    int PendingOutboxMessages,
    int FailedOutboxMessages,
    int DeadLetteredOutboxMessages,
    double? OldestPendingOutboxAgeSeconds,
    int FailedPaymentWebhooks,
    int FailedShippingWebhooks,
    int PendingPaymentWebhooks,
    int PendingShippingWebhooks,
    int SearchDocumentCount,
    int LowStockVariants,
    int ActiveInventoryReservations,
    int PendingReviewModeration,
    DateTime LastUpdatedAtUtc,
    IReadOnlyCollection<BackofficeWorkerStatusDto> Workers,
    IReadOnlyCollection<BackofficeOperationalAlertDto> Alerts);
