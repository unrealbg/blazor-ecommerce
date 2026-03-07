using BuildingBlocks.Application.Operations;
using System.Collections.Concurrent;

namespace BuildingBlocks.Infrastructure.Operations;

internal sealed class OperationalStateRegistry : IOperationalStateRegistry
{
    private const int MaxAlerts = 50;
    private readonly ConcurrentDictionary<string, WorkerExecutionState> workers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object alertsLock = new();
    private readonly Queue<OperationalAlert> alerts = new();
    private OperationalSnapshot snapshot = new(
        0,
        0,
        0,
        null,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        DateTime.MinValue);

    public void RecordWorkerStarted(string workerName, string? correlationId = null)
    {
        workers.AddOrUpdate(
            workerName,
            _ => new WorkerExecutionState(workerName, DateTime.UtcNow, null, null, null, 0, null, "running", correlationId, null, null),
            (_, existing) => existing with
            {
                LastStartedAtUtc = DateTime.UtcNow,
                State = "running",
                LastCorrelationId = correlationId,
            });
    }

    public void RecordWorkerSucceeded(string workerName, double durationMs, int? processedCount = null, string? note = null)
    {
        workers.AddOrUpdate(
            workerName,
            _ => new WorkerExecutionState(workerName, DateTime.UtcNow, DateTime.UtcNow, null, null, 0, durationMs, "healthy", null, processedCount, note),
            (_, existing) => existing with
            {
                LastSucceededAtUtc = DateTime.UtcNow,
                LastError = null,
                ConsecutiveFailureCount = 0,
                LastDurationMs = durationMs,
                State = "healthy",
                LastProcessedCount = processedCount,
                LastNote = note,
            });
    }

    public void RecordWorkerFailed(string workerName, Exception exception, double durationMs)
    {
        workers.AddOrUpdate(
            workerName,
            _ => new WorkerExecutionState(workerName, DateTime.UtcNow, null, DateTime.UtcNow, exception.Message, 1, durationMs, "failing", null, null, null),
            (_, existing) => existing with
            {
                LastFailedAtUtc = DateTime.UtcNow,
                LastError = exception.Message,
                ConsecutiveFailureCount = existing.ConsecutiveFailureCount + 1,
                LastDurationMs = durationMs,
                State = "failing",
            });
    }

    public void UpdateSnapshot(OperationalSnapshot snapshot)
    {
        this.snapshot = snapshot;
    }

    public OperationalSnapshot GetSnapshot()
    {
        return snapshot;
    }

    public IReadOnlyCollection<WorkerExecutionState> GetWorkers()
    {
        return workers.Values.OrderBy(worker => worker.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public void RecordAlert(OperationalAlert alert)
    {
        lock (alertsLock)
        {
            alerts.Enqueue(alert);
            while (alerts.Count > MaxAlerts)
            {
                alerts.Dequeue();
            }
        }
    }

    public IReadOnlyCollection<OperationalAlert> GetAlerts()
    {
        lock (alertsLock)
        {
            return alerts.Reverse().ToArray();
        }
    }
}