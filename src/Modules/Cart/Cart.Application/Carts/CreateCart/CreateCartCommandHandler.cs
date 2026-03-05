using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Cart.Domain.Carts;

namespace Cart.Application.Carts.CreateCart;

public sealed class CreateCartCommandHandler(
    ICartRepository cartRepository,
    ICartUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CreateCartCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        var cartResult = ShoppingCart.Create(request.CustomerId, clock.UtcNow);
        if (cartResult.IsFailure)
        {
            return Result<Guid>.Failure(cartResult.Error);
        }

        var cart = cartResult.Value;

        await cartRepository.AddAsync(cart, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(cart.Id);
    }
}
