using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Orders.Domain.Events;

namespace Orders.Application.Orders.OnOrderPlaced;

public sealed class OrderPlacedDomainEventHandler(
    ILogger<OrderPlacedDomainEventHandler> logger,
    IOrderAuditRepository orderAuditRepository,
    IOrdersUnitOfWork unitOfWork,
    IClock clock)
    : IDomainEventHandler<OrderPlaced>
{
    public async Task Handle(OrderPlaced domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "OrderPlaced handled. OrderId: {OrderId}, Total: {Currency} {TotalAmount}",
            domainEvent.OrderId,
            domainEvent.Currency,
            domainEvent.TotalAmount);

        await orderAuditRepository.AddAsync(
            domainEvent.EventId,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.Currency,
            domainEvent.TotalAmount,
            clock.UtcNow,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
