namespace Catalog.Application.Categories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsActive,
    string? SeoTitle,
    string? SeoDescription,
    string? ImageUrl,
    IReadOnlyCollection<CategoryBreadcrumbDto> Breadcrumbs);

public sealed record CategoryBreadcrumbDto(
    Guid Id,
    string Name,
    string Slug);
