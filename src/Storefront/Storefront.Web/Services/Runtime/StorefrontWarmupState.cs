namespace Storefront.Web.Services.Runtime;

public sealed class StorefrontWarmupState
{
    private readonly object gate = new();
    private WarmupSnapshot snapshot = new(false, false, null, null, null, "pending", 0, 0);

    public WarmupSnapshot GetSnapshot()
    {
        lock (gate)
        {
            return snapshot;
        }
    }

    public void MarkStarted()
    {
        lock (gate)
        {
            snapshot = snapshot with
            {
                StartedAtUtc = DateTime.UtcNow,
                Status = "running",
            };
        }
    }

    public void MarkCompleted(int warmedProducts, int warmedCategories)
    {
        lock (gate)
        {
            snapshot = snapshot with
            {
                HasRun = true,
                Completed = true,
                CompletedAtUtc = DateTime.UtcNow,
                Status = "completed",
                WarmedProducts = warmedProducts,
                WarmedCategories = warmedCategories,
                LastError = null,
            };
        }
    }

    public void MarkFailed(Exception exception)
    {
        lock (gate)
        {
            snapshot = snapshot with
            {
                HasRun = true,
                Completed = false,
                CompletedAtUtc = DateTime.UtcNow,
                Status = "failed",
                LastError = exception.Message,
            };
        }
    }

    public sealed record WarmupSnapshot(
        bool HasRun,
        bool Completed,
        DateTime? StartedAtUtc,
        DateTime? CompletedAtUtc,
        string? LastError,
        string Status,
        int WarmedProducts,
        int WarmedCategories);
}