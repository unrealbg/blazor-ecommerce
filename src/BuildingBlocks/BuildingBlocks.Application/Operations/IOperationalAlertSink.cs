namespace BuildingBlocks.Application.Operations;

public interface IOperationalAlertSink
{
    Task PublishAsync(OperationalAlert alert, CancellationToken cancellationToken);
}