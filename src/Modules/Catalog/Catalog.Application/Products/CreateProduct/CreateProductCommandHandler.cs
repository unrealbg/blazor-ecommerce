using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using Catalog.Application.Brands;
using Catalog.Application.Categories;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.CreateProduct;

public sealed class CreateProductCommandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IProductListCache productListCache,
    IInventoryStockProvisioner inventoryStockProvisioner,
    ICatalogUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(request.Name, cancellationToken);
        var brandId = await ResolveBrandIdAsync(request.BrandId, request.BrandName, cancellationToken);
        if (brandId.IsFailure)
        {
            return Result<Guid>.Failure(brandId.Error);
        }

        var categoryId = await ResolveDefaultCategoryIdAsync(
            request.DefaultCategoryId,
            request.CategorySlug,
            request.CategoryName,
            cancellationToken);
        if (categoryId.IsFailure)
        {
            return Result<Guid>.Failure(categoryId.Error);
        }

        var status = ParseStatus(request.Status, request.IsInStock);
        if (status.IsFailure)
        {
            return Result<Guid>.Failure(status.Error);
        }

        var productType = ParseProductType(request.ProductType, request.Variants.Count);
        if (productType.IsFailure)
        {
            return Result<Guid>.Failure(productType.Error);
        }

        var optionDrafts = request.Options
            .Select(option => new ProductOptionDraft(
                option.Id,
                option.Name,
                option.Position,
                option.Values
                    .Select(value => new ProductOptionValueDraft(value.Id, value.Value, value.Position))
                    .ToArray()))
            .ToArray();

        var variantDrafts = request.Variants.Count == 0
            ? [BuildDefaultVariant(request, slug)]
            : request.Variants
                .Select(variant => new ProductVariantDraft(
                    variant.Id,
                    variant.Sku,
                    variant.Name,
                    variant.Slug,
                    variant.Barcode,
                    variant.PriceAmount,
                    variant.Currency,
                    variant.CompareAtPriceAmount,
                    variant.WeightKg,
                    variant.IsActive,
                    variant.Position,
                    variant.OptionAssignments
                        .Select(assignment => new VariantOptionAssignmentDraft(
                            assignment.ProductOptionId,
                            assignment.ProductOptionValueId))
                        .ToArray()))
                .ToArray();

        foreach (var variant in variantDrafts)
        {
            if (await productRepository.SkuExistsAsync(variant.Sku.Trim(), null, cancellationToken))
            {
                return Result<Guid>.Failure(new Error("catalog.product.duplicate_sku", "Variant SKU must be unique."));
            }
        }

        var categories = request.Categories.Count == 0 && categoryId.Value is not null
            ? [new ProductCategoryDraft(categoryId.Value.Value, true, 0)]
            : request.Categories
                .Select(category => new ProductCategoryDraft(category.CategoryId, category.IsPrimary, category.SortOrder))
                .ToArray();

        var images = request.Images.Count == 0 && !string.IsNullOrWhiteSpace(request.ImageUrl)
            ? [new ProductImageDraft(null, null, request.ImageUrl!, request.Name, 0, true)]
            : request.Images
                .Select(image => new ProductImageDraft(
                    image.Id,
                    image.VariantId,
                    image.SourceUrl,
                    image.AltText,
                    image.Position,
                    image.IsPrimary))
                .ToArray();

        var relationDrafts = new List<ProductRelationDraft>();
        foreach (var relation in request.Relations)
        {
            var relationType = ParseRelationType(relation.RelationType);
            if (relationType.IsFailure)
            {
                return Result<Guid>.Failure(relationType.Error);
            }

            relationDrafts.Add(new ProductRelationDraft(
                relation.Id,
                relation.RelatedProductId,
                relationType.Value,
                relation.Position));
        }

        var productResult = Product.Create(
            request.Name,
            slug,
            request.ShortDescription,
            request.Description,
            brandId.Value,
            categoryId.Value,
            status.Value,
            productType.Value,
            request.SeoTitle,
            request.SeoDescription,
            request.CanonicalUrl,
            request.IsFeatured,
            request.PublishedAtUtc,
            DateTime.UtcNow,
            categories,
            variantDrafts,
            optionDrafts,
            request.Attributes
                .Select(attribute => new ProductAttributeDraft(
                    attribute.Id,
                    attribute.GroupName,
                    attribute.Name,
                    attribute.Value,
                    attribute.Position,
                    attribute.IsFilterable))
                .ToArray(),
            images,
            relationDrafts);
        if (productResult.IsFailure)
        {
            return Result<Guid>.Failure(productResult.Error);
        }

        await productRepository.AddAsync(productResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var defaultVariant = productResult.Value.Variants.First(variant => variant.Id == productResult.Value.DefaultVariantId);
        var ensureStockResult = await inventoryStockProvisioner.EnsureStockItemAsync(
            productResult.Value.Id,
            defaultVariant.Id,
            defaultVariant.Sku,
            request.IsInStock ? 100 : 0,
            isTracked: true,
            allowBackorder: false,
            cancellationToken);
        if (ensureStockResult.IsFailure)
        {
            return Result<Guid>.Failure(ensureStockResult.Error);
        }

        await productListCache.InvalidateAsync(cancellationToken);

        return Result<Guid>.Success(productResult.Value.Id);
    }

    private async Task<string> GenerateUniqueSlugAsync(string productName, CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.Generate(productName);
        var candidate = baseSlug;
        var suffix = 2;

        while (await productRepository.SlugExistsAsync(candidate, null, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private async Task<Result<Guid?>> ResolveBrandIdAsync(
        Guid? brandId,
        string? brandName,
        CancellationToken cancellationToken)
    {
        if (brandId is not null && brandId != Guid.Empty)
        {
            var existingBrand = await brandRepository.GetByIdAsync(brandId.Value, cancellationToken);
            return existingBrand is null
                ? Result<Guid?>.Failure(new Error("catalog.brand.not_found", "Brand was not found."))
                : Result<Guid?>.Success(existingBrand.Id);
        }

        if (string.IsNullOrWhiteSpace(brandName))
        {
            return Result<Guid?>.Success(null);
        }

        var slug = SlugGenerator.Generate(brandName);
        var existing = await brandRepository.GetBySlugAsync(slug, cancellationToken);
        if (existing is not null)
        {
            return Result<Guid?>.Success(existing.Id);
        }

        var createResult = Brand.Create(
            brandName,
            slug,
            description: null,
            websiteUrl: null,
            logoImageUrl: null,
            isActive: true,
            seoTitle: null,
            seoDescription: null,
            DateTime.UtcNow);
        if (createResult.IsFailure)
        {
            return Result<Guid?>.Failure(createResult.Error);
        }

        await brandRepository.AddAsync(createResult.Value, cancellationToken);
        return Result<Guid?>.Success(createResult.Value.Id);
    }

    private async Task<Result<Guid?>> ResolveDefaultCategoryIdAsync(
        Guid? categoryId,
        string? categorySlug,
        string? categoryName,
        CancellationToken cancellationToken)
    {
        if (categoryId is not null && categoryId != Guid.Empty)
        {
            var existingCategory = await categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
            return existingCategory is null
                ? Result<Guid?>.Failure(new Error("catalog.category.not_found", "Category was not found."))
                : Result<Guid?>.Success(existingCategory.Id);
        }

        if (string.IsNullOrWhiteSpace(categorySlug) && string.IsNullOrWhiteSpace(categoryName))
        {
            return Result<Guid?>.Success(null);
        }

        var resolvedSlug = string.IsNullOrWhiteSpace(categorySlug)
            ? SlugGenerator.Generate(categoryName!)
            : SlugGenerator.Generate(categorySlug);

        var existing = await categoryRepository.GetBySlugAsync(resolvedSlug, cancellationToken);
        if (existing is not null)
        {
            return Result<Guid?>.Success(existing.Id);
        }

        var createResult = Category.Create(
            categoryName ?? categorySlug!,
            resolvedSlug,
            description: null,
            parentCategoryId: null,
            sortOrder: 0,
            isActive: true,
            seoTitle: null,
            seoDescription: null,
            imageUrl: null,
            DateTime.UtcNow);
        if (createResult.IsFailure)
        {
            return Result<Guid?>.Failure(createResult.Error);
        }

        await categoryRepository.AddAsync(createResult.Value, cancellationToken);
        return Result<Guid?>.Success(createResult.Value.Id);
    }

    private static Result<ProductStatus> ParseStatus(string? value, bool legacyIsInStock)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ProductStatus>.Success(legacyIsInStock ? ProductStatus.Active : ProductStatus.Draft);
        }

        return Enum.TryParse<ProductStatus>(value, true, out var parsed)
            ? Result<ProductStatus>.Success(parsed)
            : Result<ProductStatus>.Failure(new Error(
                "catalog.product.status.invalid",
                "Product status is invalid."));
    }

    private static Result<ProductType> ParseProductType(string? value, int variantCount)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ProductType>.Success(variantCount > 1 ? ProductType.VariantParent : ProductType.Simple);
        }

        return Enum.TryParse<ProductType>(value, true, out var parsed)
            ? Result<ProductType>.Success(parsed)
            : Result<ProductType>.Failure(new Error(
                "catalog.product.type.invalid",
                "Product type is invalid."));
    }

    private static Result<ProductRelationType> ParseRelationType(string value)
    {
        return Enum.TryParse<ProductRelationType>(value, true, out var parsed)
            ? Result<ProductRelationType>.Success(parsed)
            : Result<ProductRelationType>.Failure(new Error(
                "catalog.product.relation_type.invalid",
                "Product relation type is invalid."));
    }

    private static ProductVariantDraft BuildDefaultVariant(CreateProductCommand request, string productSlug)
    {
        var normalizedSku = string.IsNullOrWhiteSpace(request.Sku)
            ? $"{productSlug.ToUpperInvariant().Replace("-", "_", StringComparison.Ordinal)}_DEFAULT"
            : request.Sku.Trim();

        return new ProductVariantDraft(
            Id: null,
            Sku: normalizedSku,
            Name: "Default",
            Slug: $"{productSlug}-default",
            Barcode: null,
            PriceAmount: request.Amount,
            Currency: request.Currency,
            CompareAtPriceAmount: request.CompareAtAmount,
            WeightKg: request.WeightKg,
            IsActive: true,
            Position: 0,
            OptionAssignments: []);
    }
}
