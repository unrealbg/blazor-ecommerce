namespace BuildingBlocks.Infrastructure.Retention;

public interface IRetentionTask
{
    string Name { get; }

    Task<int> ExecuteAsync(CancellationToken cancellationToken);
}