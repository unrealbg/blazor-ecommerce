namespace BuildingBlocks.Application.Contracts;

public sealed record ProductSnapshot(
    Guid Id,
    string Name,
    string? Description,
    string Currency,
    decimal Amount,
    bool IsActive);
