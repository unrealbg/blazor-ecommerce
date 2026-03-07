namespace Catalog.Domain.Products;

public sealed class ProductCategory
{
    private ProductCategory()
    {
    }

    internal ProductCategory(Guid categoryId, bool isPrimary, int sortOrder)
    {
        CategoryId = categoryId;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    public Guid ProductId { get; private set; }

    public Guid CategoryId { get; private set; }

    public bool IsPrimary { get; private set; }

    public int SortOrder { get; private set; }
}
