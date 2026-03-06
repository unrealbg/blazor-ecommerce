namespace BuildingBlocks.Application.Contracts;

public sealed record ProductSnapshot(
    Guid Id,
    string Name,
    string? Description,
    string Currency,
    decimal Amount,
    bool IsActive,
    bool IsInStock,
    string Slug = "",
    string? Brand = null,
    string? CategorySlug = null,
    string? CategoryName = null,
    string? ImageUrl = null,
    DateTime CreatedAtUtc = default,
    DateTime UpdatedAtUtc = default);
