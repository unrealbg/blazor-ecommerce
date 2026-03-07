namespace BuildingBlocks.Application.Auditing;

public sealed record AuditEntryInput(
    string ActionType,
    string TargetType,
    string TargetId,
    string Summary,
    string? MetadataJson,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorDisplayName,
    string? IpAddress,
    string? CorrelationId,
    DateTime? OccurredAtUtc = null);
