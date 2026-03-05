using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Cart.Domain.Carts;
using CartAggregate = Cart.Domain.Carts.Cart;

namespace Cart.Application.Carts.AddItem;

public sealed class AddItemToCartCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    IProductCatalogReader productCatalogReader)
    : ICommandHandler<AddItemToCartCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await productCatalogReader.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return Result<Guid>.Failure(new Error("cart.product.not_found", "Product was not found."));
        }

        var moneyResult = Money.Create(product.Currency, product.Amount);
        if (moneyResult.IsFailure)
        {
            return Result<Guid>.Failure(moneyResult.Error);
        }

        var cart = await cartRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        var isNewCart = cart is null;

        if (isNewCart)
        {
            var createCartResult = CartAggregate.Create(request.CustomerId);
            if (createCartResult.IsFailure)
            {
                return Result<Guid>.Failure(createCartResult.Error);
            }

            cart = createCartResult.Value;
            await cartRepository.AddAsync(cart, cancellationToken);
        }

        var addItemResult = cart!.AddItem(
            request.ProductId,
            product.Name,
            moneyResult.Value,
            request.Quantity);

        if (addItemResult.IsFailure)
        {
            return Result<Guid>.Failure(addItemResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(cart.Id);
    }
}
