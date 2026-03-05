using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxPublisher(
    IServiceProvider serviceProvider,
    IEventSerializer eventSerializer,
    ILogger<OutboxPublisher> logger)
    : IOutboxPublisher
{
    public async Task PublishAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        var domainEvent = eventSerializer.Deserialize(outboxMessage.Type, outboxMessage.Payload);

        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType).ToList();

        if (handlers.Count == 0)
        {
            logger.LogDebug("No handlers registered for domain event type {EventType}.", eventType.FullName);
            return;
        }

        var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<DomainEvent>.Handle))
                           ?? throw new InvalidOperationException(
                               $"Handler method '{nameof(IDomainEventHandler<DomainEvent>.Handle)}' was not found.");

        foreach (var handler in handlers.OfType<object>())
        {
            var task = (Task?)handleMethod.Invoke(handler, [domainEvent, cancellationToken]);

            if (task is null)
            {
                throw new InvalidOperationException(
                    $"Invoking '{handleMethod.Name}' on handler '{handler.GetType().Name}' returned null.");
            }

            await task;
        }
    }
}
