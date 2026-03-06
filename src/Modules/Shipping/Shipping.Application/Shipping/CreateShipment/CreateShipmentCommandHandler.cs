using System.Text.Json;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Application.Providers;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping.CreateShipment;

public sealed class CreateShipmentCommandHandler(
    IOrderFulfillmentService orderFulfillmentService,
    IShippingMethodRepository shippingMethodRepository,
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository,
    IShippingCarrierProviderFactory carrierProviderFactory,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CreateShipmentCommand, Guid>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<Guid>> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var order = await orderFulfillmentService.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.order.not_found",
                "Order was not found."));
        }

        if (!string.Equals(order.Status, "Paid", StringComparison.Ordinal))
        {
            return Result<Guid>.Failure(new Error(
                "shipping.order.not_fulfillable",
                "Order is not paid and cannot be fulfilled."));
        }

        var existingShipment = await shipmentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existingShipment is not null)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.shipment.already_created",
                "A shipment already exists for this order."));
        }

        var shippingMethodCode = string.IsNullOrWhiteSpace(request.ShippingMethodCode)
            ? order.ShippingMethodCode
            : request.ShippingMethodCode.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(shippingMethodCode))
        {
            return Result<Guid>.Failure(new Error(
                "shipping.method.required",
                "Shipping method code is required."));
        }

        var shippingMethod = await shippingMethodRepository.GetByCodeAsync(shippingMethodCode, cancellationToken);
        if (shippingMethod is null)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.method.not_found",
                "Shipping method was not found."));
        }

        IShippingCarrierProvider carrierProvider;
        try
        {
            carrierProvider = carrierProviderFactory.Resolve(shippingMethod.Provider);
        }
        catch (InvalidOperationException)
        {
            return Result<Guid>.Failure(new Error(
                "shipping.carrier.unavailable",
                "Shipping carrier provider is unavailable."));
        }

        var addressSnapshotJson = JsonSerializer.Serialize(order.ShippingAddress, JsonOptions);
        var recipientName = $"{order.ShippingAddress.FirstName} {order.ShippingAddress.LastName}".Trim();
        var createShipmentResult = Shipment.Create(
            request.OrderId,
            shippingMethod.Id,
            carrierName: carrierProvider.Name,
            shippingMethod.Type,
            recipientName,
            order.ShippingAddress.Phone,
            addressSnapshotJson,
            order.ShippingPriceAmount,
            order.ShippingCurrency,
            clock.UtcNow);

        if (createShipmentResult.IsFailure)
        {
            return Result<Guid>.Failure(createShipmentResult.Error);
        }

        var shipment = createShipmentResult.Value;
        var providerResponse = await carrierProvider.CreateShipmentAsync(
            new CarrierCreateShipmentRequest(
                shipment.Id,
                request.OrderId,
                shippingMethod.Code,
                shippingMethod.Name,
                recipientName,
                order.ShippingAddress.Phone,
                addressSnapshotJson,
                order.ShippingPriceAmount,
                order.ShippingCurrency),
            cancellationToken);

        var trackingResult = shipment.SetTracking(providerResponse.TrackingNumber, providerResponse.TrackingUrl, clock.UtcNow);
        if (trackingResult.IsFailure)
        {
            return Result<Guid>.Failure(trackingResult.Error);
        }

        var createEventResult = ShipmentEvent.Create(
            shipment.Id,
            ShipmentEventType.StatusChanged,
            "Shipment created",
            null,
            clock.UtcNow,
            metadataJson: null);
        if (createEventResult.IsFailure)
        {
            return Result<Guid>.Failure(createEventResult.Error);
        }

        await shipmentRepository.AddAsync(shipment, cancellationToken);
        await shipmentEventRepository.AddAsync(createEventResult.Value, cancellationToken);

        if (!string.IsNullOrWhiteSpace(providerResponse.LabelReference))
        {
            var markLabelResult = shipment.MarkLabelCreated(clock.UtcNow);
            if (markLabelResult.IsFailure)
            {
                return Result<Guid>.Failure(markLabelResult.Error);
            }

            var labelEventResult = ShipmentEvent.Create(
                shipment.Id,
                ShipmentEventType.LabelCreated,
                "Shipment label created",
                providerResponse.LabelReference,
                clock.UtcNow,
                metadataJson: null);
            if (labelEventResult.IsFailure)
            {
                return Result<Guid>.Failure(labelEventResult.Error);
            }

            await shipmentEventRepository.AddAsync(labelEventResult.Value, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(shipment.Id);
    }
}
