using System.Text.Json;
using Microsoft.Extensions.Options;
using Payments.Application.Payments;
using Payments.Application.Providers;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Providers;

internal sealed class DemoPaymentProvider(
    IOptions<DemoPaymentProviderOptions> options)
    : IPaymentProvider
{
    private readonly DemoPaymentProviderOptions options = options.Value;

    public string Name => "Demo";

    public Task<PaymentProviderCreateResponse> CreatePaymentIntentAsync(
        PaymentProviderCreateRequest request,
        CancellationToken cancellationToken)
    {
        var providerPaymentIntentId = $"demo_pi_{Guid.NewGuid():N}";
        var clientSecret = $"demo_cs_{Guid.NewGuid():N}";
        var providerTransactionId = $"demo_tx_{Guid.NewGuid():N}";

        if (ShouldFail())
        {
            return Task.FromResult(new PaymentProviderCreateResponse(
                providerPaymentIntentId,
                clientSecret,
                PaymentIntentStatus.Failed,
                RequiresAction: false,
                RedirectUrl: null,
                FailureCode: "demo_failure",
                FailureMessage: "Demo provider simulated a failure.",
                providerTransactionId,
                RawReference: providerPaymentIntentId,
                MetadataJson: SerializeMetadata(request.Metadata)));
        }

        if (options.SimulateRequiresAction)
        {
            return Task.FromResult(new PaymentProviderCreateResponse(
                providerPaymentIntentId,
                clientSecret,
                PaymentIntentStatus.RequiresAction,
                RequiresAction: true,
                RedirectUrl: $"/checkout/payment?action=demo&pi={request.PaymentIntentId:D}",
                FailureCode: null,
                FailureMessage: null,
                providerTransactionId,
                RawReference: providerPaymentIntentId,
                MetadataJson: SerializeMetadata(request.Metadata)));
        }

        var status = options.AutoCaptureOnCreate ? PaymentIntentStatus.Captured : PaymentIntentStatus.Pending;

        return Task.FromResult(new PaymentProviderCreateResponse(
            providerPaymentIntentId,
            clientSecret,
            status,
            RequiresAction: false,
            RedirectUrl: null,
            FailureCode: null,
            FailureMessage: null,
            providerTransactionId,
            RawReference: providerPaymentIntentId,
            MetadataJson: SerializeMetadata(request.Metadata)));
    }

    public Task<PaymentProviderConfirmResponse> ConfirmPaymentAsync(
        PaymentProviderConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var providerTransactionId = $"demo_tx_{Guid.NewGuid():N}";

        if (ShouldFail())
        {
            return Task.FromResult(new PaymentProviderConfirmResponse(
                PaymentIntentStatus.Failed,
                RequiresAction: false,
                RedirectUrl: null,
                FailureCode: "demo_failure",
                FailureMessage: "Demo provider simulated a confirmation failure.",
                providerTransactionId,
                RawReference: request.ProviderPaymentIntentId,
                MetadataJson: SerializeMetadata(request.Metadata)));
        }

        if (options.SimulateRequiresAction)
        {
            return Task.FromResult(new PaymentProviderConfirmResponse(
                PaymentIntentStatus.RequiresAction,
                RequiresAction: true,
                RedirectUrl: $"/checkout/payment?action=demo&pi={request.PaymentIntentId:D}",
                FailureCode: null,
                FailureMessage: null,
                providerTransactionId,
                RawReference: request.ProviderPaymentIntentId,
                MetadataJson: SerializeMetadata(request.Metadata)));
        }

        return Task.FromResult(new PaymentProviderConfirmResponse(
            PaymentIntentStatus.Captured,
            RequiresAction: false,
            RedirectUrl: null,
            FailureCode: null,
            FailureMessage: null,
            providerTransactionId,
            RawReference: request.ProviderPaymentIntentId,
            MetadataJson: SerializeMetadata(request.Metadata)));
    }

    public Task<PaymentProviderCancelResponse> CancelPaymentAsync(
        PaymentProviderCancelRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new PaymentProviderCancelResponse(
            PaymentIntentStatus.Cancelled,
            FailureCode: "cancelled",
            FailureMessage: request.Reason,
            ProviderTransactionId: $"demo_tx_{Guid.NewGuid():N}",
            RawReference: request.ProviderPaymentIntentId,
            MetadataJson: SerializeMetadata(request.Metadata)));
    }

    public Task<PaymentProviderRefundResponse> RefundPaymentAsync(
        PaymentProviderRefundRequest request,
        CancellationToken cancellationToken)
    {
        var refundAmount = request.RefundAmount ?? request.OriginalAmount;
        var isPartial = refundAmount < request.OriginalAmount;

        return Task.FromResult(new PaymentProviderRefundResponse(
            isPartial ? PaymentIntentStatus.PartiallyRefunded : PaymentIntentStatus.Refunded,
            refundAmount,
            isPartial,
            ProviderTransactionId: $"demo_rf_{Guid.NewGuid():N}",
            RawReference: request.ProviderPaymentIntentId,
            MetadataJson: SerializeMetadata(request.Metadata),
            FailureCode: null,
            FailureMessage: null));
    }

    public Task<PaymentProviderWebhookResult> ParseWebhookAsync(string payload, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var eventId = ReadString(root, "eventId")
            ?? ReadString(root, "id")
            ?? $"demo_evt_{Guid.NewGuid():N}";

        var eventType = ReadString(root, "eventType")
            ?? ReadString(root, "type")
            ?? "payment.unknown";

        var providerPaymentIntentId = ReadString(root, "providerPaymentIntentId")
            ?? ReadString(root, "paymentIntentId")
            ?? throw new InvalidOperationException("providerPaymentIntentId is required in demo webhook payload.");

        var status = ResolveWebhookStatus(eventType, ReadString(root, "status"));
        var amount = ReadDecimal(root, "amount");
        var currency = ReadString(root, "currency");
        var providerTransactionId = ReadString(root, "providerTransactionId") ?? ReadString(root, "transactionId");
        var failureCode = ReadString(root, "failureCode");
        var failureMessage = ReadString(root, "failureMessage");
        var isPartial = ReadBoolean(root, "isPartial") ?? false;

        return Task.FromResult(new PaymentProviderWebhookResult(
            eventId,
            eventType,
            providerPaymentIntentId,
            status,
            amount,
            currency,
            providerTransactionId,
            RawReference: providerPaymentIntentId,
            MetadataJson: payload,
            failureCode,
            failureMessage,
            IsPartialRefund: isPartial));
    }

    private bool ShouldFail()
    {
        var failureRate = decimal.Clamp(options.SimulateFailureRate, 0m, 1m);
        if (failureRate <= 0m)
        {
            return false;
        }

        return Convert.ToDecimal(Random.Shared.NextDouble()) < failureRate;
    }

    private string? SerializeMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        return metadata.Count == 0 ? null : JsonSerializer.Serialize(metadata);
    }

    private PaymentIntentStatus ResolveWebhookStatus(string eventType, string? statusValue)
    {
        if (eventType.Equals("payment.succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentIntentStatus.Captured;
        }

        if (eventType.Equals("payment.failed", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentIntentStatus.Failed;
        }

        if (eventType.Equals("payment.cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentIntentStatus.Cancelled;
        }

        if (eventType.Equals("payment.refunded", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentIntentStatus.Refunded;
        }

        if (eventType.Equals("payment.requires_action", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentIntentStatus.RequiresAction;
        }

        if (eventType.Equals("payment.pending", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentIntentStatus.Pending;
        }

        if (eventType.Equals("payment.authorized", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentIntentStatus.Authorized;
        }

        if (Enum.TryParse<PaymentIntentStatus>(statusValue, ignoreCase: true, out var parsedStatus))
        {
            return parsedStatus;
        }

        return PaymentIntentStatus.Pending;
    }

    private string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private decimal? ReadDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var numberValue))
        {
            return numberValue;
        }

        if (property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out var stringValue))
        {
            return stringValue;
        }

        return null;
    }

    private bool? ReadBoolean(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var booleanValue))
        {
            return booleanValue;
        }

        return null;
    }
}
