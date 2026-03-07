using BuildingBlocks.Application.Operations;

namespace BuildingBlocks.Infrastructure.Operations;

public interface IOperationalStateRegistry
{
    void RecordWorkerStarted(string workerName, string? correlationId = null);

    void RecordWorkerSucceeded(string workerName, double durationMs, int? processedCount = null, string? note = null);

    void RecordWorkerFailed(string workerName, Exception exception, double durationMs);

    void UpdateSnapshot(OperationalSnapshot snapshot);

    OperationalSnapshot GetSnapshot();

    IReadOnlyCollection<WorkerExecutionState> GetWorkers();

    void RecordAlert(OperationalAlert alert);

    IReadOnlyCollection<OperationalAlert> GetAlerts();
}