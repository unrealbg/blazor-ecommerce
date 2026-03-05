using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.Checkout;

public sealed class CheckoutCommandHandler(
    ICartCheckoutAccessor cartCheckoutAccessor,
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CheckoutCommand, Guid>
{
    public Task<Result<Guid>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(
            async innerCancellationToken =>
            {
                var cart = await cartCheckoutAccessor.GetByCustomerIdAsync(
                    request.CustomerId,
                    innerCancellationToken);

                if (cart is null || cart.Lines.Count == 0)
                {
                    return Result<Guid>.Failure(
                        new Error("orders.checkout.cart_empty", "Cannot checkout an empty cart."));
                }

                var lineData = new List<OrderLineData>(cart.Lines.Count);
                foreach (var line in cart.Lines)
                {
                    var moneyResult = Money.Create(line.Currency, line.UnitAmount);
                    if (moneyResult.IsFailure)
                    {
                        return Result<Guid>.Failure(moneyResult.Error);
                    }

                    lineData.Add(new OrderLineData(line.ProductId, line.Name, moneyResult.Value, line.Quantity));
                }

                var orderResult = Order.Create(cart.CustomerId, lineData, clock.UtcNow);
                if (orderResult.IsFailure)
                {
                    return Result<Guid>.Failure(orderResult.Error);
                }

                await orderRepository.AddAsync(orderResult.Value, innerCancellationToken);
                await cartCheckoutAccessor.ClearCartAsync(cart.CartId, innerCancellationToken);
                await unitOfWork.SaveChangesAsync(innerCancellationToken);

                return Result<Guid>.Success(orderResult.Value.Id);
            },
            cancellationToken);
    }
}
