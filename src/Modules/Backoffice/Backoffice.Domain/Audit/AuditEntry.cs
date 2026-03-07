using BuildingBlocks.Domain.Primitives;

namespace Backoffice.Domain.Audit;

public sealed class AuditEntry : Entity<Guid>
{
    private AuditEntry()
    {
    }

    private AuditEntry(
        Guid id,
        DateTime occurredAtUtc,
        string actionType,
        string targetType,
        string targetId,
        string summary,
        string? metadataJson,
        string? actorUserId,
        string? actorEmail,
        string? actorDisplayName,
        string? ipAddress,
        string? correlationId)
    {
        Id = id;
        OccurredAtUtc = occurredAtUtc;
        ActionType = actionType;
        TargetType = targetType;
        TargetId = targetId;
        Summary = summary;
        MetadataJson = metadataJson;
        ActorUserId = actorUserId;
        ActorEmail = actorEmail;
        ActorDisplayName = actorDisplayName;
        IpAddress = ipAddress;
        CorrelationId = correlationId;
    }

    public DateTime OccurredAtUtc { get; private set; }

    public string? ActorUserId { get; private set; }

    public string? ActorEmail { get; private set; }

    public string? ActorDisplayName { get; private set; }

    public string ActionType { get; private set; } = string.Empty;

    public string TargetType { get; private set; } = string.Empty;

    public string TargetId { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string? MetadataJson { get; private set; }

    public string? IpAddress { get; private set; }

    public string? CorrelationId { get; private set; }

    public static AuditEntry Create(
        DateTime occurredAtUtc,
        string actionType,
        string targetType,
        string targetId,
        string summary,
        string? metadataJson,
        string? actorUserId,
        string? actorEmail,
        string? actorDisplayName,
        string? ipAddress,
        string? correlationId)
    {
        return new AuditEntry(
            Guid.NewGuid(),
            occurredAtUtc,
            actionType.Trim(),
            targetType.Trim(),
            targetId.Trim(),
            summary.Trim(),
            Normalize(metadataJson),
            Normalize(actorUserId),
            Normalize(actorEmail),
            Normalize(actorDisplayName),
            Normalize(ipAddress),
            Normalize(correlationId));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
