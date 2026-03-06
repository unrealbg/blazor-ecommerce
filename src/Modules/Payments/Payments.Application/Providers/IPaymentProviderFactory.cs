namespace Payments.Application.Providers;

public interface IPaymentProviderFactory
{
    IPaymentProvider Resolve(string? provider);
}
