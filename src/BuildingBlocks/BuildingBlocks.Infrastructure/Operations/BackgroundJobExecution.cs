using System.Diagnostics;
using BuildingBlocks.Application.Diagnostics;

namespace BuildingBlocks.Infrastructure.Operations;

public sealed class BackgroundJobExecution : IDisposable
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private readonly IOperationalStateRegistry registry;
    private bool completed;

    internal BackgroundJobExecution(IOperationalStateRegistry registry, string workerName, string? correlationId)
    {
        this.registry = registry;
        WorkerName = workerName;
        registry.RecordWorkerStarted(workerName, correlationId);
    }

    public string WorkerName { get; }

    public void Complete(int? processedCount = null, string? note = null)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        stopwatch.Stop();
        registry.RecordWorkerSucceeded(WorkerName, stopwatch.Elapsed.TotalMilliseconds, processedCount, note);
        CommerceDiagnostics.RecordBackgroundJobDuration(WorkerName, stopwatch.Elapsed.TotalMilliseconds, "success");
    }

    public void Fail(Exception exception)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        stopwatch.Stop();
        registry.RecordWorkerFailed(WorkerName, exception, stopwatch.Elapsed.TotalMilliseconds);
        CommerceDiagnostics.RecordBackgroundJobDuration(WorkerName, stopwatch.Elapsed.TotalMilliseconds, "failure");
    }

    public void Dispose()
    {
        if (!completed)
        {
            Complete();
        }
    }
}