namespace Backoffice.Application.Backoffice;

public sealed record BackofficeAuditEntryDto(
    Guid Id,
    DateTime OccurredAtUtc,
    string ActionType,
    string TargetType,
    string TargetId,
    string Summary,
    string? MetadataJson,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorDisplayName,
    string? IpAddress,
    string? CorrelationId);
