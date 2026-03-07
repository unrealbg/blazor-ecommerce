namespace BuildingBlocks.Application.Auditing;

public interface IAuditTrail
{
    Task WriteAsync(AuditEntryInput input, CancellationToken cancellationToken);
}
