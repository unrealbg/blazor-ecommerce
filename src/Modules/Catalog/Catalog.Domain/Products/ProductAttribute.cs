namespace Catalog.Domain.Products;

public sealed class ProductAttribute
{
    private ProductAttribute()
    {
    }

    internal ProductAttribute(
        Guid id,
        string? groupName,
        string name,
        string value,
        int position,
        bool isFilterable)
    {
        Id = id;
        GroupName = groupName;
        Name = name;
        Value = value;
        Position = position;
        IsFilterable = isFilterable;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string? GroupName { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public bool IsFilterable { get; private set; }
}
