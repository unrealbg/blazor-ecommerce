using Redirects.Application.RedirectRules;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectHitRecorder(RedirectHitQueue redirectHitQueue) : IRedirectHitRecorder
{
    public void RecordHit(string normalizedFromPath, DateTime occurredOnUtc)
    {
        var record = new RedirectHitRecord(normalizedFromPath, occurredOnUtc);
        _ = redirectHitQueue.TryWrite(record);
    }
}
