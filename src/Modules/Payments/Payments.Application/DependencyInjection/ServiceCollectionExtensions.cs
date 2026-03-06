using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Payments.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(AssemblyReference.Assembly);
        return services;
    }
}
