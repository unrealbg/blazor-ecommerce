using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Shipping.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShippingApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
