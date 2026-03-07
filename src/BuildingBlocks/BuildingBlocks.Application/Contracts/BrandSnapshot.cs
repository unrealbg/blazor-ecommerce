namespace BuildingBlocks.Application.Contracts;

public sealed record BrandSnapshot(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? WebsiteUrl,
    string? LogoImageUrl,
    bool IsActive);
