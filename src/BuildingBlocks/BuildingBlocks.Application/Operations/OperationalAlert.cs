namespace BuildingBlocks.Application.Operations;

public sealed record OperationalAlert(
    string Code,
    string Severity,
    string Summary,
    string? Details,
    IReadOnlyDictionary<string, string?> Context,
    DateTime OccurredAtUtc);