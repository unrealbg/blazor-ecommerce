using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping.CreateShippingRateRule;

public sealed class CreateShippingRateRuleCommandHandler(
    IShippingMethodRepository shippingMethodRepository,
    IShippingZoneRepository shippingZoneRepository,
    IShippingRateRuleRepository shippingRateRuleRepository,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CreateShippingRateRuleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateShippingRateRuleCommand request,
        CancellationToken cancellationToken)
    {
        var method = await shippingMethodRepository.GetByIdAsync(request.ShippingMethodId, cancellationToken);
        if (method is null)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.method.not_found",
                "Shipping method was not found."));
        }

        var zone = await shippingZoneRepository.GetByIdAsync(request.ShippingZoneId, cancellationToken);
        if (zone is null)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.zone.not_found",
                "Shipping zone was not found."));
        }

        var createResult = ShippingRateRule.Create(
            request.ShippingMethodId,
            request.ShippingZoneId,
            request.MinOrderAmount,
            request.MaxOrderAmount,
            request.MinWeightKg,
            request.MaxWeightKg,
            request.PriceAmount,
            request.FreeShippingThresholdAmount,
            request.Currency,
            clock.UtcNow);

        if (createResult.IsFailure)
        {
            return Result<Guid>.Failure(createResult.Error);
        }

        await shippingRateRuleRepository.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(createResult.Value.Id);
    }
}
