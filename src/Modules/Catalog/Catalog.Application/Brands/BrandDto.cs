namespace Catalog.Application.Brands;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? WebsiteUrl,
    string? LogoImageUrl,
    bool IsActive,
    string? SeoTitle,
    string? SeoDescription);
