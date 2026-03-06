namespace Payments.Application.Payments;

public sealed class DemoPaymentProviderOptions
{
    public bool AutoCaptureOnCreate { get; set; } = true;

    public bool SimulateRequiresAction { get; set; }

    public decimal SimulateFailureRate { get; set; }
}
