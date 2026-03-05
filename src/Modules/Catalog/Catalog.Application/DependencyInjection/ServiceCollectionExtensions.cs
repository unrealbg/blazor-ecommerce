using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
