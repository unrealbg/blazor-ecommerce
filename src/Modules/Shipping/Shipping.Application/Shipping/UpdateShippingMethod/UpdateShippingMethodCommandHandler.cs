using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Shipping.Application.Shipping.UpdateShippingMethod;

public sealed class UpdateShippingMethodCommandHandler(
    IShippingMethodRepository repository,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<UpdateShippingMethodCommand, bool>
{
    public async Task<Result<bool>> Handle(
        UpdateShippingMethodCommand request,
        CancellationToken cancellationToken)
    {
        var shippingMethod = await repository.GetByIdAsync(request.ShippingMethodId, cancellationToken);
        if (shippingMethod is null)
        {
            return Result<bool>.Failure(new Error(
                "shipping.method.not_found",
                "Shipping method was not found."));
        }

        var updateResult = shippingMethod.Update(
            request.Name,
            request.Description,
            request.Provider,
            request.Type,
            request.BasePriceAmount,
            request.Currency,
            request.SupportsTracking,
            request.SupportsPickupPoint,
            request.EstimatedMinDays,
            request.EstimatedMaxDays,
            request.Priority,
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
