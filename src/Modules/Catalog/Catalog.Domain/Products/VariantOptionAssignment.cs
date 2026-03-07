namespace Catalog.Domain.Products;

public sealed class VariantOptionAssignment
{
    private VariantOptionAssignment()
    {
    }

    internal VariantOptionAssignment(Guid variantId, Guid productOptionId, Guid productOptionValueId)
    {
        VariantId = variantId;
        ProductOptionId = productOptionId;
        ProductOptionValueId = productOptionValueId;
    }

    public Guid VariantId { get; private set; }

    public Guid ProductOptionId { get; private set; }

    public Guid ProductOptionValueId { get; private set; }
}
