using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShippingRateRules;

public sealed class ListShippingRateRulesQueryHandler(IShippingRateRuleRepository repository)
    : IQueryHandler<ListShippingRateRulesQuery, IReadOnlyCollection<ShippingRateRuleDto>>
{
    public async Task<IReadOnlyCollection<ShippingRateRuleDto>> Handle(
        ListShippingRateRulesQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await repository.ListAsync(request.ActiveOnly, cancellationToken);
        return rules.Select(ShippingMappings.ToDto).ToList();
    }
}
