using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Catalog.Application.Products.UpdateProductSlug;

public sealed class UpdateProductSlugCommandHandler(
    IProductRepository productRepository,
    IProductListCache productListCache,
    ICatalogUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductSlugCommand, string>
{
    public async Task<Result<string>> Handle(UpdateProductSlugCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<string>.Failure(new Error("catalog.product.not_found", "Product was not found."));
        }

        var targetSlug = await GenerateUniqueSlugAsync(product.Slug, request.Slug, cancellationToken);

        var updateResult = product.UpdateSlug(targetSlug);
        if (updateResult.IsFailure)
        {
            return Result<string>.Failure(updateResult.Error);
        }

        if (!updateResult.Value)
        {
            return Result<string>.Success(product.Slug);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productListCache.InvalidateAsync(cancellationToken);

        return Result<string>.Success(product.Slug);
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string currentSlug,
        string requestedSlug,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.Generate(requestedSlug);
        var candidate = baseSlug;

        if (string.Equals(candidate, currentSlug, StringComparison.Ordinal))
        {
            return candidate;
        }

        var suffix = 2;
        while (await productRepository.SlugExistsAsync(candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
