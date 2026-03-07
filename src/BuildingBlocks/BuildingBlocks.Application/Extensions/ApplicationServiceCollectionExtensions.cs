using System.Reflection;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.Contracts;
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
        services.TryAddScoped<IRedirectRuleWriter, NullRedirectRuleWriter>();
        services.TryAddScoped<IProductSearchIndexer, NullProductSearchIndexer>();
        services.TryAddScoped<ICustomerCheckoutAccessor, NullCustomerCheckoutAccessor>();
        services.TryAddScoped<ICustomerSessionCache, NullCustomerSessionCache>();
        services.TryAddScoped<IInventoryAvailabilityReader, NullInventoryAvailabilityReader>();
        services.TryAddScoped<IInventoryReservationService, NullInventoryReservationService>();
        services.TryAddScoped<IInventoryStockProvisioner, NullInventoryStockProvisioner>();
        services.TryAddScoped<IOrderPaymentService, NullOrderPaymentService>();
        services.TryAddScoped<IOrderPricingReader, NullOrderPricingReader>();
        services.TryAddScoped<IShippingQuoteService, NullShippingQuoteService>();
        services.TryAddScoped<IOrderFulfillmentService, NullOrderFulfillmentService>();
        services.TryAddScoped<IVariantPricingService, NullVariantPricingService>();
        services.TryAddScoped<ICartPricingService, NullCartPricingService>();
        services.TryAddScoped<IPricingRedemptionService, NullPricingRedemptionService>();

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
