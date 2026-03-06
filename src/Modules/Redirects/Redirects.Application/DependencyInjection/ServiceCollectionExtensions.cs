using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Redirects.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedirectsApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
