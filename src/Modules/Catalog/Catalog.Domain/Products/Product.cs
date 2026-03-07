using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Catalog.Domain.Products.Events;

namespace Catalog.Domain.Products;

public sealed class Product : AggregateRoot<Guid>
{
    private readonly List<ProductCategory> _categories = [];
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductOption> _options = [];
    private readonly List<ProductAttribute> _attributes = [];
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductRelation> _relations = [];

    private Product()
    {
    }

    private Product(
        Guid id,
        string name,
        string slug,
        string? shortDescription,
        string? description,
        Guid? brandId,
        Guid? defaultCategoryId,
        ProductStatus status,
        ProductType productType,
        string? seoTitle,
        string? seoDescription,
        string? canonicalUrl,
        bool isFeatured,
        DateTime? publishedAtUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        ShortDescription = shortDescription;
        Description = description;
        BrandId = brandId;
        DefaultCategoryId = defaultCategoryId;
        Status = status;
        ProductType = productType;
        SeoTitle = seoTitle;
        SeoDescription = seoDescription;
        CanonicalUrl = canonicalUrl;
        IsFeatured = isFeatured;
        PublishedAtUtc = publishedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? ShortDescription { get; private set; }

    public string? Description { get; private set; }

    public Guid? BrandId { get; private set; }

    public Guid? DefaultCategoryId { get; private set; }

    public ProductStatus Status { get; private set; }

    public ProductType ProductType { get; private set; }

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    public string? CanonicalUrl { get; private set; }

    public bool IsFeatured { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public Guid DefaultVariantId { get; private set; }

    public bool IsActive => Status == ProductStatus.Active;

    public IReadOnlyCollection<ProductCategory> Categories => _categories.AsReadOnly();

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public IReadOnlyCollection<ProductOption> Options => _options.AsReadOnly();

    public IReadOnlyCollection<ProductAttribute> Attributes => _attributes.AsReadOnly();

    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public IReadOnlyCollection<ProductRelation> Relations => _relations.AsReadOnly();

    public static Result<Product> Create(
        string name,
        string slug,
        string? shortDescription,
        string? description,
        Guid? brandId,
        Guid? defaultCategoryId,
        ProductStatus status,
        ProductType productType,
        string? seoTitle,
        string? seoDescription,
        string? canonicalUrl,
        bool isFeatured,
        DateTime? publishedAtUtc,
        DateTime createdAtUtc,
        IReadOnlyCollection<ProductCategoryDraft> categories,
        IReadOnlyCollection<ProductVariantDraft> variants,
        IReadOnlyCollection<ProductOptionDraft> options,
        IReadOnlyCollection<ProductAttributeDraft> attributes,
        IReadOnlyCollection<ProductImageDraft> images,
        IReadOnlyCollection<ProductRelationDraft> relations)
    {
        var validation = ValidateBasic(name, slug, shortDescription, description, seoTitle, seoDescription, canonicalUrl);
        if (validation.IsFailure)
        {
            return Result<Product>.Failure(validation.Error);
        }

        var product = new Product(
            Guid.NewGuid(),
            name.Trim(),
            NormalizeSlug(slug),
            NormalizeOptional(shortDescription),
            NormalizeOptional(description),
            NormalizeGuid(brandId),
            NormalizeGuid(defaultCategoryId),
            status,
            productType,
            NormalizeOptional(seoTitle),
            NormalizeOptional(seoDescription),
            NormalizeOptional(canonicalUrl),
            isFeatured,
            NormalizePublishedAt(publishedAtUtc),
            SpecifyUtc(createdAtUtc));

        var rebuildResult = product.RebuildCollections(categories, variants, options, attributes, images, relations, product.CreatedAtUtc);
        if (rebuildResult.IsFailure)
        {
            return Result<Product>.Failure(rebuildResult.Error);
        }

        product.RaiseDomainEvent(new ProductCreated(product.Id, product.DefaultVariantId));
        return Result<Product>.Success(product);
    }

    public Result Update(
        string name,
        string slug,
        string? shortDescription,
        string? description,
        Guid? brandId,
        Guid? defaultCategoryId,
        ProductType productType,
        string? seoTitle,
        string? seoDescription,
        string? canonicalUrl,
        bool isFeatured,
        DateTime? publishedAtUtc,
        IReadOnlyCollection<ProductCategoryDraft> categories,
        IReadOnlyCollection<ProductVariantDraft> variants,
        IReadOnlyCollection<ProductOptionDraft> options,
        IReadOnlyCollection<ProductAttributeDraft> attributes,
        IReadOnlyCollection<ProductImageDraft> images,
        IReadOnlyCollection<ProductRelationDraft> relations,
        DateTime updatedAtUtc)
    {
        var validation = ValidateBasic(name, slug, shortDescription, description, seoTitle, seoDescription, canonicalUrl);
        if (validation.IsFailure)
        {
            return validation;
        }

        var normalizedSlug = NormalizeSlug(slug);
        if (!string.Equals(Slug, normalizedSlug, StringComparison.Ordinal))
        {
            RaiseDomainEvent(new ProductSlugChanged(Id, Slug, normalizedSlug));
            Slug = normalizedSlug;
        }

        Name = name.Trim();
        ShortDescription = NormalizeOptional(shortDescription);
        Description = NormalizeOptional(description);
        BrandId = NormalizeGuid(brandId);
        DefaultCategoryId = NormalizeGuid(defaultCategoryId);
        ProductType = productType;
        SeoTitle = NormalizeOptional(seoTitle);
        SeoDescription = NormalizeOptional(seoDescription);
        CanonicalUrl = NormalizeOptional(canonicalUrl);
        IsFeatured = isFeatured;
        PublishedAtUtc = NormalizePublishedAt(publishedAtUtc);

        var rebuildResult = RebuildCollections(categories, variants, options, attributes, images, relations, updatedAtUtc);
        if (rebuildResult.IsFailure)
        {
            return rebuildResult;
        }

        UpdatedAtUtc = SpecifyUtc(updatedAtUtc);
        RaiseDomainEvent(new ProductUpdated(Id));
        return Result.Success();
    }

    public Result Activate(DateTime utcNow)
    {
        if (Status == ProductStatus.Active)
        {
            return Result.Success();
        }

        if (_variants.Count == 0 || _variants.All(variant => !variant.IsActive))
        {
            return Result.Failure(new Error(
                "catalog.variant.not_sellable",
                "At least one active variant is required before activation."));
        }

        Status = ProductStatus.Active;
        PublishedAtUtc ??= SpecifyUtc(utcNow);
        UpdatedAtUtc = SpecifyUtc(utcNow);
        RaiseDomainEvent(new ProductActivated(Id));
        return Result.Success();
    }

    public Result Archive(DateTime utcNow)
    {
        if (Status == ProductStatus.Archived)
        {
            return Result.Success();
        }

        Status = ProductStatus.Archived;
        UpdatedAtUtc = SpecifyUtc(utcNow);
        RaiseDomainEvent(new ProductArchived(Id));
        return Result.Success();
    }

    public Result<bool> UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<bool>.Failure(new Error("catalog.product.slug.required", "Product slug is required."));
        }

        var normalizedSlug = NormalizeSlug(slug);
        if (normalizedSlug.Length > 220)
        {
            return Result<bool>.Failure(new Error("catalog.product.slug.too_long", "Product slug is too long."));
        }

        if (string.Equals(Slug, normalizedSlug, StringComparison.Ordinal))
        {
            return Result<bool>.Success(false);
        }

        RaiseDomainEvent(new ProductSlugChanged(Id, Slug, normalizedSlug));
        Slug = normalizedSlug;
        return Result<bool>.Success(true);
    }

    private Result RebuildCollections(
        IReadOnlyCollection<ProductCategoryDraft> categories,
        IReadOnlyCollection<ProductVariantDraft> variants,
        IReadOnlyCollection<ProductOptionDraft> options,
        IReadOnlyCollection<ProductAttributeDraft> attributes,
        IReadOnlyCollection<ProductImageDraft> images,
        IReadOnlyCollection<ProductRelationDraft> relations,
        DateTime utcNow)
    {
        var normalizedOptions = BuildOptions(options);
        if (normalizedOptions.IsFailure)
        {
            return normalizedOptions;
        }

        var normalizedVariants = BuildVariants(variants, normalizedOptions.Value, utcNow);
        if (normalizedVariants.IsFailure)
        {
            return normalizedVariants;
        }

        var normalizedCategories = BuildCategories(categories, DefaultCategoryId);
        if (normalizedCategories.IsFailure)
        {
            return normalizedCategories;
        }

        var normalizedAttributes = BuildAttributes(attributes);
        if (normalizedAttributes.IsFailure)
        {
            return normalizedAttributes;
        }

        var normalizedImages = BuildImages(images, normalizedVariants.Value.Select(variant => variant.Id).ToHashSet(), utcNow);
        if (normalizedImages.IsFailure)
        {
            return normalizedImages;
        }

        var normalizedRelations = BuildRelations(relations, utcNow);
        if (normalizedRelations.IsFailure)
        {
            return normalizedRelations;
        }

        _options.Clear();
        _options.AddRange(normalizedOptions.Value);

        _variants.Clear();
        _variants.AddRange(normalizedVariants.Value.OrderBy(variant => variant.Position).ThenBy(variant => variant.Sku, StringComparer.OrdinalIgnoreCase));

        _categories.Clear();
        _categories.AddRange(normalizedCategories.Value.OrderBy(category => category.SortOrder).ThenBy(category => category.CategoryId));

        _attributes.Clear();
        _attributes.AddRange(normalizedAttributes.Value.OrderBy(attribute => attribute.Position).ThenBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase));

        _images.Clear();
        _images.AddRange(normalizedImages.Value.OrderBy(image => image.Position).ThenBy(image => image.Id));

        _relations.Clear();
        _relations.AddRange(normalizedRelations.Value.OrderBy(relation => relation.Position).ThenBy(relation => relation.RelatedProductId));

        DefaultVariantId = _variants
            .OrderBy(variant => variant.Position)
            .ThenBy(variant => variant.Sku, StringComparer.OrdinalIgnoreCase)
            .Select(variant => variant.Id)
            .First();

        if (DefaultCategoryId is not null && _categories.All(category => category.CategoryId != DefaultCategoryId.Value))
        {
            return Result.Failure(new Error(
                "catalog.product.primary_category.missing",
                "Default category must be part of the product category assignments."));
        }

        return Result.Success();
    }

    private static Result<List<ProductCategory>> BuildCategories(
        IReadOnlyCollection<ProductCategoryDraft> categories,
        Guid? defaultCategoryId)
    {
        var normalized = categories
            .Where(category => category.CategoryId != Guid.Empty)
            .GroupBy(category => category.CategoryId)
            .Select(group => group.OrderBy(item => item.SortOrder).First())
            .ToList();

        if (normalized.Count > 0 && normalized.Count(category => category.IsPrimary) != 1)
        {
            return Result<List<ProductCategory>>.Failure(new Error(
                "catalog.product.primary_category.invalid",
                "Exactly one primary category is required when categories are assigned."));
        }

        if (defaultCategoryId is not null &&
            normalized.Count > 0 &&
            normalized.All(category => category.CategoryId != defaultCategoryId.Value))
        {
            return Result<List<ProductCategory>>.Failure(new Error(
                "catalog.product.primary_category.missing",
                "Default category must be assigned to the product."));
        }

        return Result<List<ProductCategory>>.Success(
            normalized
                .Select(category => new ProductCategory(category.CategoryId, category.IsPrimary, category.SortOrder))
                .ToList());
    }

    private static Result<List<ProductOption>> BuildOptions(IReadOnlyCollection<ProductOptionDraft> options)
    {
        var normalized = new List<ProductOption>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var option in options.OrderBy(item => item.Position).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var optionName = NormalizeOptional(option.Name);
            if (optionName is null)
            {
                return Result<List<ProductOption>>.Failure(new Error(
                    "catalog.product.option.name.required",
                    "Option name is required."));
            }

            if (!usedNames.Add(optionName))
            {
                return Result<List<ProductOption>>.Failure(new Error(
                    "catalog.product.option.duplicate",
                    "Option names must be unique within a product."));
            }

            var values = new List<ProductOptionValue>();
            var usedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in option.Values.OrderBy(item => item.Position).ThenBy(item => item.Value, StringComparer.OrdinalIgnoreCase))
            {
                var normalizedValue = NormalizeOptional(value.Value);
                if (normalizedValue is null)
                {
                    return Result<List<ProductOption>>.Failure(new Error(
                        "catalog.product.option_value.required",
                        "Option value is required."));
                }

                if (!usedValues.Add(normalizedValue))
                {
                    return Result<List<ProductOption>>.Failure(new Error(
                        "catalog.product.option_value.duplicate",
                        "Option values must be unique within an option."));
                }

                values.Add(new ProductOptionValue(value.Id.GetValueOrDefault(Guid.NewGuid()), normalizedValue, value.Position));
            }

            normalized.Add(new ProductOption(option.Id.GetValueOrDefault(Guid.NewGuid()), optionName, option.Position, values));
        }

        return Result<List<ProductOption>>.Success(normalized);
    }

    private static Result<List<ProductVariant>> BuildVariants(
        IReadOnlyCollection<ProductVariantDraft> variants,
        IReadOnlyCollection<ProductOption> options,
        DateTime utcNow)
    {
        if (variants.Count == 0)
        {
            return Result<List<ProductVariant>>.Failure(new Error(
                "catalog.product.variant.required",
                "At least one product variant is required."));
        }

        var optionLookup = options.ToDictionary(option => option.Id);
        var optionValueLookup = options
            .SelectMany(option => option.Values.Select(value => new { OptionId = option.Id, Value = value }))
            .ToDictionary(item => item.Value.Id, item => item.OptionId);

        var normalized = new List<ProductVariant>();
        var usedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedCombinations = new HashSet<string>(StringComparer.Ordinal);

        foreach (var variant in variants.OrderBy(item => item.Position).ThenBy(item => item.Sku, StringComparer.OrdinalIgnoreCase))
        {
            var normalizedSku = NormalizeOptional(variant.Sku);
            if (normalizedSku is null)
            {
                return Result<List<ProductVariant>>.Failure(new Error(
                    "catalog.product.variant.sku.required",
                    "Variant SKU is required."));
            }

            if (!usedSkus.Add(normalizedSku))
            {
                return Result<List<ProductVariant>>.Failure(new Error(
                    "catalog.product.duplicate_sku",
                    "Variant SKU must be unique."));
            }

            var currency = NormalizeOptional(variant.Currency)?.ToUpperInvariant();
            if (currency is null || currency.Length != 3)
            {
                return Result<List<ProductVariant>>.Failure(new Error(
                    "catalog.product.variant.currency.invalid",
                    "Variant currency is invalid."));
            }

            if (variant.PriceAmount < 0m)
            {
                return Result<List<ProductVariant>>.Failure(new Error(
                    "catalog.product.variant.price.invalid",
                    "Variant price must be zero or positive."));
            }

            if (variant.CompareAtPriceAmount is < 0m)
            {
                return Result<List<ProductVariant>>.Failure(new Error(
                    "catalog.product.variant.compare_at.invalid",
                    "Compare-at price must be zero or positive."));
            }

            if (variant.WeightKg is < 0m)
            {
                return Result<List<ProductVariant>>.Failure(new Error(
                    "catalog.product.variant.weight.invalid",
                    "Variant weight must be zero or positive."));
            }

            var variantId = variant.Id.GetValueOrDefault(Guid.NewGuid());
            var assignments = new List<VariantOptionAssignment>();
            var usedOptionIds = new HashSet<Guid>();

            foreach (var assignment in variant.OptionAssignments)
            {
                if (!optionLookup.ContainsKey(assignment.ProductOptionId) ||
                    !optionValueLookup.TryGetValue(assignment.ProductOptionValueId, out var ownerOptionId) ||
                    ownerOptionId != assignment.ProductOptionId)
                {
                    return Result<List<ProductVariant>>.Failure(new Error(
                        "catalog.product.variant.option_assignment.invalid",
                        "Variant option assignment is invalid."));
                }

                if (!usedOptionIds.Add(assignment.ProductOptionId))
                {
                    return Result<List<ProductVariant>>.Failure(new Error(
                        "catalog.product.variant.option_assignment.duplicate",
                        "Variant cannot contain duplicate option assignments."));
                }

                assignments.Add(new VariantOptionAssignment(
                    variantId,
                    assignment.ProductOptionId,
                    assignment.ProductOptionValueId));
            }

            var combinationKey = string.Join(
                "|",
                assignments
                    .OrderBy(item => item.ProductOptionId)
                    .Select(item => $"{item.ProductOptionId:N}:{item.ProductOptionValueId:N}"));

            if (combinationKey.Length != 0 && !usedCombinations.Add(combinationKey))
            {
                return Result<List<ProductVariant>>.Failure(new Error(
                    "catalog.product.variant.option_combination.duplicate",
                    "Variant option combinations must be unique."));
            }

            var createdAtUtc = SpecifyUtc(utcNow);
            var entity = new ProductVariant(
                variantId,
                normalizedSku,
                NormalizeOptional(variant.Name),
                NormalizeOptional(variant.Slug),
                NormalizeOptional(variant.Barcode),
                decimal.Round(variant.PriceAmount, 2, MidpointRounding.AwayFromZero),
                currency,
                variant.CompareAtPriceAmount is null
                    ? null
                    : decimal.Round(variant.CompareAtPriceAmount.Value, 2, MidpointRounding.AwayFromZero),
                variant.WeightKg,
                variant.IsActive,
                variant.Position,
                createdAtUtc);
            entity.Update(
                normalizedSku,
                NormalizeOptional(variant.Name),
                NormalizeOptional(variant.Slug),
                NormalizeOptional(variant.Barcode),
                decimal.Round(variant.PriceAmount, 2, MidpointRounding.AwayFromZero),
                currency,
                variant.CompareAtPriceAmount is null
                    ? null
                    : decimal.Round(variant.CompareAtPriceAmount.Value, 2, MidpointRounding.AwayFromZero),
                variant.WeightKg,
                variant.IsActive,
                variant.Position,
                createdAtUtc,
                assignments);

            normalized.Add(entity);
        }

        return Result<List<ProductVariant>>.Success(normalized);
    }

    private static Result<List<ProductAttribute>> BuildAttributes(IReadOnlyCollection<ProductAttributeDraft> attributes)
    {
        var normalized = new List<ProductAttribute>();
        foreach (var attribute in attributes.OrderBy(item => item.Position).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var name = NormalizeOptional(attribute.Name);
            var value = NormalizeOptional(attribute.Value);
            if (name is null || value is null)
            {
                return Result<List<ProductAttribute>>.Failure(new Error(
                    "catalog.product.attribute.invalid",
                    "Product attribute name and value are required."));
            }

            normalized.Add(new ProductAttribute(
                attribute.Id.GetValueOrDefault(Guid.NewGuid()),
                NormalizeOptional(attribute.GroupName),
                name,
                value,
                attribute.Position,
                attribute.IsFilterable));
        }

        return Result<List<ProductAttribute>>.Success(normalized);
    }

    private static Result<List<ProductImage>> BuildImages(
        IReadOnlyCollection<ProductImageDraft> images,
        IReadOnlySet<Guid> variantIds,
        DateTime utcNow)
    {
        var normalized = new List<ProductImage>();
        var primaryByContext = new HashSet<string>(StringComparer.Ordinal);

        foreach (var image in images.OrderBy(item => item.Position).ThenBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase))
        {
            var sourceUrl = NormalizeOptional(image.SourceUrl);
            if (sourceUrl is null ||
                !(sourceUrl.StartsWith("/", StringComparison.Ordinal) || Uri.TryCreate(sourceUrl, UriKind.Absolute, out _)))
            {
                return Result<List<ProductImage>>.Failure(new Error(
                    "catalog.product.image.invalid",
                    "Product image URL is invalid."));
            }

            if (image.VariantId is not null && !variantIds.Contains(image.VariantId.Value))
            {
                return Result<List<ProductImage>>.Failure(new Error(
                    "catalog.product.image.variant.invalid",
                    "Product image references an unknown variant."));
            }

            if (image.IsPrimary)
            {
                var contextKey = image.VariantId?.ToString("N") ?? "product";
                if (!primaryByContext.Add(contextKey))
                {
                    return Result<List<ProductImage>>.Failure(new Error(
                        "catalog.product.image.primary.invalid",
                        "Only one primary image is allowed per product or variant."));
                }
            }

            normalized.Add(new ProductImage(
                image.Id.GetValueOrDefault(Guid.NewGuid()),
                image.VariantId,
                sourceUrl,
                NormalizeOptional(image.AltText),
                image.Position,
                image.IsPrimary,
                SpecifyUtc(utcNow)));
        }

        return Result<List<ProductImage>>.Success(normalized);
    }

    private Result<List<ProductRelation>> BuildRelations(
        IReadOnlyCollection<ProductRelationDraft> relations,
        DateTime utcNow)
    {
        var normalized = new List<ProductRelation>();
        var duplicates = new HashSet<string>(StringComparer.Ordinal);

        foreach (var relation in relations.OrderBy(item => item.Position).ThenBy(item => item.RelatedProductId))
        {
            if (relation.RelatedProductId == Guid.Empty)
            {
                return Result<List<ProductRelation>>.Failure(new Error(
                    "catalog.product.relation.related_product.required",
                    "Related product id is required."));
            }

            if (relation.RelatedProductId == Id)
            {
                return Result<List<ProductRelation>>.Failure(new Error(
                    "catalog.product.relation.self_reference",
                    "Product cannot relate to itself."));
            }

            var duplicateKey = $"{relation.RelationType}:{relation.RelatedProductId:N}";
            if (!duplicates.Add(duplicateKey))
            {
                return Result<List<ProductRelation>>.Failure(new Error(
                    "catalog.product.relation.duplicate",
                    "Duplicate product relation is not allowed."));
            }

            normalized.Add(new ProductRelation(
                relation.Id.GetValueOrDefault(Guid.NewGuid()),
                relation.RelatedProductId,
                relation.RelationType,
                relation.Position,
                SpecifyUtc(utcNow)));
        }

        return Result<List<ProductRelation>>.Success(normalized);
    }

    private static Result ValidateBasic(
        string name,
        string slug,
        string? shortDescription,
        string? description,
        string? seoTitle,
        string? seoDescription,
        string? canonicalUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new Error("catalog.product.name.required", "Product name is required."));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result.Failure(new Error("catalog.product.slug.required", "Product slug is required."));
        }

        if (name.Trim().Length > 220)
        {
            return Result.Failure(new Error("catalog.product.name.too_long", "Product name is too long."));
        }

        if (NormalizeSlug(slug).Length > 220)
        {
            return Result.Failure(new Error("catalog.product.slug.too_long", "Product slug is too long."));
        }

        if (NormalizeOptional(shortDescription) is { Length: > 800 })
        {
            return Result.Failure(new Error(
                "catalog.product.short_description.too_long",
                "Short description is too long."));
        }

        if (NormalizeOptional(description) is { Length: > 8000 })
        {
            return Result.Failure(new Error(
                "catalog.product.description.too_long",
                "Description is too long."));
        }

        if (NormalizeOptional(seoTitle) is { Length: > 200 })
        {
            return Result.Failure(new Error("catalog.product.seo_title.too_long", "SEO title is too long."));
        }

        if (NormalizeOptional(seoDescription) is { Length: > 320 })
        {
            return Result.Failure(new Error(
                "catalog.product.seo_description.too_long",
                "SEO description is too long."));
        }

        if (!string.IsNullOrWhiteSpace(canonicalUrl) &&
            !(canonicalUrl.StartsWith("/", StringComparison.Ordinal) || Uri.TryCreate(canonicalUrl, UriKind.Absolute, out _)))
        {
            return Result.Failure(new Error(
                "catalog.product.canonical.invalid",
                "Canonical URL is invalid."));
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

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value == Guid.Empty ? null : value;
    }

    private static DateTime? NormalizePublishedAt(DateTime? value)
    {
        return value is null ? null : SpecifyUtc(value.Value);
    }

    private static DateTime SpecifyUtc(DateTime value)
    {
        return value == default
            ? DateTime.UtcNow
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
