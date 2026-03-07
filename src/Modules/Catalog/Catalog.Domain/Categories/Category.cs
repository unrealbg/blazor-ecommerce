using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Catalog.Domain.Categories.Events;

namespace Catalog.Domain.Categories;

public sealed class Category : AggregateRoot<Guid>
{
    private Category()
    {
    }

    private Category(
        Guid id,
        string name,
        string slug,
        string? description,
        Guid? parentCategoryId,
        int sortOrder,
        bool isActive,
        string? seoTitle,
        string? seoDescription,
        string? imageUrl,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Description = description;
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        IsActive = isActive;
        SeoTitle = seoTitle;
        SeoDescription = seoDescription;
        ImageUrl = imageUrl;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid? ParentCategoryId { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    public string? ImageUrl { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<Category> Create(
        string name,
        string slug,
        string? description,
        Guid? parentCategoryId,
        int sortOrder,
        bool isActive,
        string? seoTitle,
        string? seoDescription,
        string? imageUrl,
        DateTime createdAtUtc)
    {
        var validation = Validate(name, slug, description, seoTitle, seoDescription, imageUrl);
        if (validation.IsFailure)
        {
            return Result<Category>.Failure(validation.Error);
        }

        if (parentCategoryId == Guid.Empty)
        {
            return Result<Category>.Failure(new Error(
                "catalog.category.parent.invalid",
                "Parent category id is invalid."));
        }

        return Result<Category>.Success(new Category(
            Guid.NewGuid(),
            name.Trim(),
            NormalizeSlug(slug),
            NormalizeOptional(description),
            parentCategoryId,
            sortOrder,
            isActive,
            NormalizeOptional(seoTitle),
            NormalizeOptional(seoDescription),
            NormalizeOptional(imageUrl),
            SpecifyUtc(createdAtUtc)));
    }

    public Result Update(
        string name,
        string slug,
        string? description,
        Guid? parentCategoryId,
        int sortOrder,
        bool isActive,
        string? seoTitle,
        string? seoDescription,
        string? imageUrl,
        DateTime updatedAtUtc)
    {
        var validation = Validate(name, slug, description, seoTitle, seoDescription, imageUrl);
        if (validation.IsFailure)
        {
            return validation;
        }

        if (parentCategoryId == Id)
        {
            return Result.Failure(new Error(
                "catalog.category.cycle_detected",
                "Category cannot be its own parent."));
        }

        var normalizedSlug = NormalizeSlug(slug);
        if (!string.Equals(Slug, normalizedSlug, StringComparison.Ordinal))
        {
            RaiseDomainEvent(new CategorySlugChanged(Id, Slug, normalizedSlug));
        }

        Name = name.Trim();
        Slug = normalizedSlug;
        Description = NormalizeOptional(description);
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        IsActive = isActive;
        SeoTitle = NormalizeOptional(seoTitle);
        SeoDescription = NormalizeOptional(seoDescription);
        ImageUrl = NormalizeOptional(imageUrl);
        UpdatedAtUtc = SpecifyUtc(updatedAtUtc);
        return Result.Success();
    }

    private static Result Validate(
        string name,
        string slug,
        string? description,
        string? seoTitle,
        string? seoDescription,
        string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new Error("catalog.category.name.required", "Category name is required."));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result.Failure(new Error("catalog.category.slug.required", "Category slug is required."));
        }

        if (name.Trim().Length > 200)
        {
            return Result.Failure(new Error("catalog.category.name.too_long", "Category name is too long."));
        }

        if (NormalizeSlug(slug).Length > 220)
        {
            return Result.Failure(new Error("catalog.category.slug.too_long", "Category slug is too long."));
        }

        if (NormalizeOptional(description) is { Length: > 2000 })
        {
            return Result.Failure(new Error(
                "catalog.category.description.too_long",
                "Category description is too long."));
        }

        if (NormalizeOptional(seoTitle) is { Length: > 200 })
        {
            return Result.Failure(new Error("catalog.category.seo_title.too_long", "Category SEO title is too long."));
        }

        if (NormalizeOptional(seoDescription) is { Length: > 320 })
        {
            return Result.Failure(new Error(
                "catalog.category.seo_description.too_long",
                "Category SEO description is too long."));
        }

        if (!IsValidImageUrl(imageUrl))
        {
            return Result.Failure(new Error(
                "catalog.category.image_url.invalid",
                "Category image URL is invalid."));
        }

        return Result.Success();
    }

    private static string NormalizeSlug(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsValidImageUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
               value.StartsWith("/", StringComparison.Ordinal) ||
               Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static DateTime SpecifyUtc(DateTime value)
    {
        return value == default
            ? DateTime.UtcNow
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
