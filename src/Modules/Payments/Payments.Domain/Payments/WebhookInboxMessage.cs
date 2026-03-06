using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Payments.Domain.Payments;

public sealed class WebhookInboxMessage : Entity<Guid>
{
    private WebhookInboxMessage()
    {
    }

    private WebhookInboxMessage(
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
        ProcessingStatus = WebhookInboxProcessingStatus.Received;
    }

    public string Provider { get; private set; } = string.Empty;

    public string ExternalEventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public WebhookInboxProcessingStatus ProcessingStatus { get; private set; }

    public string? Error { get; private set; }

    public static Result<WebhookInboxMessage> Create(
        string provider,
        string externalEventId,
        string eventType,
        string payload,
        DateTime receivedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result<WebhookInboxMessage>.Failure(new Error(
                "payments.webhook.provider.required",
                "Webhook provider is required."));
        }

        if (string.IsNullOrWhiteSpace(externalEventId))
        {
            return Result<WebhookInboxMessage>.Failure(new Error(
                "payments.webhook.external_event.required",
                "Webhook external event id is required."));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return Result<WebhookInboxMessage>.Failure(new Error(
                "payments.webhook.event_type.required",
                "Webhook event type is required."));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<WebhookInboxMessage>.Failure(new Error(
                "payments.webhook.payload.required",
                "Webhook payload is required."));
        }

        return Result<WebhookInboxMessage>.Success(new WebhookInboxMessage(
            Guid.NewGuid(),
            provider.Trim(),
            externalEventId.Trim(),
            eventType.Trim(),
            payload,
            receivedAtUtc));
    }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        ProcessingStatus = WebhookInboxProcessingStatus.Processed;
        ProcessedAtUtc = processedAtUtc;
        Error = null;
    }

    public void MarkIgnored(DateTime processedAtUtc, string? reason)
    {
        ProcessingStatus = WebhookInboxProcessingStatus.Ignored;
        ProcessedAtUtc = processedAtUtc;
        Error = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void MarkFailed(DateTime processedAtUtc, string error)
    {
        ProcessingStatus = WebhookInboxProcessingStatus.Failed;
        ProcessedAtUtc = processedAtUtc;
        Error = string.IsNullOrWhiteSpace(error) ? "Webhook processing failed." : error.Trim();
    }
}
