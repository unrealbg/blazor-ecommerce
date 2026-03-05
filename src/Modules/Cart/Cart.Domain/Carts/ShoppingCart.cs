using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;

namespace Cart.Domain.Carts;

public sealed class ShoppingCart : AggregateRoot<Guid>
{
    private ShoppingCart()
    {
    }

    private ShoppingCart(Guid id, Guid customerId, DateTime createdOnUtc)
    {
        Id = id;
        CustomerId = customerId;
        CreatedOnUtc = createdOnUtc;
        Status = CartStatus.Open;
    }

    public Guid CustomerId { get; private set; }

    public CartStatus Status { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime? CheckedOutOnUtc { get; private set; }

    public Money? CheckoutTotal { get; private set; }

    public static Result<ShoppingCart> Create(Guid customerId, DateTime createdOnUtc)
    {
        if (customerId == Guid.Empty)
        {
            return Result<ShoppingCart>.Failure(
                new Error("cart.customer.required", "Customer id is required."));
        }

        return Result<ShoppingCart>.Success(new ShoppingCart(Guid.NewGuid(), customerId, createdOnUtc));
    }

    public Result Checkout(Money total, DateTime checkedOutOnUtc)
    {
        if (Status != CartStatus.Open)
        {
            return Result.Failure(new Error("cart.checkout.invalid_state", "Only open carts can be checked out."));
        }

        Status = CartStatus.CheckedOut;
        CheckedOutOnUtc = checkedOutOnUtc;
        CheckoutTotal = total;

        RaiseDomainEvent(new CartCheckedOutDomainEvent(Id, CustomerId, total.Currency, total.Amount));
        return Result.Success();
    }
}
