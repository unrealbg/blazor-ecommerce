namespace Search.Domain.Documents;

public sealed class ProductSearchDocument
{
    private ProductSearchDocument()
    {
    }

    public Guid ProductId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? DescriptionText { get; private set; }

    public string? CategorySlug { get; private set; }

    public string? CategoryName { get; private set; }

    public string? Brand { get; private set; }

    public decimal PriceAmount { get; private set; }

    public string Currency { get; private set; } = "EUR";

    public bool IsActive { get; private set; }

    public bool IsInStock { get; private set; }

    public string? ImageUrl { get; private set; }

    public decimal? PopularityScore { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static ProductSearchDocument Create(
        Guid productId,
        string slug,
        string name,
        string? descriptionText,
        string? categorySlug,
        string? categoryName,
        string? brand,
        decimal priceAmount,
        string currency,
        bool isActive,
        bool isInStock,
        string? imageUrl,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        decimal? popularityScore = null)
    {
        var document = new ProductSearchDocument();
        document.Apply(
            productId,
            slug,
            name,
            descriptionText,
            categorySlug,
            categoryName,
            brand,
            priceAmount,
            currency,
            isActive,
            isInStock,
            imageUrl,
            createdAtUtc,
            updatedAtUtc,
            popularityScore);

        return document;
    }

    public void Update(
        string slug,
        string name,
        string? descriptionText,
        string? categorySlug,
        string? categoryName,
        string? brand,
        decimal priceAmount,
        string currency,
        bool isActive,
        bool isInStock,
        string? imageUrl,
        DateTime updatedAtUtc,
        decimal? popularityScore = null)
    {
        var createdAt = this.CreatedAtUtc == default ? updatedAtUtc : this.CreatedAtUtc;

        this.Apply(
            this.ProductId,
            slug,
            name,
            descriptionText,
            categorySlug,
            categoryName,
            brand,
            priceAmount,
            currency,
            isActive,
            isInStock,
            imageUrl,
            createdAt,
            updatedAtUtc,
            popularityScore);
    }

    private void Apply(
        Guid productId,
        string slug,
        string name,
        string? descriptionText,
        string? categorySlug,
        string? categoryName,
        string? brand,
        decimal priceAmount,
        string currency,
        bool isActive,
        bool isInStock,
        string? imageUrl,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        decimal? popularityScore)
    {
        this.ProductId = productId;
        this.Slug = slug.Trim().ToLowerInvariant();
        this.Name = name.Trim();
        this.NormalizedName = this.Name.ToLowerInvariant();
        this.DescriptionText = string.IsNullOrWhiteSpace(descriptionText)
            ? null
            : descriptionText.Trim();
        this.CategorySlug = string.IsNullOrWhiteSpace(categorySlug)
            ? null
            : categorySlug.Trim().ToLowerInvariant();
        this.CategoryName = string.IsNullOrWhiteSpace(categoryName)
            ? null
            : categoryName.Trim();
        this.Brand = string.IsNullOrWhiteSpace(brand)
            ? null
            : brand.Trim();
        this.PriceAmount = decimal.Round(priceAmount, 2, MidpointRounding.AwayFromZero);
        this.Currency = string.IsNullOrWhiteSpace(currency)
            ? "EUR"
            : currency.Trim().ToUpperInvariant();
        this.IsActive = isActive;
        this.IsInStock = isInStock;
        this.ImageUrl = string.IsNullOrWhiteSpace(imageUrl)
            ? null
            : imageUrl.Trim();
        this.CreatedAtUtc = createdAtUtc == default ? DateTime.UtcNow : DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        this.UpdatedAtUtc = updatedAtUtc == default ? DateTime.UtcNow : DateTime.SpecifyKind(updatedAtUtc, DateTimeKind.Utc);
        this.PopularityScore = popularityScore;
    }
}
