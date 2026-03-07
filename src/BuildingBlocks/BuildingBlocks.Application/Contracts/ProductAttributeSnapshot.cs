namespace BuildingBlocks.Application.Contracts;

public sealed record ProductAttributeSnapshot(
    Guid Id,
    string? GroupName,
    string Name,
    string Value,
    int Position,
    bool IsFilterable);
