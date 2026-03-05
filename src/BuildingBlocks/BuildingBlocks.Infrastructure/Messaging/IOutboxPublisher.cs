namespace BuildingBlocks.Infrastructure.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
}
