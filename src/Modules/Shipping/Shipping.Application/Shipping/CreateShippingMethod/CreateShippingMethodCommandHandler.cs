using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping.CreateShippingMethod;

public sealed class CreateShippingMethodCommandHandler(
    IShippingMethodRepository repository,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CreateShippingMethodCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateShippingMethodCommand request,
        CancellationToken cancellationToken)
    {
        var existingMethod = await repository.GetByCodeAsync(request.Code.Trim(), cancellationToken);
        if (existingMethod is not null)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.method.code.conflict",
                "Shipping method code already exists."));
        }

        var createResult = ShippingMethod.Create(
            request.Code,
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
            clock.UtcNow);

        if (createResult.IsFailure)
        {
            return Result<Guid>.Failure(createResult.Error);
        }

        await repository.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(createResult.Value.Id);
    }
}
