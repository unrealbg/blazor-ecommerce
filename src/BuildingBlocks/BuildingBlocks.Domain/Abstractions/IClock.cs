namespace BuildingBlocks.Domain.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
