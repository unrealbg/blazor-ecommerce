using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Shared;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.OnCartCheckedOut;

public sealed class CartCheckedOutDomainEventHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IClock clock)
    : IDomainEventHandler<CartCheckedOutDomainEvent>
{
    public async Task Handle(CartCheckedOutDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var totalResult = Money.Create(domainEvent.Currency, domainEvent.TotalAmount);
        if (totalResult.IsFailure)
        {
            return;
        }

        var orderResult = Order.Create(domainEvent.CartId, domainEvent.CustomerId, totalResult.Value, clock.UtcNow);
        if (orderResult.IsFailure)
        {
            return;
        }

        await orderRepository.AddAsync(orderResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
