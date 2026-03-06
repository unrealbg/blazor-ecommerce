namespace Shipping.Domain.Shipping;

public enum CarrierWebhookInboxProcessingStatus
{
    Received = 1,
    Processed = 2,
    Failed = 3,
    Ignored = 4,
}
