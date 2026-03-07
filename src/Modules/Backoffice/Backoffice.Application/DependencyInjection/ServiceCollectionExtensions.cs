using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Backoffice.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackofficeApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(typeof(AssemblyReference).Assembly);
        return services;
    }
}
