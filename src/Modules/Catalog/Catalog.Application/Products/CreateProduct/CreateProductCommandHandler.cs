using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    ICatalogUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var moneyResult = Money.Create(request.Currency, request.Amount);
        if (moneyResult.IsFailure)
        {
            return Result<Guid>.Failure(moneyResult.Error);
        }

        var productResult = Product.Create(request.Name, moneyResult.Value, clock.UtcNow);
        if (productResult.IsFailure)
        {
            return Result<Guid>.Failure(productResult.Error);
        }

        var product = productResult.Value;

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}
