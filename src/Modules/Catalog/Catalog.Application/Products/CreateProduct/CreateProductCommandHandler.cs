using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IProductListCache productListCache,
    ICatalogUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var moneyResult = Money.Create(request.Currency, request.Amount);
        if (moneyResult.IsFailure)
        {
            return Result<Guid>.Failure(moneyResult.Error);
        }

        var slug = await GenerateUniqueSlugAsync(request.Name, cancellationToken);
        var categorySlug = NormalizeCategorySlug(request.CategorySlug, request.CategoryName);
        var productResult = Product.Create(
            request.Name,
            slug,
            request.Description,
            request.Brand,
            request.Sku,
            request.ImageUrl,
            request.IsInStock,
            categorySlug,
            request.CategoryName,
            moneyResult.Value,
            request.IsActive);
        if (productResult.IsFailure)
        {
            return Result<Guid>.Failure(productResult.Error);
        }

        var product = productResult.Value;

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productListCache.InvalidateAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }

    private async Task<string> GenerateUniqueSlugAsync(string productName, CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.Generate(productName);
        var candidate = baseSlug;
        var suffix = 2;

        while (await productRepository.SlugExistsAsync(candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private string? NormalizeCategorySlug(string? categorySlug, string? categoryName)
    {
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            return SlugGenerator.Generate(categorySlug);
        }

        return string.IsNullOrWhiteSpace(categoryName) ? null : SlugGenerator.Generate(categoryName);
    }
}
