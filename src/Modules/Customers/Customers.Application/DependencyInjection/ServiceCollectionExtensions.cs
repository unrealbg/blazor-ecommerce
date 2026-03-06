using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomersApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
