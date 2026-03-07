using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? ShortDescription,
    string? Description,
    Guid? BrandId,
    string? BrandName,
    Guid? DefaultCategoryId,
    string? CategorySlug,
    string? CategoryName,
    string? Status,
    string? ProductType,
    string? SeoTitle,
    string? SeoDescription,
    string? CanonicalUrl,
    bool IsFeatured,
    DateTime? PublishedAtUtc,
    string Currency,
    decimal Amount,
    string? Sku,
    decimal? CompareAtAmount,
    decimal? WeightKg,
    string? ImageUrl,
    bool IsInStock,
    IReadOnlyCollection<CreateProductCategoryModel> Categories,
    IReadOnlyCollection<CreateProductOptionModel> Options,
    IReadOnlyCollection<CreateProductVariantModel> Variants,
    IReadOnlyCollection<CreateProductAttributeModel> Attributes,
    IReadOnlyCollection<CreateProductImageModel> Images,
    IReadOnlyCollection<CreateProductRelationModel> Relations) : ICommand<Guid>;

public sealed record CreateProductCategoryModel(Guid CategoryId, bool IsPrimary, int SortOrder);

public sealed record CreateProductOptionModel(
    Guid? Id,
    string Name,
    int Position,
    IReadOnlyCollection<CreateProductOptionValueModel> Values);

public sealed record CreateProductOptionValueModel(Guid? Id, string Value, int Position);

public sealed record CreateProductVariantModel(
    Guid? Id,
    string Sku,
    string? Name,
    string? Slug,
    string? Barcode,
    decimal PriceAmount,
    string Currency,
    decimal? CompareAtPriceAmount,
    decimal? WeightKg,
    bool IsActive,
    int Position,
    IReadOnlyCollection<CreateProductVariantOptionAssignmentModel> OptionAssignments);

public sealed record CreateProductVariantOptionAssignmentModel(Guid ProductOptionId, Guid ProductOptionValueId);

public sealed record CreateProductAttributeModel(
    Guid? Id,
    string? GroupName,
    string Name,
    string Value,
    int Position,
    bool IsFilterable);

public sealed record CreateProductImageModel(
    Guid? Id,
    Guid? VariantId,
    string SourceUrl,
    string? AltText,
    int Position,
    bool IsPrimary);

public sealed record CreateProductRelationModel(
    Guid? Id,
    Guid RelatedProductId,
    string RelationType,
    int Position);
