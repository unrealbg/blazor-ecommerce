namespace BuildingBlocks.Infrastructure.Operations;

public sealed record OperationalSnapshot(
    int PendingOutboxMessages,
    int FailedOutboxMessages,
    int DeadLetteredOutboxMessages,
    double? OldestPendingOutboxAgeSeconds,
    int PendingPaymentWebhooks,
    int FailedPaymentWebhooks,
    int PendingShippingWebhooks,
    int FailedShippingWebhooks,
    int LowStockVariants,
    int ActiveInventoryReservations,
    int PendingReviewModeration,
    DateTime LastUpdatedAtUtc);