using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(
    IProductCatalogReader productCatalogReader,
    IProductListCache productListCache,
    IVariantPricingService variantPricingService)
    : IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductSnapshot>>
{
    public async Task<IReadOnlyCollection<ProductSnapshot>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await productListCache.GetAsync(cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var response = await productCatalogReader.ListAllAsync(cancellationToken);
        var pricing = await variantPricingService.GetVariantPricingAsync(
            response
                .SelectMany(product => product.Variants.Select(variant => variant.Id))
                .Distinct()
                .ToArray(),
            cancellationToken);

        var enriched = response
            .Select(product => EnrichProduct(product, pricing))
            .ToList();

        await productListCache.SetAsync(enriched, cancellationToken);
        return enriched;
    }

    private static ProductSnapshot EnrichProduct(
        ProductSnapshot product,
        IReadOnlyDictionary<Guid, VariantPricingSnapshot> pricing)
    {
        if (!pricing.TryGetValue(product.DefaultVariantId, out var variantPricing))
        {
            return product with
            {
                Variants = EnrichVariants(product.Variants, pricing),
            };
        }

        var compareAtAmount = variantPricing.CompareAtPriceAmount;
        if (compareAtAmount is null && variantPricing.BasePriceAmount > variantPricing.EffectivePriceAmount)
        {
            compareAtAmount = variantPricing.BasePriceAmount;
        }

        return product with
        {
            Currency = variantPricing.Currency,
            Amount = variantPricing.EffectivePriceAmount,
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
