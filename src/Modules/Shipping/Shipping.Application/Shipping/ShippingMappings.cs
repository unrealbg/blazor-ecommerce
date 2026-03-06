using System.Text.Json;
using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping;

internal static class ShippingMappings
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ShippingMethodDto ToDto(this ShippingMethod method)
    {
        return new ShippingMethodDto(
            method.Id,
            method.Code,
            method.Name,
            method.Description,
            method.Provider,
            method.Type,
            method.BasePriceAmount,
            method.Currency,
            method.IsActive,
            method.SupportsTracking,
            method.SupportsPickupPoint,
            method.EstimatedMinDays,
            method.EstimatedMaxDays,
            method.Priority,
            method.CreatedAtUtc,
            method.UpdatedAtUtc);
    }

    public static ShippingZoneDto ToDto(this ShippingZone zone)
    {
        return new ShippingZoneDto(
            zone.Id,
            zone.Code,
            zone.Name,
            DeserializeCountries(zone.CountryCodesJson),
            zone.IsActive,
            zone.CreatedAtUtc,
            zone.UpdatedAtUtc);
    }

    public static ShippingRateRuleDto ToDto(this ShippingRateRule rule)
    {
        return new ShippingRateRuleDto(
            rule.Id,
            rule.ShippingMethodId,
            rule.ShippingZoneId,
            rule.MinOrderAmount,
            rule.MaxOrderAmount,
            rule.MinWeightKg,
            rule.MaxWeightKg,
            rule.PriceAmount,
            rule.FreeShippingThresholdAmount,
            rule.Currency,
            rule.IsActive,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc);
    }

    public static ShipmentDto ToDto(this Shipment shipment, IReadOnlyCollection<ShipmentEvent> events)
    {
        return new ShipmentDto(
            shipment.Id,
            shipment.OrderId,
            shipment.ShippingMethodId,
            shipment.CarrierName,
            shipment.CarrierServiceCode,
            shipment.TrackingNumber,
            shipment.TrackingUrl,
            shipment.Status.ToString(),
            shipment.RecipientName,
            shipment.RecipientPhone,
            shipment.AddressSnapshotJson,
            shipment.ShippingPriceAmount,
            shipment.Currency,
            shipment.ShippedAtUtc,
            shipment.DeliveredAtUtc,
            shipment.CreatedAtUtc,
            shipment.UpdatedAtUtc,
            events.Select(ToDto).ToList());
    }

    public static ShipmentEventDto ToDto(this ShipmentEvent shipmentEvent)
    {
        return new ShipmentEventDto(
            shipmentEvent.Id,
            shipmentEvent.EventType.ToString(),
            shipmentEvent.Message,
            shipmentEvent.ExternalEventId,
            shipmentEvent.OccurredAtUtc,
            shipmentEvent.MetadataJson);
    }

    private static IReadOnlyCollection<string> DeserializeCountries(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyCollection<string>>(payload, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
