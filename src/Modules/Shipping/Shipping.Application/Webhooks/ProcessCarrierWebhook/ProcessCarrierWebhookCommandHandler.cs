using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Shipping.Application.Providers;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Webhooks.ProcessCarrierWebhook;

public sealed class ProcessCarrierWebhookCommandHandler(
    ICarrierWebhookInboxRepository webhookInboxRepository,
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository,
    IShippingCarrierProviderFactory carrierProviderFactory,
    IShippingUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<ProcessCarrierWebhookCommand, bool>
{
    public async Task<Result<bool>> Handle(ProcessCarrierWebhookCommand request, CancellationToken cancellationToken)
    {
        IShippingCarrierProvider carrierProvider;
        try
        {
            carrierProvider = carrierProviderFactory.Resolve(request.Provider);
        }
        catch (InvalidOperationException)
        {
            return Result<bool>.Failure(new Error(
                "shipping.carrier.unavailable",
                "Shipping carrier provider is unavailable."));
        }

        var webhook = await carrierProvider.ParseWebhookAsync(request.Payload, cancellationToken);

        var existing = await webhookInboxRepository.GetByProviderAndEventIdAsync(
            request.Provider,
            webhook.ExternalEventId,
            cancellationToken);
        if (existing is not null)
        {
            return Result<bool>.Success(false);
        }

        var createInboxResult = CarrierWebhookInboxMessage.Create(
            request.Provider,
            webhook.ExternalEventId,
            webhook.EventType,
            request.Payload,
            clock.UtcNow);
        if (createInboxResult.IsFailure)
        {
            return Result<bool>.Failure(createInboxResult.Error);
        }

        var inboxMessage = createInboxResult.Value;
        await webhookInboxRepository.AddAsync(inboxMessage, cancellationToken);

        if (webhook.ShipmentId is null || webhook.ShipmentId.Value == Guid.Empty)
        {
            inboxMessage.MarkIgnored(clock.UtcNow, "Shipment id was not found in webhook payload.");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(false);
        }

        var shipment = await shipmentRepository.GetByIdAsync(webhook.ShipmentId.Value, cancellationToken);
        if (shipment is null)
        {
            inboxMessage.MarkIgnored(clock.UtcNow, "Shipment was not found.");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(false);
        }

        var statusResult = ApplyStatus(shipment, webhook.Status, webhook.Message, clock.UtcNow);
        if (statusResult.IsFailure)
        {
            inboxMessage.MarkFailed(clock.UtcNow, statusResult.Error.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure(statusResult.Error);
        }

        if (!string.IsNullOrWhiteSpace(webhook.TrackingNumber))
        {
            var trackingResult = shipment.SetTracking(
                webhook.TrackingNumber,
                webhook.MetadataJson is null ? shipment.TrackingUrl : shipment.TrackingUrl,
                clock.UtcNow);
            if (trackingResult.IsFailure)
            {
                inboxMessage.MarkFailed(clock.UtcNow, trackingResult.Error.Message);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<bool>.Failure(trackingResult.Error);
            }
        }

        var createEventResult = ShipmentEvent.Create(
            shipment.Id,
            ShipmentEventType.CarrierWebhook,
            webhook.Message,
            webhook.ExternalEventId,
            clock.UtcNow,
            webhook.MetadataJson);
        if (createEventResult.IsFailure)
        {
            inboxMessage.MarkFailed(clock.UtcNow, createEventResult.Error.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure(createEventResult.Error);
        }

        await shipmentEventRepository.AddAsync(createEventResult.Value, cancellationToken);
        inboxMessage.MarkProcessed(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static Result ApplyStatus(Shipment shipment, string? status, string? message, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return Result.Success();
        }

        var normalizedStatus = status.Trim();
        return normalizedStatus.ToLowerInvariant() switch
        {
            "pending" => Result.Success(),
            "labelcreated" => shipment.MarkLabelCreated(utcNow),
            "readyforpickup" => shipment.MarkReadyForPickup(utcNow),
            "shipped" => shipment.MarkShipped(utcNow),
            "intransit" => shipment.MarkInTransit(utcNow),
            "outfordelivery" => shipment.MarkOutForDelivery(utcNow),
            "delivered" => shipment.MarkDelivered(utcNow),
            "failed" => shipment.MarkFailed(message, utcNow),
            "returned" => shipment.MarkReturned(message, utcNow),
            "cancelled" => shipment.MarkCancelled(utcNow),
            _ => Result.Failure(new Error(
                "shipping.webhook.status.invalid",
                $"Unsupported shipment status '{status}'.")),
        };
    }
}
