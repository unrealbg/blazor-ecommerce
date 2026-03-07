namespace Catalog.Domain.Products;

public sealed class ProductVariant
{
    private readonly List<VariantOptionAssignment> _optionAssignments = [];

    private ProductVariant()
    {
    }

    internal ProductVariant(
        Guid id,
        string sku,
        string? name,
        string? slug,
        string? barcode,
        decimal priceAmount,
        string currency,
        decimal? compareAtPriceAmount,
        decimal? weightKg,
        bool isActive,
        int position,
        DateTime createdAtUtc)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Slug = slug;
        Barcode = barcode;
        PriceAmount = priceAmount;
        Currency = currency;
        CompareAtPriceAmount = compareAtPriceAmount;
        WeightKg = weightKg;
        IsActive = isActive;
        Position = position;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string? Name { get; private set; }

    public string? Slug { get; private set; }

    public string? Barcode { get; private set; }

    public decimal PriceAmount { get; private set; }

    public string Currency { get; private set; } = "EUR";

    public decimal? CompareAtPriceAmount { get; private set; }

    public decimal? WeightKg { get; private set; }

    public bool IsActive { get; private set; }

    public int Position { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<VariantOptionAssignment> OptionAssignments => _optionAssignments.AsReadOnly();

    internal void Update(
        string sku,
        string? name,
        string? slug,
        string? barcode,
        decimal priceAmount,
        string currency,
        decimal? compareAtPriceAmount,
        decimal? weightKg,
        bool isActive,
        int position,
        DateTime updatedAtUtc,
        IReadOnlyCollection<VariantOptionAssignment> optionAssignments)
    {
        Sku = sku;
        Name = name;
        Slug = slug;
        Barcode = barcode;
        PriceAmount = priceAmount;
        Currency = currency;
        CompareAtPriceAmount = compareAtPriceAmount;
        WeightKg = weightKg;
        IsActive = isActive;
        Position = position;
        UpdatedAtUtc = updatedAtUtc;

        _optionAssignments.Clear();
        _optionAssignments.AddRange(optionAssignments);
    }
}
