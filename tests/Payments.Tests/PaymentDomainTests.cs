using Payments.Domain.Payments;

namespace Payments.Tests;

public sealed class PaymentDomainTests
{
    [Fact]
    public void PaymentIntentCreate_ShouldFail_WhenOrderIdIsEmpty()
    {
        var result = PaymentIntent.Create(
            Guid.Empty,
            customerId: null,
            provider: "Demo",
            amount: 15m,
            currency: "EUR",
            idempotencyKey: "key-1",
            createdAtUtc: DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("payments.order.required", result.Error.Code);
    }

    [Fact]
    public void PaymentIntentCreate_ShouldFail_WhenAmountIsNonPositive()
    {
        var result = PaymentIntent.Create(
            Guid.NewGuid(),
            customerId: null,
            provider: "Demo",
            amount: 0m,
            currency: "EUR",
            idempotencyKey: "key-1",
            createdAtUtc: DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("payments.amount.invalid", result.Error.Code);
    }

    [Fact]
    public void PaymentIntentCreate_ShouldNormalizeCurrencyAndIdempotencyKey()
    {
        var result = PaymentIntent.Create(
            Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            provider: "Demo",
            amount: 25m,
            currency: "eur",
            idempotencyKey: "  key-123  ",
            createdAtUtc: DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value.Currency);
        Assert.Equal("key-123", result.Value.IdempotencyKey);
        Assert.Equal(PaymentIntentStatus.Created, result.Value.Status);
    }

    [Fact]
    public void ApplyProviderCreation_ShouldSetProviderReferences()
    {
        var intent = CreatePaymentIntent();
        var now = DateTime.UtcNow;

        var result = intent.ApplyProviderCreation(
            providerPaymentIntentId: "demo_pi_1",
            clientSecret: "demo_cs_1",
            status: PaymentIntentStatus.Pending,
            failureCode: null,
            failureMessage: null,
            utcNow: now);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentIntentStatus.Pending, intent.Status);
        Assert.Equal("demo_pi_1", intent.ProviderPaymentIntentId);
        Assert.Equal("demo_cs_1", intent.ClientSecret);
        Assert.Null(intent.CompletedAtUtc);
    }

    [Fact]
    public void ApplyProviderCreation_ShouldSetCompletedAt_WhenCaptured()
    {
        var intent = CreatePaymentIntent();
        var now = DateTime.UtcNow;

        var result = intent.ApplyProviderCreation(
            providerPaymentIntentId: "demo_pi_2",
            clientSecret: "demo_cs_2",
            status: PaymentIntentStatus.Captured,
            failureCode: null,
            failureMessage: null,
            utcNow: now);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentIntentStatus.Captured, intent.Status);
        Assert.Equal(now, intent.CompletedAtUtc);
    }

    [Fact]
    public void ApplyProviderRefund_ShouldFail_FromCreatedStatus()
    {
        var intent = CreatePaymentIntent();

        var result = intent.ApplyProviderRefund(partial: false, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("payments.status.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void Transition_ShouldAllow_PendingToRequiresActionToCaptured()
    {
        var intent = CreatePaymentIntent();
        var now = DateTime.UtcNow;

        var pendingResult = intent.ApplyProviderCreation(
            providerPaymentIntentId: "demo_pi_3",
            clientSecret: "demo_cs_3",
            status: PaymentIntentStatus.Pending,
            failureCode: null,
            failureMessage: null,
            utcNow: now);

        var actionResult = intent.ApplyProviderConfirmation(
            PaymentIntentStatus.RequiresAction,
            failureCode: null,
            failureMessage: null,
            utcNow: now.AddSeconds(1));

        var captureResult = intent.ApplyProviderConfirmation(
            PaymentIntentStatus.Captured,
            failureCode: null,
            failureMessage: null,
            utcNow: now.AddSeconds(2));

        Assert.True(pendingResult.IsSuccess);
        Assert.True(actionResult.IsSuccess);
        Assert.True(captureResult.IsSuccess);
        Assert.Equal(PaymentIntentStatus.Captured, intent.Status);
    }

    [Fact]
    public void Transition_ShouldFail_FromCapturedToFailed()
    {
        var intent = CreateCapturedIntent();

        var result = intent.ApplyProviderConfirmation(
            PaymentIntentStatus.Failed,
            failureCode: "declined",
            failureMessage: "Provider decline",
            utcNow: DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("payments.status.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void Transition_ShouldAllow_CapturedToPartialRefundToRefunded()
    {
        var intent = CreateCapturedIntent();

        var partialResult = intent.ApplyProviderRefund(partial: true, DateTime.UtcNow);
        var finalResult = intent.ApplyProviderRefund(partial: false, DateTime.UtcNow.AddSeconds(1));

        Assert.True(partialResult.IsSuccess);
        Assert.True(finalResult.IsSuccess);
        Assert.Equal(PaymentIntentStatus.Refunded, intent.Status);
    }

    [Fact]
    public void WebhookInboxMessageCreate_ShouldFail_WhenProviderIsMissing()
    {
        var result = WebhookInboxMessage.Create(
            provider: string.Empty,
            externalEventId: "evt_1",
            eventType: "payment.succeeded",
            payload: "{}",
            receivedAtUtc: DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("payments.webhook.provider.required", result.Error.Code);
    }

    [Fact]
    public void WebhookInboxMessageMarkProcessed_ShouldSetProcessedState()
    {
        var result = WebhookInboxMessage.Create(
            provider: "Demo",
            externalEventId: "evt_2",
            eventType: "payment.succeeded",
            payload: "{}",
            receivedAtUtc: DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        var message = result.Value;

        var processedAtUtc = DateTime.UtcNow.AddSeconds(5);
        message.MarkProcessed(processedAtUtc);

        Assert.Equal(WebhookInboxProcessingStatus.Processed, message.ProcessingStatus);
        Assert.Equal(processedAtUtc, message.ProcessedAtUtc);
        Assert.Null(message.Error);
    }

    [Fact]
    public void PaymentIdempotencyRecordCreate_ShouldFail_WhenIdempotencyKeyIsMissing()
    {
        var result = PaymentIdempotencyRecord.Create(
            operation: "create-intent",
            idempotencyKey: " ",
            paymentIntentId: Guid.NewGuid(),
            createdAtUtc: DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("payments.idempotency.key.required", result.Error.Code);
    }

    [Fact]
    public void PaymentTransactionCreate_ShouldNormalizeCurrencyAndMetadata()
    {
        var result = PaymentTransaction.Create(
            paymentIntentId: Guid.NewGuid(),
            type: PaymentTransactionType.Capture,
            providerTransactionId: "tx_1",
            amount: 55.35m,
            currency: "eur",
            status: "Captured",
            rawReference: "ref_1",
            metadataJson: " {\"source\":\"demo\"} ",
            createdAtUtc: DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value.Currency);
        Assert.Equal("{\"source\":\"demo\"}", result.Value.MetadataJson);
    }

    private static PaymentIntent CreatePaymentIntent()
    {
        var result = PaymentIntent.Create(
            Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            provider: "Demo",
            amount: 42m,
            currency: "EUR",
            idempotencyKey: "k",
            createdAtUtc: DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static PaymentIntent CreateCapturedIntent()
    {
        var intent = CreatePaymentIntent();
        var createResult = intent.ApplyProviderCreation(
            providerPaymentIntentId: "demo_pi_captured",
            clientSecret: "demo_cs_captured",
            status: PaymentIntentStatus.Captured,
            failureCode: null,
            failureMessage: null,
            utcNow: DateTime.UtcNow);

        Assert.True(createResult.IsSuccess);
        return intent;
    }
}
