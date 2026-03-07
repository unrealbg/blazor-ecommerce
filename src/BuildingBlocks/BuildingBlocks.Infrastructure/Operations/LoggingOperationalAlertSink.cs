using BuildingBlocks.Application.Operations;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Operations;

internal sealed class LoggingOperationalAlertSink(
    ILogger<LoggingOperationalAlertSink> logger,
    IOperationalStateRegistry operationalStateRegistry) : IOperationalAlertSink
{
    public Task PublishAsync(OperationalAlert alert, CancellationToken cancellationToken)
    {
        operationalStateRegistry.RecordAlert(alert);
        logger.LogWarning(
            "Operational alert {Code} {Severity} {Summary} {@Context}",
            alert.Code,
            alert.Severity,
            alert.Summary,
            alert.Context);

        return Task.CompletedTask;
    }
}