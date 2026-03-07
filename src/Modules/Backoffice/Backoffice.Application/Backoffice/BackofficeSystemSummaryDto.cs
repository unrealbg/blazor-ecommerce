namespace Backoffice.Application.Backoffice;

public sealed record BackofficeSystemSummaryDto(
    bool DatabaseHealthy,
    bool RedisHealthy,
    int PendingOutboxMessages,
    int FailedOutboxMessages,
    int FailedPaymentWebhooks,
    int FailedShippingWebhooks,
    int PendingPaymentWebhooks,
    int PendingShippingWebhooks,
    int SearchDocumentCount);
