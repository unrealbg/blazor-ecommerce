using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Diagnostics;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;
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
    IClock clock,
    ILogger<ProcessCarrierWebhookCommandHandler> logger)
    : ICommandHandler<ProcessCarrierWebhookCommand, bool>
{
    public async Task<Result<bool>> Handle(ProcessCarrierWebhookCommand request, CancellationToken cancellationToken)
    {
        using var activity = CommerceDiagnostics.StartActivity("shipping.webhook.process");
        activity?.SetTag("shipping.provider", request.Provider);

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
            if (existing.ProcessingStatus == CarrierWebhookInboxProcessingStatus.Failed)
            {
                existing.RequeueForProcessing();
            }
            else
            {
                logger.LogInformation(
                    "Ignoring duplicate shipping webhook {Provider} {ExternalEventId}",
                    request.Provider,
                    webhook.ExternalEventId);
                return Result<bool>.Success(false);
            }
        }

        CarrierWebhookInboxMessage inboxMessage;
        if (existing is null)
        {
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

            inboxMessage = createInboxResult.Value;
            await webhookInboxRepository.AddAsync(inboxMessage, cancellationToken);
        }
        else
        {
            inboxMessage = existing;
        }

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

        activity?.SetTag("shipment.id", shipment.Id);

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

        logger.LogInformation(
            "Processed shipping webhook {Provider} {ExternalEventId} for shipment {ShipmentId}",
            request.Provider,
            webhook.ExternalEventId,
            shipment.Id);

        if (string.Equals(webhook.Status, "delivered", StringComparison.OrdinalIgnoreCase))
        {
            CommerceDiagnostics.RecordShipment("delivery", request.Provider, "success");
        }

        if (string.Equals(webhook.Status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            CommerceDiagnostics.RecordShipment("delivery", request.Provider, "failure");
            CommerceDiagnostics.RecordShippingWebhookFailure(request.Provider);
        }

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
