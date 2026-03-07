namespace Catalog.Domain.Products;

public sealed record ProductCategoryDraft(Guid CategoryId, bool IsPrimary, int SortOrder);

public sealed record ProductVariantDraft(
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
    IReadOnlyCollection<VariantOptionAssignmentDraft> OptionAssignments);

public sealed record VariantOptionAssignmentDraft(
    Guid ProductOptionId,
    Guid ProductOptionValueId);

public sealed record ProductOptionDraft(
    Guid? Id,
    string Name,
    int Position,
    IReadOnlyCollection<ProductOptionValueDraft> Values);

public sealed record ProductOptionValueDraft(
    Guid? Id,
    string Value,
    int Position);

public sealed record ProductAttributeDraft(
    Guid? Id,
    string? GroupName,
    string Name,
    string Value,
    int Position,
    bool IsFilterable);

public sealed record ProductImageDraft(
    Guid? Id,
    Guid? VariantId,
    string SourceUrl,
    string? AltText,
    int Position,
    bool IsPrimary);

public sealed record ProductRelationDraft(
    Guid? Id,
    Guid RelatedProductId,
    ProductRelationType RelationType,
    int Position);
