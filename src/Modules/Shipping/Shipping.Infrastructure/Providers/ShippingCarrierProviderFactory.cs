using Shipping.Application.Providers;

namespace Shipping.Infrastructure.Providers;

internal sealed class ShippingCarrierProviderFactory(IEnumerable<IShippingCarrierProvider> providers)
    : IShippingCarrierProviderFactory
{
    private readonly IReadOnlyDictionary<string, IShippingCarrierProvider> providersByName = providers
        .ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);

    public IShippingCarrierProvider Resolve(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("Carrier provider name is required.");
        }

        if (providersByName.TryGetValue(providerName.Trim(), out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"Carrier provider '{providerName}' is not registered.");
    }
}
