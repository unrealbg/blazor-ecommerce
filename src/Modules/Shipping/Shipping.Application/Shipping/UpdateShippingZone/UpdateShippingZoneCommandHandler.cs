using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Shipping.Application.Shipping.UpdateShippingZone;

public sealed class UpdateShippingZoneCommandHandler(
    IShippingZoneRepository repository,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<UpdateShippingZoneCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateShippingZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await repository.GetByIdAsync(request.ShippingZoneId, cancellationToken);
        if (zone is null)
        {
            return Result<bool>.Failure(new Error(
                "shipping.zone.not_found",
                "Shipping zone was not found."));
        }

        var updateResult = zone.Update(
            request.Name,
            request.CountryCodes,
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
