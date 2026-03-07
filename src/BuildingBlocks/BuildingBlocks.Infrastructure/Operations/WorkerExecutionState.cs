namespace BuildingBlocks.Infrastructure.Operations;

public sealed record WorkerExecutionState(
    string Name,
    DateTime? LastStartedAtUtc,
    DateTime? LastSucceededAtUtc,
    DateTime? LastFailedAtUtc,
    string? LastError,
    int ConsecutiveFailureCount,
    double? LastDurationMs,
    string State,
    string? LastCorrelationId,
    int? LastProcessedCount,
    string? LastNote);