using BuildingBlocks.Application.Auditing;

namespace BuildingBlocks.Application.Extensions;

public sealed class NullAuditTrail : IAuditTrail
{
    public Task WriteAsync(AuditEntryInput input, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
