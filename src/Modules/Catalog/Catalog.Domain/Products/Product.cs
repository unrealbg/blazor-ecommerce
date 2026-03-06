using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Catalog.Domain.Products.Events;

namespace Catalog.Domain.Products;

public sealed class Product : AggregateRoot<Guid>
{
    private Product()
    {
    }

    private Product(
        Guid id,
        string name,
        string slug,
        string? description,
        string? brand,
        string? sku,
        string? imageUrl,
        bool isInStock,
        string? categorySlug,
        string? categoryName,
        Money price,
        bool isActive)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Description = description;
        Brand = brand;
        Sku = sku;
        ImageUrl = imageUrl;
        IsInStock = isInStock;
        CategorySlug = categorySlug;
        CategoryName = categoryName;
        Price = price;
        IsActive = isActive;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? Brand { get; private set; }

    public string? Sku { get; private set; }

    public string? ImageUrl { get; private set; }

    public bool IsInStock { get; private set; }

    public string? CategorySlug { get; private set; }

    public string? CategoryName { get; private set; }

    public Money Price { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public static Result<Product> Create(
        string name,
        string slug,
        string? description,
        string? brand,
        string? sku,
        string? imageUrl,
        bool isInStock,
        string? categorySlug,
        string? categoryName,
        Money price,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Product>.Failure(
                new Error("catalog.product.name.required", "Product name is required."));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<Product>.Failure(
                new Error("catalog.product.slug.required", "Product slug is required."));
        }

        var trimmedName = name.Trim();
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        var normalizedBrand = string.IsNullOrWhiteSpace(brand)
            ? null
            : brand.Trim();
        var normalizedSku = string.IsNullOrWhiteSpace(sku)
            ? null
            : sku.Trim();
        var normalizedImageUrl = string.IsNullOrWhiteSpace(imageUrl)
            ? null
            : imageUrl.Trim();
        var normalizedCategorySlug = string.IsNullOrWhiteSpace(categorySlug)
            ? null
            : categorySlug.Trim().ToLowerInvariant();
        var normalizedCategoryName = string.IsNullOrWhiteSpace(categoryName)
            ? null
            : categoryName.Trim();

        if (normalizedSlug.Length > 220)
        {
            return Result<Product>.Failure(
                new Error("catalog.product.slug.too_long", "Product slug is too long."));
        }

        if (normalizedDescription is { Length: > 2000 })
        {
            return Result<Product>.Failure(
                new Error("catalog.product.description.too_long", "Product description is too long."));
        }

        if (normalizedBrand is { Length: > 120 })
        {
            return Result<Product>.Failure(
                new Error("catalog.product.brand.too_long", "Product brand is too long."));
        }

        if (normalizedSku is { Length: > 64 })
        {
            return Result<Product>.Failure(
                new Error("catalog.product.sku.too_long", "Product SKU is too long."));
        }

        if (normalizedImageUrl is { Length: > 2000 })
        {
            return Result<Product>.Failure(
                new Error("catalog.product.image_url.too_long", "Product image URL is too long."));
        }

        if (!IsValidImageUrlInternal(normalizedImageUrl))
        {
            return Result<Product>.Failure(
                new Error("catalog.product.image_url.invalid", "Product image URL is invalid."));
        }

        if (normalizedCategorySlug is { Length: > 120 })
        {
            return Result<Product>.Failure(
                new Error("catalog.product.category_slug.too_long", "Product category slug is too long."));
        }

        if (normalizedCategoryName is { Length: > 120 })
        {
            return Result<Product>.Failure(
                new Error("catalog.product.category_name.too_long", "Product category name is too long."));
        }

        if ((normalizedCategorySlug is null) != (normalizedCategoryName is null))
        {
            return Result<Product>.Failure(
                new Error(
                    "catalog.product.category.invalid",
                    "Category slug and category name should be provided together."));
        }

        var product = new Product(
            Guid.NewGuid(),
            trimmedName,
            normalizedSlug,
            normalizedDescription,
            normalizedBrand,
            normalizedSku,
            normalizedImageUrl,
            isInStock,
            normalizedCategorySlug,
            normalizedCategoryName,
            price,
            isActive);

        return Result<Product>.Success(product);

        static bool IsValidImageUrlInternal(string? imageUrl)
        {
            if (imageUrl is null)
            {
                return true;
            }

            if (imageUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return true;
            }

            return Uri.TryCreate(imageUrl, UriKind.Absolute, out _);
        }
    }

    public Result<bool> UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<bool>.Failure(
                new Error("catalog.product.slug.required", "Product slug is required."));
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (normalizedSlug.Length > 220)
        {
            return Result<bool>.Failure(
                new Error("catalog.product.slug.too_long", "Product slug is too long."));
        }

        if (string.Equals(Slug, normalizedSlug, StringComparison.Ordinal))
        {
            return Result<bool>.Success(false);
        }

        var previousSlug = Slug;
        Slug = normalizedSlug;
        RaiseDomainEvent(new ProductSlugChanged(Id, previousSlug, Slug));

        return Result<bool>.Success(true);
    }
}
