namespace Shipping.Application.Providers;

public interface IShippingCarrierProvider
{
    string Name { get; }

    Task<CarrierQuoteResponse> QuoteAsync(CarrierQuoteRequest request, CancellationToken cancellationToken);

    Task<CarrierCreateShipmentResponse> CreateShipmentAsync(
        CarrierCreateShipmentRequest request,
        CancellationToken cancellationToken);

    Task<CarrierCreateLabelResponse> CreateLabelAsync(
        CarrierCreateLabelRequest request,
        CancellationToken cancellationToken);

    Task<CarrierTrackingResponse> TrackShipmentAsync(
        CarrierTrackingRequest request,
        CancellationToken cancellationToken);

    Task<CarrierCancelShipmentResponse> CancelShipmentAsync(
        CarrierCancelShipmentRequest request,
        CancellationToken cancellationToken);

    Task<CarrierWebhookParseResult> ParseWebhookAsync(string payload, CancellationToken cancellationToken);
}
