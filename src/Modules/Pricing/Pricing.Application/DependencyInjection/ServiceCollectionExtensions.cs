using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Pricing.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPricingApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
