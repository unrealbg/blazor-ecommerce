using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping.MarkShipmentShipped;

public sealed class MarkShipmentShippedCommandHandler(
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<MarkShipmentShippedCommand, bool>
{
    public async Task<Result<bool>> Handle(MarkShipmentShippedCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return Result<bool>.Failure(new Error(
                "shipping.shipment.not_found",
                "Shipment was not found."));
        }

        var result = shipment.MarkShipped(clock.UtcNow);
        if (result.IsFailure)
        {
            return Result<bool>.Failure(result.Error);
        }

        var createEventResult = ShipmentEvent.Create(
            shipment.Id,
            ShipmentEventType.StatusChanged,
            "Shipment marked as shipped",
            externalEventId: null,
            clock.UtcNow,
            metadataJson: null);
        if (createEventResult.IsFailure)
        {
            return Result<bool>.Failure(createEventResult.Error);
        }

        await shipmentEventRepository.AddAsync(createEventResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
