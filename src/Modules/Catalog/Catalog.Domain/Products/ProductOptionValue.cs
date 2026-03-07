namespace Catalog.Domain.Products;

public sealed class ProductOptionValue
{
    private ProductOptionValue()
    {
    }

    internal ProductOptionValue(Guid id, string value, int position)
    {
        Id = id;
        Value = value;
        Position = position;
    }

    public Guid Id { get; private set; }

    public Guid ProductOptionId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public int Position { get; private set; }
}
