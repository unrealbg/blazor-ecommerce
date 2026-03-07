namespace BuildingBlocks.Application.Contracts;

public sealed record ProductRelationSnapshot(
    Guid ProductId,
    string Slug,
    string Name,
    string? ImageUrl,
    string Currency,
    decimal Amount,
    string RelationType);
