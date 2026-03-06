using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.GetShippingMethod;

public sealed class GetShippingMethodQueryHandler(IShippingMethodRepository repository)
    : IQueryHandler<GetShippingMethodQuery, ShippingMethodDto?>
{
    public async Task<ShippingMethodDto?> Handle(
        GetShippingMethodQuery request,
        CancellationToken cancellationToken)
    {
        var method = await repository.GetByIdAsync(request.ShippingMethodId, cancellationToken);
        return method?.ToDto();
    }
}
