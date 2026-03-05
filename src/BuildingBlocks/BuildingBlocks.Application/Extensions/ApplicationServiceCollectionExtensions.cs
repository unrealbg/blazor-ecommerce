using System.Reflection;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        return services;
    }

    public static IServiceCollection AddModuleApplication(this IServiceCollection services, Assembly assembly)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        RegisterDomainEventHandlers(services, assembly);

        return services;
    }

    private static void RegisterDomainEventHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterfaceType = typeof(IDomainEventHandler<>);

        var registrations = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type
                .GetInterfaces()
                .Where(contract => contract.IsGenericType &&
                                   contract.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(contract => new { Service = contract, Implementation = type }))
            .ToList();

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.Service, registration.Implementation);
        }
    }
}
