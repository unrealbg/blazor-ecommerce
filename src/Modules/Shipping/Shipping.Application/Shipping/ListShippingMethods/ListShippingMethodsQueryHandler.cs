using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShippingMethods;

public sealed class ListShippingMethodsQueryHandler(IShippingMethodRepository repository)
    : IQueryHandler<ListShippingMethodsQuery, IReadOnlyCollection<ShippingMethodDto>>
{
    public async Task<IReadOnlyCollection<ShippingMethodDto>> Handle(
        ListShippingMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var methods = await repository.ListAsync(request.ActiveOnly, cancellationToken);
        return methods.Select(ShippingMappings.ToDto).ToList();
    }
}
