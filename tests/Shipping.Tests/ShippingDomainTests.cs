using Shipping.Domain.Shipping;

namespace Shipping.Tests;

public sealed class ShippingDomainTests
{
    [Fact]
    public void ShippingRateRule_Should_MatchOrderAmountWithinRange()
    {
        var createResult = ShippingRateRule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            minOrderAmount: 50m,
            maxOrderAmount: 200m,
            minWeightKg: null,
            maxWeightKg: null,
            priceAmount: 7.99m,
            freeShippingThresholdAmount: null,
            currency: "EUR",
            DateTime.UtcNow);

        Assert.True(createResult.IsSuccess);
        Assert.True(createResult.Value.Matches(subtotalAmount: 120m, totalWeightKg: null));
        Assert.False(createResult.Value.Matches(subtotalAmount: 10m, totalWeightKg: null));
    }

    [Fact]
    public void ShippingRateRule_Should_ApplyFreeShippingThreshold()
    {
        var createResult = ShippingRateRule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            minOrderAmount: null,
            maxOrderAmount: null,
            minWeightKg: null,
            maxWeightKg: null,
            priceAmount: 9.99m,
            freeShippingThresholdAmount: 100m,
            currency: "EUR",
            DateTime.UtcNow);

        Assert.True(createResult.IsSuccess);
        Assert.Equal(0m, createResult.Value.ResolvePrice(150m));
        Assert.Equal(9.99m, createResult.Value.ResolvePrice(80m));
    }

    [Fact]
    public void ShippingZone_Should_ApplyToConfiguredCountryCodes()
    {
        var createResult = ShippingZone.Create(
            "eu",
            "Europe",
            ["BG", "DE", "FR"],
            DateTime.UtcNow);

        Assert.True(createResult.IsSuccess);
        Assert.True(createResult.Value.AppliesToCountry("bg"));
        Assert.False(createResult.Value.AppliesToCountry("US"));
    }

    [Fact]
    public void ShippingMethod_Create_Should_FailForNegativeBasePrice()
    {
        var createResult = ShippingMethod.Create(
            "standard",
            "Standard Delivery",
            description: null,
            provider: "DemoCarrier",
            type: "Delivery",
            basePriceAmount: -1m,
            currency: "EUR",
            supportsTracking: true,
            supportsPickupPoint: false,
            estimatedMinDays: 2,
            estimatedMaxDays: 4,
            priority: 10,
            createdAtUtc: DateTime.UtcNow);

        Assert.True(createResult.IsFailure);
        Assert.Equal("shipping.method.base_price.invalid", createResult.Error.Code);
    }

    [Fact]
    public void Shipment_Should_AllowHappyPathTransitions_ToDelivered()
    {
        var shipment = CreateShipment();

        var labelResult = shipment.MarkLabelCreated(DateTime.UtcNow);
        var shippedResult = shipment.MarkShipped(DateTime.UtcNow);
        var inTransitResult = shipment.MarkInTransit(DateTime.UtcNow);
        var deliveredResult = shipment.MarkDelivered(DateTime.UtcNow);

        Assert.True(labelResult.IsSuccess);
        Assert.True(shippedResult.IsSuccess);
        Assert.True(inTransitResult.IsSuccess);
        Assert.True(deliveredResult.IsSuccess);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    [Fact]
    public void Shipment_Should_RejectInvalidTransition()
    {
        var shipment = CreateShipment();
        Assert.True(shipment.MarkShipped(DateTime.UtcNow).IsSuccess);
        Assert.True(shipment.MarkDelivered(DateTime.UtcNow).IsSuccess);

        var invalidTransition = shipment.MarkShipped(DateTime.UtcNow);

        Assert.True(invalidTransition.IsFailure);
        Assert.Equal("shipping.shipment.status.transition.invalid", invalidTransition.Error.Code);
    }

    [Fact]
    public void Shipment_Create_Should_Fail_WhenRecipientMissing()
    {
        var createResult = Shipment.Create(
            orderId: Guid.NewGuid(),
            shippingMethodId: Guid.NewGuid(),
            carrierName: "DemoCarrier",
            carrierServiceCode: "standard",
            recipientName: string.Empty,
            recipientPhone: null,
            addressSnapshotJson: "{\"street\":\"Main\"}",
            shippingPriceAmount: 5m,
            currency: "EUR",
            createdAtUtc: DateTime.UtcNow);

        Assert.True(createResult.IsFailure);
        Assert.Equal("shipping.shipment.recipient.required", createResult.Error.Code);
    }

    [Fact]
    public void Shipment_Should_AllowTransitionFromFailed_ToReturned()
    {
        var shipment = CreateShipment();
        Assert.True(shipment.MarkShipped(DateTime.UtcNow).IsSuccess);
        Assert.True(shipment.MarkInTransit(DateTime.UtcNow).IsSuccess);
        Assert.True(shipment.MarkFailed("Carrier issue", DateTime.UtcNow).IsSuccess);

        var returnedResult = shipment.MarkReturned("Returned to sender", DateTime.UtcNow);

        Assert.True(returnedResult.IsSuccess);
        Assert.Equal(ShipmentStatus.Returned, shipment.Status);
    }

    private static Shipment CreateShipment()
    {
        var createResult = Shipment.Create(
            orderId: Guid.NewGuid(),
            shippingMethodId: Guid.NewGuid(),
            carrierName: "DemoCarrier",
            carrierServiceCode: "standard",
            recipientName: "Alex Mercer",
            recipientPhone: "+359888000000",
            addressSnapshotJson: "{\"street\":\"Main\"}",
            shippingPriceAmount: 5m,
            currency: "EUR",
            createdAtUtc: DateTime.UtcNow);

        Assert.True(createResult.IsSuccess);
        return createResult.Value;
    }
}
