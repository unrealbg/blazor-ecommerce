using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;

namespace Orders.Domain.Orders;

public sealed class Order : AggregateRoot<Guid>
{
    private Order()
    {
    }

    private Order(
        Guid id,
        Guid cartId,
        Guid customerId,
        Money total,
        DateTime createdOnUtc)
    {
        Id = id;
        CartId = cartId;
        CustomerId = customerId;
        Total = total;
        CreatedOnUtc = createdOnUtc;
        Status = OrderStatus.Pending;
    }

    public Guid CartId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Money Total { get; private set; } = null!;

    public DateTime CreatedOnUtc { get; private set; }

    public OrderStatus Status { get; private set; }

    public static Result<Order> Create(Guid cartId, Guid customerId, Money total, DateTime createdOnUtc)
    {
        if (cartId == Guid.Empty)
        {
            return Result<Order>.Failure(new Error("orders.cart.required", "Cart id is required."));
        }

        if (customerId == Guid.Empty)
        {
            return Result<Order>.Failure(new Error("orders.customer.required", "Customer id is required."));
        }

        return Result<Order>.Success(new Order(Guid.NewGuid(), cartId, customerId, total, createdOnUtc));
    }
}
