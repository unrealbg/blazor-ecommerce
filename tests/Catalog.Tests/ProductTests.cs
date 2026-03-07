using Catalog.Domain.Products;

namespace Catalog.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_Should_ReturnSuccess_When_InputIsValid()
    {
        var categoryId = Guid.NewGuid();
        var sizeOptionId = Guid.NewGuid();
        var mediumValueId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var result = Product.Create(
            "Product A",
            "product-a",
            "Short description",
            "Long description",
            Guid.NewGuid(),
            categoryId,
            ProductStatus.Draft,
            ProductType.Simple,
            "SEO title",
            "SEO description",
            null,
            isFeatured: true,
            publishedAtUtc: null,
            createdAt,
            [new ProductCategoryDraft(categoryId, true, 0)],
            [new ProductVariantDraft(
                null,
                "SKU-1",
                "Default",
                null,
                null,
                12.34m,
                "EUR",
                null,
                null,
                IsActive: true,
                Position: 0,
                [new VariantOptionAssignmentDraft(sizeOptionId, mediumValueId)])],
            [new ProductOptionDraft(
                sizeOptionId,
                "Size",
                0,
                [new ProductOptionValueDraft(mediumValueId, "M", 0)])],
            [new ProductAttributeDraft(null, "General", "Material", "Cotton", 0, false)],
            [new ProductImageDraft(null, null, "/images/product-a.jpg", "Product A", 0, true)],
            []);

        Assert.True(result.IsSuccess);
        Assert.Equal("Product A", result.Value.Name);
        Assert.Equal(ProductStatus.Draft, result.Value.Status);
        Assert.Equal(categoryId, result.Value.DefaultCategoryId);
        Assert.Single(result.Value.Variants);
        Assert.Single(result.Value.Options);
        Assert.Single(result.Value.Attributes);
        Assert.Single(result.Value.Images);
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_NameMissing()
    {
        var result = Product.Create(
            string.Empty,
            "product-a",
            null,
            null,
            null,
            null,
            ProductStatus.Draft,
            ProductType.Simple,
            null,
            null,
            null,
            isFeatured: false,
            publishedAtUtc: null,
            DateTime.UtcNow,
            [],
            CreateDefaultVariants(),
            [],
            [],
            [],
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.name.required", result.Error.Code);
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_DuplicateSkuIsUsed()
    {
        var variants = new[]
        {
            CreateVariantDraft("SKU-1"),
            CreateVariantDraft("SKU-1", position: 1),
        };

        var result = Product.Create(
            "Product A",
            "product-a",
            null,
            null,
            null,
            null,
            ProductStatus.Draft,
            ProductType.VariantParent,
            null,
            null,
            null,
            isFeatured: false,
            publishedAtUtc: null,
            DateTime.UtcNow,
            [],
            variants,
            [],
            [],
            [],
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.duplicate_sku", result.Error.Code);
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_MultiplePrimaryCategoriesExist()
    {
        var firstCategoryId = Guid.NewGuid();
        var secondCategoryId = Guid.NewGuid();

        var result = Product.Create(
            "Product A",
            "product-a",
            null,
            null,
            null,
            firstCategoryId,
            ProductStatus.Draft,
            ProductType.Simple,
            null,
            null,
            null,
            isFeatured: false,
            publishedAtUtc: null,
            DateTime.UtcNow,
            [
                new ProductCategoryDraft(firstCategoryId, true, 0),
                new ProductCategoryDraft(secondCategoryId, true, 1),
            ],
            CreateDefaultVariants(),
            [],
            [],
            [],
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.primary_category.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_VariantOptionCombinationIsDuplicated()
    {
        var colorOptionId = Guid.NewGuid();
        var redValueId = Guid.NewGuid();

        var result = Product.Create(
            "Product A",
            "product-a",
            null,
            null,
            null,
            null,
            ProductStatus.Draft,
            ProductType.VariantParent,
            null,
            null,
            null,
            isFeatured: false,
            publishedAtUtc: null,
            DateTime.UtcNow,
            [],
            [
                new ProductVariantDraft(null, "SKU-1", "Red / S", null, null, 10m, "EUR", null, null, true, 0, [new VariantOptionAssignmentDraft(colorOptionId, redValueId)]),
                new ProductVariantDraft(null, "SKU-2", "Red / M", null, null, 11m, "EUR", null, null, true, 1, [new VariantOptionAssignmentDraft(colorOptionId, redValueId)]),
            ],
            [new ProductOptionDraft(colorOptionId, "Color", 0, [new ProductOptionValueDraft(redValueId, "Red", 0)])],
            [],
            [],
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("catalog.product.variant.option_combination.duplicate", result.Error.Code);
    }

    [Fact]
    public void Activate_Should_ReturnFailure_When_NoActiveVariantsExist()
    {
        var productResult = Product.Create(
            "Product A",
            "product-a",
            null,
            null,
            null,
            null,
            ProductStatus.Draft,
            ProductType.Simple,
            null,
            null,
            null,
            isFeatured: false,
            publishedAtUtc: null,
            DateTime.UtcNow,
            [],
            [new ProductVariantDraft(null, "SKU-1", null, null, null, 10m, "EUR", null, null, false, 0, [])],
            [],
            [],
            [],
            []);

        Assert.True(productResult.IsSuccess);

        var activateResult = productResult.Value.Activate(DateTime.UtcNow);

        Assert.True(activateResult.IsFailure);
        Assert.Equal("catalog.variant.not_sellable", activateResult.Error.Code);
    }

    [Fact]
    public void Update_Should_ReturnFailure_When_RelatedProductReferencesSelf()
    {
        var productResult = Product.Create(
            "Product A",
            "product-a",
            null,
            null,
            null,
            null,
            ProductStatus.Draft,
            ProductType.Simple,
            null,
            null,
            null,
            isFeatured: false,
            publishedAtUtc: null,
            DateTime.UtcNow,
            [],
            [new ProductVariantDraft(Guid.NewGuid(), "SKU-1", null, null, null, 10m, "EUR", null, null, true, 0, [])],
            [],
            [],
            [],
            []);

        Assert.True(productResult.IsSuccess);

        var updateResult = productResult.Value.Update(
            "Product A",
            "product-a",
            null,
            null,
            null,
            null,
            ProductType.Simple,
            null,
            null,
            null,
            isFeatured: false,
            publishedAtUtc: null,
            [],
            [new ProductVariantDraft(productResult.Value.DefaultVariantId, "SKU-1", null, null, null, 10m, "EUR", null, null, true, 0, [])],
            [],
            [],
            [],
            [new ProductRelationDraft(Guid.NewGuid(), productResult.Value.Id, ProductRelationType.Related, 0)],
            DateTime.UtcNow);

        Assert.True(updateResult.IsFailure);
        Assert.Equal("catalog.product.relation.self_reference", updateResult.Error.Code);
    }

    private static ProductVariantDraft[] CreateDefaultVariants()
    {
        return [CreateVariantDraft("SKU-1")];
    }

    private static ProductVariantDraft CreateVariantDraft(string sku, int position = 0)
    {
        return new ProductVariantDraft(
            null,
            sku,
            "Default",
            null,
            null,
            10m,
            "EUR",
            null,
            null,
            IsActive: true,
            position,
            []);
    }
}
