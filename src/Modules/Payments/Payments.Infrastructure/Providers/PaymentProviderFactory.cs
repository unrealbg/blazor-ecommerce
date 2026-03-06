using Microsoft.Extensions.Options;
using Payments.Application.Payments;
using Payments.Application.Providers;

namespace Payments.Infrastructure.Providers;

internal sealed class PaymentProviderFactory(
    IEnumerable<IPaymentProvider> providers,
    IOptions<PaymentsModuleOptions> options)
    : IPaymentProviderFactory
{
    private readonly Dictionary<string, IPaymentProvider> providersByName = providers
        .ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);

    private readonly PaymentsModuleOptions options = options.Value;

    public IPaymentProvider Resolve(string? provider)
    {
        var providerName = string.IsNullOrWhiteSpace(provider)
            ? options.DefaultProvider
            : provider.Trim();

        if (!providersByName.TryGetValue(providerName, out var resolvedProvider))
        {
            throw new InvalidOperationException($"Payment provider '{providerName}' is not registered.");
        }

        return resolvedProvider;
    }
}
