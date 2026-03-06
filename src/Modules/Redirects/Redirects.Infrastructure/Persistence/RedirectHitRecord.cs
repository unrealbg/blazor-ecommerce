namespace Redirects.Infrastructure.Persistence;

internal sealed record RedirectHitRecord(string FromPath, DateTime OccurredOnUtc);
