using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping.CreateShippingZone;

public sealed class CreateShippingZoneCommandHandler(
    IShippingZoneRepository repository,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CreateShippingZoneCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateShippingZoneCommand request, CancellationToken cancellationToken)
    {
        var existingZone = await repository.GetByCodeAsync(request.Code.Trim(), cancellationToken);
        if (existingZone is not null)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.zone.code.conflict",
                "Shipping zone code already exists."));
        }

        var createResult = ShippingZone.Create(
            request.Code,
            request.Name,
            request.CountryCodes,
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
