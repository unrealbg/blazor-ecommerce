using BuildingBlocks.Application.Contracts;
using Catalog.Domain.Brands;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductCatalogReader(
    CatalogDbContext dbContext,
    IInventoryAvailabilityReader inventoryAvailabilityReader)
    : IProductCatalogReader
{
    public async Task<ProductSnapshot?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await QueryProducts()
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var relatedProducts = await LoadRelatedProductsAsync(product, cancellationToken);
        var brands = await LoadBrandsAsync(product, cancellationToken);
        var categories = await LoadCategoriesAsync(product, cancellationToken);
        var productAvailability = await inventoryAvailabilityReader.GetByProductIdAsync(product.Id, cancellationToken);
        var variantAvailability = await inventoryAvailabilityReader.GetByVariantIdsAsync(
            product.Variants.Select(variant => variant.Id).ToArray(),
            cancellationToken);

        return MapProduct(
            product,
            brands,
            categories,
            relatedProducts,
            productAvailability,
            variantAvailability);
    }

    public async Task<ProductSnapshot?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken)
    {
        var product = await QueryProducts()
            .SingleOrDefaultAsync(item => item.Variants.Any(variant => variant.Id == variantId), cancellationToken);

        return product is null
            ? null
            : await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> ListAllAsync(CancellationToken cancellationToken)
    {
        var products = await QueryProducts()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        var brands = await LoadBrandsAsync(products, cancellationToken);
        var categories = await LoadCategoriesAsync(products, cancellationToken);
        var relatedProducts = products.ToDictionary(product => product.Id);
        var productAvailability = await inventoryAvailabilityReader.GetByProductIdsAsync(
            products.Select(product => product.Id).ToArray(),
            cancellationToken);
        var variantAvailability = await inventoryAvailabilityReader.GetByVariantIdsAsync(
            products.SelectMany(product => product.Variants).Select(variant => variant.Id).ToArray(),
            cancellationToken);

        return products
            .Select(product =>
            {
                productAvailability.TryGetValue(product.Id, out var aggregateAvailability);
                return MapProduct(
                    product,
                    brands,
                    categories,
                    relatedProducts,
                    aggregateAvailability,
                    variantAvailability);
            })
            .ToList();
    }

    private IQueryable<Product> QueryProducts()
    {
        return dbContext.Products
            .AsNoTracking()
            .Include(product => product.Categories)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.OptionAssignments)
            .Include(product => product.Options)
                .ThenInclude(option => option.Values)
            .Include(product => product.Attributes)
            .Include(product => product.Images)
            .Include(product => product.Relations)
            .AsSplitQuery();
    }

    private async Task<IReadOnlyDictionary<Guid, Product>> LoadRelatedProductsAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        var relatedIds = product.Relations
            .Select(relation => relation.RelatedProductId)
            .Distinct()
            .ToArray();

        if (relatedIds.Length == 0)
        {
            return new Dictionary<Guid, Product>();
        }

        var relatedProducts = await QueryProducts()
            .Where(item => relatedIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        return relatedProducts.ToDictionary(item => item.Id);
    }

    private async Task<IReadOnlyDictionary<Guid, Brand>> LoadBrandsAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        Guid[] brandIds = product.BrandId is null ? [] : [product.BrandId.Value];
        return await LoadBrandsAsync(brandIds, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, Brand>> LoadBrandsAsync(
        IReadOnlyCollection<Product> products,
        CancellationToken cancellationToken)
    {
        var brandIds = products
            .Where(product => product.BrandId is not null)
            .Select(product => product.BrandId!.Value)
            .Distinct()
            .ToArray();

        return await LoadBrandsAsync(brandIds, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, Brand>> LoadBrandsAsync(
        IReadOnlyCollection<Guid> brandIds,
        CancellationToken cancellationToken)
    {
        if (brandIds.Count == 0)
        {
            return new Dictionary<Guid, Brand>();
        }

        var brands = await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brandIds.Contains(brand.Id))
            .ToListAsync(cancellationToken);

        return brands.ToDictionary(brand => brand.Id);
    }

    private async Task<IReadOnlyDictionary<Guid, Category>> LoadCategoriesAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        var categoryIds = product.Categories
            .Select(category => category.CategoryId)
            .ToHashSet();

        if (product.DefaultCategoryId is not null)
        {
            categoryIds.Add(product.DefaultCategoryId.Value);
        }

        return await LoadCategoriesAsync(categoryIds.ToArray(), cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, Category>> LoadCategoriesAsync(
        IReadOnlyCollection<Product> products,
        CancellationToken cancellationToken)
    {
        var categoryIds = products
            .SelectMany(product => product.Categories.Select(category => category.CategoryId))
            .ToHashSet();

        foreach (var categoryId in products
                     .Where(product => product.DefaultCategoryId is not null)
                     .Select(product => product.DefaultCategoryId!.Value))
        {
            categoryIds.Add(categoryId);
        }

        return await LoadCategoriesAsync(categoryIds.ToArray(), cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, Category>> LoadCategoriesAsync(
        IReadOnlyCollection<Guid> initialCategoryIds,
        CancellationToken cancellationToken)
    {
        if (initialCategoryIds.Count == 0)
        {
            return new Dictionary<Guid, Category>();
        }

        var known = new Dictionary<Guid, Category>();
        var pendingIds = initialCategoryIds.ToHashSet();

        while (pendingIds.Count > 0)
        {
            var batch = pendingIds.ToArray();
            pendingIds.Clear();

            var batchCategories = await dbContext.Categories
                .AsNoTracking()
                .Where(category => batch.Contains(category.Id))
                .ToListAsync(cancellationToken);

            foreach (var category in batchCategories)
            {
                known[category.Id] = category;
                if (category.ParentCategoryId is not null &&
                    !known.ContainsKey(category.ParentCategoryId.Value))
                {
                    pendingIds.Add(category.ParentCategoryId.Value);
                }
            }
        }

        return known;
    }

    private ProductSnapshot MapProduct(
        Product product,
        IReadOnlyDictionary<Guid, Brand> brands,
        IReadOnlyDictionary<Guid, Category> categories,
        IReadOnlyDictionary<Guid, Product> relatedProducts,
        InventoryAvailabilitySnapshot? productAvailability,
        IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot> variantAvailability)
    {
        var brand = product.BrandId is not null && brands.TryGetValue(product.BrandId.Value, out var brandEntity)
            ? new BrandSnapshot(
                brandEntity.Id,
                brandEntity.Name,
                brandEntity.Slug,
                brandEntity.Description,
                brandEntity.WebsiteUrl,
                brandEntity.LogoImageUrl,
                brandEntity.IsActive)
            : null;

        var primaryCategoryId = product.DefaultCategoryId ??
                                product.Categories.FirstOrDefault(category => category.IsPrimary)?.CategoryId;
        categories.TryGetValue(primaryCategoryId.GetValueOrDefault(), out var primaryCategory);

        var breadcrumbs = BuildCategoryBreadcrumbs(primaryCategory, categories);
        var optionLookup = product.Options.ToDictionary(option => option.Id);
        var valueLookup = product.Options
            .SelectMany(option => option.Values.Select(value => new { value.Id, Option = option, Value = value }))
            .ToDictionary(item => item.Id);

        var images = product.Images
            .OrderBy(image => image.Position)
            .Select(image => new ProductImageSnapshot(
                image.Id,
                product.Id,
                image.VariantId,
                image.SourceUrl,
                image.AltText,
                image.Position,
                image.IsPrimary))
            .ToArray();

        var variants = product.Variants
            .OrderBy(variant => variant.Position)
            .ThenBy(variant => variant.Sku, StringComparer.OrdinalIgnoreCase)
            .Select(variant =>
            {
                variantAvailability.TryGetValue(variant.Id, out var variantStock);
                var variantImage = ResolvePrimaryImage(product, variant.Id);
                var selectedOptions = variant.OptionAssignments
                    .OrderBy(assignment =>
                        optionLookup.TryGetValue(assignment.ProductOptionId, out var option)
                            ? option.Position
                            : int.MaxValue)
                    .Select(assignment =>
                    {
                        var option = optionLookup[assignment.ProductOptionId];
                        var value = valueLookup[assignment.ProductOptionValueId].Value;
                        return new ProductOptionSelectionSnapshot(
                            option.Id,
                            option.Name,
                            value.Id,
                            value.Value);
                    })
                    .ToArray();

                return new ProductVariantSnapshot(
                    variant.Id,
                    product.Id,
                    variant.Sku,
                    variant.Name,
                    variant.Slug,
                    variant.Barcode,
                    variant.Currency,
                    variant.PriceAmount,
                    variant.CompareAtPriceAmount,
                    variant.WeightKg,
                    variant.IsActive,
                    variant.Position,
                    variantStock?.IsTracked ?? false,
                    variantStock?.AllowBackorder ?? false,
                    variantStock?.AvailableQuantity,
                    variantStock?.IsInStock ?? true,
                    variantImage?.SourceUrl,
                    selectedOptions);
            })
            .ToArray();

        var defaultVariant = variants.FirstOrDefault(variant => variant.Id == product.DefaultVariantId) ?? variants[0];
        var attributes = product.Attributes
            .OrderBy(attribute => attribute.Position)
            .Select(attribute => new ProductAttributeSnapshot(
                attribute.Id,
                attribute.GroupName,
                attribute.Name,
                attribute.Value,
                attribute.Position,
                attribute.IsFilterable))
            .ToArray();

        var related = product.Relations
            .OrderBy(relation => relation.Position)
            .Where(relation => relatedProducts.TryGetValue(relation.RelatedProductId, out _))
            .Select(relation =>
            {
                var relatedProduct = relatedProducts[relation.RelatedProductId];
                var relatedImage = ResolvePrimaryImage(relatedProduct, relatedProduct.DefaultVariantId);
                var relatedVariant = relatedProduct.Variants
                    .OrderBy(variant => variant.Position)
                    .FirstOrDefault(variant => variant.Id == relatedProduct.DefaultVariantId) ??
                                     relatedProduct.Variants.OrderBy(variant => variant.Position).First();

                return new ProductRelationSnapshot(
                    relatedProduct.Id,
                    relatedProduct.Slug,
                    relatedProduct.Name,
                    relatedImage?.SourceUrl,
                    relatedVariant.Currency,
                    relatedVariant.PriceAmount,
                    relation.RelationType.ToString());
            })
            .ToArray();

        var primaryImage = ResolvePrimaryImage(product, defaultVariant.Id);

        return new ProductSnapshot(
            product.Id,
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.Description,
            product.Status.ToString(),
            product.ProductType.ToString(),
            product.IsFeatured,
            product.PublishedAtUtc,
            product.IsActive,
            product.SeoTitle,
            product.SeoDescription,
            product.CanonicalUrl,
            brand,
            primaryCategory?.Id,
            primaryCategory?.Slug,
            primaryCategory?.Name,
            breadcrumbs,
            defaultVariant.Id,
            defaultVariant.Currency,
            defaultVariant.Amount,
            defaultVariant.CompareAtAmount,
            productAvailability?.IsInStock ?? defaultVariant.IsInStock,
            productAvailability?.IsTracked ?? defaultVariant.IsTracked,
            productAvailability?.AllowBackorder ?? defaultVariant.AllowBackorder,
            productAvailability?.AvailableQuantity ?? defaultVariant.AvailableQuantity,
            primaryImage?.SourceUrl,
            defaultVariant.Sku,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            variants,
            attributes,
            images,
            related);
    }

    private static IReadOnlyCollection<CategoryBreadcrumbSnapshot> BuildCategoryBreadcrumbs(
        Category? category,
        IReadOnlyDictionary<Guid, Category> categories)
    {
        if (category is null)
        {
            return [];
        }

        var breadcrumbs = new List<CategoryBreadcrumbSnapshot>();
        var current = category;
        var safety = 0;
        while (current is not null && safety < 32)
        {
            breadcrumbs.Add(new CategoryBreadcrumbSnapshot(current.Id, current.Name, current.Slug));
            current = current.ParentCategoryId is not null &&
                      categories.TryGetValue(current.ParentCategoryId.Value, out var parent)
                ? parent
                : null;
            safety++;
        }

        breadcrumbs.Reverse();
        return breadcrumbs;
    }

    private static ProductImage? ResolvePrimaryImage(Product product, Guid variantId)
    {
        return product.Images
                   .Where(image => image.VariantId == variantId)
                   .OrderByDescending(image => image.IsPrimary)
                   .ThenBy(image => image.Position)
                   .FirstOrDefault()
               ?? product.Images
                   .Where(image => image.VariantId is null)
                   .OrderByDescending(image => image.IsPrimary)
                   .ThenBy(image => image.Position)
                   .FirstOrDefault();
    }
}
