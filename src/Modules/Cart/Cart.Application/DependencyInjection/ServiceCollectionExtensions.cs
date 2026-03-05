using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
