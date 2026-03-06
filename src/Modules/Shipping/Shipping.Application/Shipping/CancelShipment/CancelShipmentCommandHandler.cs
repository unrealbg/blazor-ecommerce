using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Application.Providers;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping.CancelShipment;

public sealed class CancelShipmentCommandHandler(
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository,
    IShippingCarrierProviderFactory carrierProviderFactory,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CancelShipmentCommand, bool>
{
    public async Task<Result<bool>> Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return Result<bool>.Failure(new Error(
                "shipping.shipment.not_found",
                "Shipment was not found."));
        }

        IShippingCarrierProvider provider;
        try
        {
            provider = carrierProviderFactory.Resolve(shipment.CarrierName);
        }
        catch (InvalidOperationException)
        {
            return Result<bool>.Failure(new Error(
                "shipping.carrier.unavailable",
                "Shipping carrier provider is unavailable."));
        }

        var response = await provider.CancelShipmentAsync(
            new CarrierCancelShipmentRequest(shipment.Id, shipment.TrackingNumber, request.Reason),
            cancellationToken);
        if (!response.Cancelled)
        {
            return Result<bool>.Failure(new Error(
                "shipping.shipment.cancel.not_allowed",
                response.Message ?? "Shipment cannot be cancelled."));
        }

        var statusResult = shipment.MarkCancelled(clock.UtcNow);
        if (statusResult.IsFailure)
        {
            return Result<bool>.Failure(statusResult.Error);
        }

        var createEventResult = ShipmentEvent.Create(
            shipment.Id,
            ShipmentEventType.StatusChanged,
            "Shipment cancelled",
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
