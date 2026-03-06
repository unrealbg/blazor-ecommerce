using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
