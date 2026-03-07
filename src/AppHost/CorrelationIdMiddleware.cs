using System.Diagnostics;
using AppHost.Configuration;
using Microsoft.Extensions.Options;
using Serilog.Context;

public sealed class CorrelationIdMiddleware(RequestDelegate next, IOptions<AppObservabilityOptions> options)
{
    private readonly AppObservabilityOptions options = options.Value;

    public async Task Invoke(HttpContext context)
    {
        var headerName = string.IsNullOrWhiteSpace(options.CorrelationHeaderName)
            ? "X-Correlation-Id"
            : options.CorrelationHeaderName;

        var correlationId = context.Request.Headers.TryGetValue(headerName, out var existingValue) && !string.IsNullOrWhiteSpace(existingValue)
            ? existingValue.ToString().Trim()
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[headerName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
        {
            await next(context);
        }
    }
}