using System.Text.Json;
using Microsoft.Extensions.Options;
using Shipping.Application.Providers;
using Shipping.Application.Shipping;

namespace Shipping.Infrastructure.Providers;

internal sealed class DemoCarrierProvider(IOptions<DemoCarrierOptions> options) : IShippingCarrierProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DemoCarrierOptions options = options.Value;

    public string Name => "DemoCarrier";

    public Task<CarrierQuoteResponse> QuoteAsync(CarrierQuoteRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CarrierQuoteResponse(
            PriceAmount: null,
            Currency: request.Currency,
            EstimatedMinDays: 2,
            EstimatedMaxDays: 4));
    }

    public Task<CarrierCreateShipmentResponse> CreateShipmentAsync(
        CarrierCreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var trackingNumber = $"DEMO-{DateTime.UtcNow:yyyyMMdd}-{request.ShipmentId.ToString("N")[..10].ToUpperInvariant()}";
        var trackingUrl = BuildTrackingUrl(trackingNumber);
        var labelReference = $"LBL-{request.ShipmentId:N}";

        return Task.FromResult(new CarrierCreateShipmentResponse(
            CarrierName: Name,
            CarrierServiceCode: request.ShippingMethodCode,
            TrackingNumber: trackingNumber,
            TrackingUrl: trackingUrl,
            LabelReference: labelReference));
    }

    public Task<CarrierCreateLabelResponse> CreateLabelAsync(
        CarrierCreateLabelRequest request,
        CancellationToken cancellationToken)
    {
        var labelReference = $"LBL-{request.ShipmentId:N}";
        var trackingNumber = request.TrackingNumber ?? $"DEMO-{request.ShipmentId.ToString("N")[..12].ToUpperInvariant()}";
        return Task.FromResult(new CarrierCreateLabelResponse(labelReference, BuildTrackingUrl(trackingNumber)));
    }

    public Task<CarrierTrackingResponse> TrackShipmentAsync(
        CarrierTrackingRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new CarrierTrackingResponse(
            Status: "InTransit",
            TrackingNumber: request.TrackingNumber,
            TrackingUrl: request.TrackingUrl,
            Message: "Demo carrier status response."));
    }

    public Task<CarrierCancelShipmentResponse> CancelShipmentAsync(
        CarrierCancelShipmentRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new CarrierCancelShipmentResponse(
            Cancelled: true,
            Message: "Demo shipment cancelled."));
    }

    public Task<CarrierWebhookParseResult> ParseWebhookAsync(string payload, CancellationToken cancellationToken)
    {
        var parsedPayload = JsonSerializer.Deserialize<DemoCarrierWebhookPayload>(payload, JsonOptions)
                           ?? throw new InvalidOperationException("Carrier webhook payload is invalid.");

        if (string.IsNullOrWhiteSpace(parsedPayload.EventId))
        {
            throw new InvalidOperationException("Carrier webhook event id is required.");
        }

        return Task.FromResult(new CarrierWebhookParseResult(
            parsedPayload.EventId.Trim(),
            parsedPayload.EventType?.Trim() ?? "carrier.unknown",
            parsedPayload.ShipmentId,
            parsedPayload.TrackingNumber,
            parsedPayload.Status,
            parsedPayload.Message,
            payload));
    }

    private string? BuildTrackingUrl(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(this.options.BaseTrackingUrl))
        {
            return null;
        }

        var baseUrl = this.options.BaseTrackingUrl.TrimEnd('/');
        return $"{baseUrl}/{Uri.EscapeDataString(trackingNumber)}";
    }

    private sealed record DemoCarrierWebhookPayload(
        string EventId,
        string? EventType,
        Guid? ShipmentId,
        string? TrackingNumber,
        string? Status,
        string? Message);
}
