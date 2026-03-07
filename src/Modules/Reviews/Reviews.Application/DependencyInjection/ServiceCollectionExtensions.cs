using BuildingBlocks.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Reviews.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReviewsApplication(this IServiceCollection services)
    {
        services.AddModuleApplication(typeof(AssemblyReference).Assembly);
        return services;
    }
}
