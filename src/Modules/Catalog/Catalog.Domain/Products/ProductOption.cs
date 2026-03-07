namespace Catalog.Domain.Products;

public sealed class ProductOption
{
    private readonly List<ProductOptionValue> _values = [];

    private ProductOption()
    {
    }

    internal ProductOption(Guid id, string name, int position, IReadOnlyCollection<ProductOptionValue> values)
    {
        Id = id;
        Name = name;
        Position = position;
        _values.AddRange(values);
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public IReadOnlyCollection<ProductOptionValue> Values => _values.AsReadOnly();
}
