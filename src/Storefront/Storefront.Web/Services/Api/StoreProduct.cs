using System.Text.Json.Serialization;

namespace Storefront.Web.Services.Api;

public sealed class StoreProduct
{
    public StoreProduct()
    {
    }

    public StoreProduct(
        Guid id,
        string slug,
        string name,
        string? description,
        string? brand,
        string? sku,
        string? imageUrl,
        bool isInStock,
        bool isTracked,
        bool allowBackorder,
        int? availableQuantity,
        string? categorySlug,
        string? categoryName,
        string currency,
        decimal amount,
        bool isActive)
    {
        Id = id;
        Slug = slug;
        Name = name;
        Description = description;
        BrandDetails = string.IsNullOrWhiteSpace(brand)
            ? null
            : new StoreBrand
            {
                Name = brand,
                Slug = brand.Trim().ToLowerInvariant().Replace(' ', '-'),
                IsActive = true,
            };
        Sku = sku;
        ImageUrl = imageUrl;
        IsInStock = isInStock;
        IsTracked = isTracked;
        AllowBackorder = allowBackorder;
        AvailableQuantity = availableQuantity;
        CategorySlug = categorySlug;
        CategoryName = categoryName;
        Currency = currency;
        Amount = amount;
        IsActive = isActive;
    }

    public Guid Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? ShortDescription { get; init; }

    public string? Description { get; init; }

    public string Status { get; init; } = string.Empty;

    public string ProductType { get; init; } = string.Empty;

    public bool IsFeatured { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public bool IsActive { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public string? CanonicalUrl { get; init; }

    [JsonPropertyName("brand")]
    public StoreBrand? BrandDetails { get; init; }

    public Guid? DefaultCategoryId { get; init; }

    public string? CategorySlug { get; init; }

    public string? CategoryName { get; init; }

    public IReadOnlyCollection<StoreCategoryBreadcrumb> CategoryBreadcrumbs { get; init; } = [];

    public Guid DefaultVariantId { get; init; }

    public string Currency { get; init; } = "EUR";

    public decimal Amount { get; init; }

    public decimal? CompareAtAmount { get; init; }

    public bool IsInStock { get; init; }

    public bool IsTracked { get; init; }

    public bool AllowBackorder { get; init; }

    public int? AvailableQuantity { get; init; }

    public string? ImageUrl { get; init; }

    public string? Sku { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public IReadOnlyCollection<StoreProductVariant> Variants { get; init; } = [];

    public IReadOnlyCollection<StoreProductAttribute> Attributes { get; init; } = [];

    public IReadOnlyCollection<StoreProductImage> Images { get; init; } = [];

    public IReadOnlyCollection<StoreProductRelation> RelatedProducts { get; init; } = [];

    [JsonIgnore]
    public string? Brand => BrandDetails?.Name;
}
