using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Shipping.Domain.Shipping;

public sealed class CarrierWebhookInboxMessage : Entity<Guid>
{
    private CarrierWebhookInboxMessage()
    {
    }

    private CarrierWebhookInboxMessage(
        Guid id,
        string provider,
        string externalEventId,
        string eventType,
        string payload,
        DateTime receivedAtUtc)
    {
        Id = id;
        Provider = provider;
        ExternalEventId = externalEventId;
        EventType = eventType;
        Payload = payload;
        ReceivedAtUtc = receivedAtUtc;
        ProcessingStatus = CarrierWebhookInboxProcessingStatus.Received;
    }

    public string Provider { get; private set; } = string.Empty;

    public string ExternalEventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public CarrierWebhookInboxProcessingStatus ProcessingStatus { get; private set; }

    public string? Error { get; private set; }

    public static Result<CarrierWebhookInboxMessage> Create(
        string provider,
        string externalEventId,
        string eventType,
        string payload,
        DateTime receivedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result<CarrierWebhookInboxMessage>.Failure(new Error(
                "shipping.webhook.provider.required",
                "Webhook provider is required."));
        }

        if (string.IsNullOrWhiteSpace(externalEventId))
        {
            return Result<CarrierWebhookInboxMessage>.Failure(new Error(
                "shipping.webhook.external_event.required",
                "Webhook external event id is required."));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return Result<CarrierWebhookInboxMessage>.Failure(new Error(
                "shipping.webhook.event_type.required",
                "Webhook event type is required."));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<CarrierWebhookInboxMessage>.Failure(new Error(
                "shipping.webhook.payload.required",
                "Webhook payload is required."));
        }

        return Result<CarrierWebhookInboxMessage>.Success(new CarrierWebhookInboxMessage(
            Guid.NewGuid(),
            provider.Trim(),
            externalEventId.Trim(),
            eventType.Trim(),
            payload,
            receivedAtUtc));
    }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        ProcessingStatus = CarrierWebhookInboxProcessingStatus.Processed;
        ProcessedAtUtc = processedAtUtc;
        Error = null;
    }

    public void MarkIgnored(DateTime processedAtUtc, string? reason)
    {
        ProcessingStatus = CarrierWebhookInboxProcessingStatus.Ignored;
        ProcessedAtUtc = processedAtUtc;
        Error = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void MarkFailed(DateTime processedAtUtc, string error)
    {
        ProcessingStatus = CarrierWebhookInboxProcessingStatus.Failed;
        ProcessedAtUtc = processedAtUtc;
        Error = string.IsNullOrWhiteSpace(error) ? "Carrier webhook processing failed." : error.Trim();
    }

    public void RequeueForProcessing()
    {
        ProcessingStatus = CarrierWebhookInboxProcessingStatus.Received;
        ProcessedAtUtc = null;
        Error = null;
    }

    public void TrimPayloadForRetention(DateTime processedAtUtc)
    {
        Payload = "{}";
        ProcessedAtUtc ??= processedAtUtc;
    }
}
