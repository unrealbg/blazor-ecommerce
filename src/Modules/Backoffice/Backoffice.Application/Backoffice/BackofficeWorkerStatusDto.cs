namespace Backoffice.Application.Backoffice;

public sealed record BackofficeWorkerStatusDto(
    string Name,
    string State,
    DateTime? LastStartedAtUtc,
    DateTime? LastSucceededAtUtc,
    DateTime? LastFailedAtUtc,
    string? LastError,
    int ConsecutiveFailureCount,
    double? LastDurationMs,
    string? LastCorrelationId,
    int? LastProcessedCount,
    string? LastNote);