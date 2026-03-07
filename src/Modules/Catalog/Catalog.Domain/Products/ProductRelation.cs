namespace Catalog.Domain.Products;

public sealed class ProductRelation
{
    private ProductRelation()
    {
    }

    internal ProductRelation(
        Guid id,
        Guid relatedProductId,
        ProductRelationType relationType,
        int position,
        DateTime createdAtUtc)
    {
        Id = id;
        RelatedProductId = relatedProductId;
        RelationType = relationType;
        Position = position;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid RelatedProductId { get; private set; }

    public ProductRelationType RelationType { get; private set; }

    public int Position { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
