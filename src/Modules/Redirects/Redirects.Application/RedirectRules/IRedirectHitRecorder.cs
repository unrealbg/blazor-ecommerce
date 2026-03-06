namespace Redirects.Application.RedirectRules;

public interface IRedirectHitRecorder
{
    void RecordHit(string normalizedFromPath, DateTime occurredOnUtc);
}
