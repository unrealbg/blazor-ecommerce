namespace Catalog.Domain.Products;

public sealed class ProductImage
{
    private ProductImage()
    {
    }

    internal ProductImage(
        Guid id,
        Guid? variantId,
        string sourceUrl,
        string? altText,
        int position,
        bool isPrimary,
        DateTime createdAtUtc)
    {
        Id = id;
        VariantId = variantId;
        SourceUrl = sourceUrl;
        AltText = altText;
        Position = position;
        IsPrimary = isPrimary;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid? VariantId { get; private set; }

    public string SourceUrl { get; private set; } = string.Empty;

    public string? AltText { get; private set; }

    public int Position { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
