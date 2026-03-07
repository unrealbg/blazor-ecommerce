namespace BuildingBlocks.Application.Contracts;

public sealed record ProductImageSnapshot(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string SourceUrl,
    string? AltText,
    int Position,
    bool IsPrimary);
