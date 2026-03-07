using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(
    IProductCatalogReader productCatalogReader,
    IVariantPricingService variantPricingService)
    : IQueryHandler<GetProductBySlugQuery, ProductSnapshot?>
{
    public async Task<ProductSnapshot?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var products = await productCatalogReader.ListAllAsync(cancellationToken);
        var product = products.SingleOrDefault(item => item.Slug == request.Slug.Trim().ToLowerInvariant());
        if (product is null)
        {
            return null;
        }

        var pricing = await variantPricingService.GetVariantPricingAsync(
            product.Variants.Select(variant => variant.Id).ToArray(),
            cancellationToken);

        if (!pricing.TryGetValue(product.DefaultVariantId, out var defaultVariantPricing))
        {
            return product with
            {
                Variants = EnrichVariants(product.Variants, pricing),
            };
        }

        var compareAtAmount = defaultVariantPricing.CompareAtPriceAmount;
        if (compareAtAmount is null && defaultVariantPricing.BasePriceAmount > defaultVariantPricing.EffectivePriceAmount)
        {
            compareAtAmount = defaultVariantPricing.BasePriceAmount;
        }

        return product with
        {
            Currency = defaultVariantPricing.Currency,
            Amount = defaultVariantPricing.EffectivePriceAmount,
            CompareAtAmount = compareAtAmount,
            Variants = EnrichVariants(product.Variants, pricing),
        };
    }

    private static IReadOnlyCollection<ProductVariantSnapshot> EnrichVariants(
        IReadOnlyCollection<ProductVariantSnapshot> variants,
        IReadOnlyDictionary<Guid, VariantPricingSnapshot> pricing)
    {
        return variants
            .Select(variant =>
            {
                if (!pricing.TryGetValue(variant.Id, out var variantPricing))
                {
                    return variant;
                }

                var compareAtAmount = variantPricing.CompareAtPriceAmount;
                if (compareAtAmount is null && variantPricing.BasePriceAmount > variantPricing.EffectivePriceAmount)
                {
                    compareAtAmount = variantPricing.BasePriceAmount;
                }

                return variant with
                {
                    Currency = variantPricing.Currency,
                    Amount = variantPricing.EffectivePriceAmount,
                    CompareAtAmount = compareAtAmount,
                };
            })
            .ToArray();
    }
}
