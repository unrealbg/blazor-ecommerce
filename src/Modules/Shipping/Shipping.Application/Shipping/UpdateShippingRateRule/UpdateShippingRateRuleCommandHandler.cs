using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Shipping.Application.Shipping.UpdateShippingRateRule;

public sealed class UpdateShippingRateRuleCommandHandler(
    IShippingRateRuleRepository shippingRateRuleRepository,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<UpdateShippingRateRuleCommand, bool>
{
    public async Task<Result<bool>> Handle(
        UpdateShippingRateRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await shippingRateRuleRepository.GetByIdAsync(request.ShippingRateRuleId, cancellationToken);
        if (rule is null)
        {
            return Result<bool>.Failure(new Error(
                "shipping.rule.not_found",
                "Shipping rate rule was not found."));
        }

        var updateResult = rule.Update(
            request.MinOrderAmount,
            request.MaxOrderAmount,
            request.MinWeightKg,
            request.MaxWeightKg,
            request.PriceAmount,
            request.FreeShippingThresholdAmount,
            request.Currency,
            request.IsActive,
            clock.UtcNow);

        if (updateResult.IsFailure)
        {
            return Result<bool>.Failure(updateResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
