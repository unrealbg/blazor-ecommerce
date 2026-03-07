namespace AppHost.Configuration;

public sealed class AppObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "blazor-ecommerce-app";

    public bool EnableConsoleExporter { get; set; } = true;

    public string? OtlpEndpoint { get; set; }

    public string CorrelationHeaderName { get; set; } = "X-Correlation-Id";
}