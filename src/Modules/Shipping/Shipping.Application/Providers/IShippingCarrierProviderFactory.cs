namespace Shipping.Application.Providers;

public interface IShippingCarrierProviderFactory
{
    IShippingCarrierProvider Resolve(string providerName);
}
