using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Application.Providers;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping.CreateShipmentLabel;

public sealed class CreateShipmentLabelCommandHandler(
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository,
    IShippingCarrierProviderFactory carrierProviderFactory,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CreateShipmentLabelCommand, bool>
{
    public async Task<Result<bool>> Handle(CreateShipmentLabelCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return Result<bool>.Failure(new Error(
                "shipping.shipment.not_found",
                "Shipment was not found."));
        }

        IShippingCarrierProvider carrierProvider;
        try
        {
            carrierProvider = carrierProviderFactory.Resolve(shipment.CarrierName);
        }
        catch (InvalidOperationException)
        {
            return Result<bool>.Failure(new Error(
                "shipping.carrier.unavailable",
                "Shipping carrier provider is unavailable."));
        }

        var labelResponse = await carrierProvider.CreateLabelAsync(
            new CarrierCreateLabelRequest(
                shipment.Id,
                shipment.TrackingNumber,
                shipment.TrackingUrl),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(labelResponse.TrackingUrl) &&
            !string.Equals(labelResponse.TrackingUrl, shipment.TrackingUrl, StringComparison.Ordinal))
        {
            var trackingResult = shipment.SetTracking(
                shipment.TrackingNumber ?? $"demo-{shipment.Id:N}",
                labelResponse.TrackingUrl,
                clock.UtcNow);
            if (trackingResult.IsFailure)
            {
                return Result<bool>.Failure(trackingResult.Error);
            }
        }

        var statusResult = shipment.MarkLabelCreated(clock.UtcNow);
        if (statusResult.IsFailure)
        {
            return Result<bool>.Failure(statusResult.Error);
        }

        var createEventResult = ShipmentEvent.Create(
            shipment.Id,
            ShipmentEventType.LabelCreated,
            "Shipment label created manually",
            labelResponse.LabelReference,
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
