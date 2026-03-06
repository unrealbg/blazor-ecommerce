namespace Payments.Domain.Payments;

public enum WebhookInboxProcessingStatus
{
    Received = 1,
    Processed = 2,
    Failed = 3,
    Ignored = 4,
}
