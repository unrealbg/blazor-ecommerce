namespace Backoffice.Application.Backoffice;

public sealed record BackofficeShipmentSummaryDto(
    Guid Id,
    string Status,
    string CarrierName,
    string? CarrierServiceCode,
    string? TrackingNumber,
    string? TrackingUrl,
    decimal ShippingPriceAmount,
    string Currency,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc,
    IReadOnlyCollection<BackofficeShipmentEventDto> Events);
