using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Catalog.Domain.Brands;

public sealed class Brand : AggregateRoot<Guid>
{
    private Brand()
    {
    }

    private Brand(
        Guid id,
        string name,
        string slug,
        string? description,
        string? websiteUrl,
        string? logoImageUrl,
        bool isActive,
        string? seoTitle,
        string? seoDescription,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Description = description;
        WebsiteUrl = websiteUrl;
        LogoImageUrl = logoImageUrl;
        IsActive = isActive;
        SeoTitle = seoTitle;
        SeoDescription = seoDescription;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? WebsiteUrl { get; private set; }

    public string? LogoImageUrl { get; private set; }

    public bool IsActive { get; private set; }

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<Brand> Create(
        string name,
        string slug,
        string? description,
        string? websiteUrl,
        string? logoImageUrl,
        bool isActive,
        string? seoTitle,
        string? seoDescription,
        DateTime createdAtUtc)
    {
        var validation = Validate(name, slug, description, websiteUrl, logoImageUrl, seoTitle, seoDescription);
        if (validation.IsFailure)
        {
            return Result<Brand>.Failure(validation.Error);
        }

        return Result<Brand>.Success(new Brand(
            Guid.NewGuid(),
            name.Trim(),
            NormalizeSlug(slug),
            NormalizeOptional(description),
            NormalizeOptional(websiteUrl),
            NormalizeOptional(logoImageUrl),
            isActive,
            NormalizeOptional(seoTitle),
            NormalizeOptional(seoDescription),
            SpecifyUtc(createdAtUtc)));
    }

    public Result Update(
        string name,
        string slug,
        string? description,
        string? websiteUrl,
        string? logoImageUrl,
        bool isActive,
        string? seoTitle,
        string? seoDescription,
        DateTime updatedAtUtc)
    {
        var validation = Validate(name, slug, description, websiteUrl, logoImageUrl, seoTitle, seoDescription);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Slug = NormalizeSlug(slug);
        Description = NormalizeOptional(description);
        WebsiteUrl = NormalizeOptional(websiteUrl);
        LogoImageUrl = NormalizeOptional(logoImageUrl);
        IsActive = isActive;
        SeoTitle = NormalizeOptional(seoTitle);
        SeoDescription = NormalizeOptional(seoDescription);
        UpdatedAtUtc = SpecifyUtc(updatedAtUtc);
        return Result.Success();
    }

    private static Result Validate(
        string name,
        string slug,
        string? description,
        string? websiteUrl,
        string? logoImageUrl,
        string? seoTitle,
        string? seoDescription)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new Error("catalog.brand.name.required", "Brand name is required."));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result.Failure(new Error("catalog.brand.slug.required", "Brand slug is required."));
        }

        if (name.Trim().Length > 160)
        {
            return Result.Failure(new Error("catalog.brand.name.too_long", "Brand name is too long."));
        }

        if (NormalizeSlug(slug).Length > 220)
        {
            return Result.Failure(new Error("catalog.brand.slug.too_long", "Brand slug is too long."));
        }

        if (NormalizeOptional(description) is { Length: > 2000 })
        {
            return Result.Failure(new Error("catalog.brand.description.too_long", "Brand description is too long."));
        }

        if (!string.IsNullOrWhiteSpace(websiteUrl) && !Uri.TryCreate(websiteUrl, UriKind.Absolute, out _))
        {
            return Result.Failure(new Error("catalog.brand.website.invalid", "Brand website URL is invalid."));
        }

        if (!IsValidImageUrl(logoImageUrl))
        {
            return Result.Failure(new Error("catalog.brand.logo.invalid", "Brand logo image URL is invalid."));
        }

        if (NormalizeOptional(seoTitle) is { Length: > 200 })
        {
            return Result.Failure(new Error("catalog.brand.seo_title.too_long", "Brand SEO title is too long."));
        }

        if (NormalizeOptional(seoDescription) is { Length: > 320 })
        {
            return Result.Failure(new Error("catalog.brand.seo_description.too_long", "Brand SEO description is too long."));
        }

        return Result.Success();
    }

    private static bool IsValidImageUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
               value.StartsWith("/", StringComparison.Ordinal) ||
               Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static string NormalizeSlug(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime SpecifyUtc(DateTime value)
    {
        return value == default
            ? DateTime.UtcNow
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
