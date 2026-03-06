using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Payments.Domain.Events;

namespace Payments.Domain.Payments;

public sealed class PaymentIntent : AggregateRoot<Guid>
{
    private PaymentIntent()
    {
    }

    private PaymentIntent(
        Guid id,
        Guid orderId,
        Guid? customerId,
        string provider,
        decimal amount,
        string currency,
        string? idempotencyKey,
        DateTime createdAtUtc)
    {
        Id = id;
        OrderId = orderId;
        CustomerId = customerId;
        Provider = provider;
        Amount = amount;
        Currency = currency;
        Status = PaymentIntentStatus.Created;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = 0;

        RaiseDomainEvent(new PaymentIntentCreated(Id, OrderId, Provider, Amount, Currency));
    }

    public Guid OrderId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string? ProviderPaymentIntentId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public PaymentIntentStatus Status { get; private set; }

    public string? ClientSecret { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureMessage { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public long RowVersion { get; private set; }

    public bool IsActive => Status is
        PaymentIntentStatus.Created or
        PaymentIntentStatus.Pending or
        PaymentIntentStatus.RequiresAction or
        PaymentIntentStatus.Authorized;

    public static Result<PaymentIntent> Create(
        Guid orderId,
        Guid? customerId,
        string provider,
        decimal amount,
        string currency,
        string? idempotencyKey,
        DateTime createdAtUtc)
    {
        if (orderId == Guid.Empty)
        {
            return Result<PaymentIntent>.Failure(new Error(
                "payments.order.required",
                "Order id is required."));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result<PaymentIntent>.Failure(new Error(
                "payments.provider.required",
                "Payment provider is required."));
        }

        if (amount <= 0m)
        {
            return Result<PaymentIntent>.Failure(new Error(
                "payments.amount.invalid",
                "Payment amount must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result<PaymentIntent>.Failure(new Error(
                "payments.currency.invalid",
                "Currency must be a 3-letter code."));
        }

        return Result<PaymentIntent>.Success(new PaymentIntent(
            Guid.NewGuid(),
            orderId,
            customerId,
            provider.Trim(),
            amount,
            currency.Trim().ToUpperInvariant(),
            idempotencyKey,
            createdAtUtc));
    }

    public Result ApplyProviderCreation(
        string? providerPaymentIntentId,
        string? clientSecret,
        PaymentIntentStatus status,
        string? failureCode,
        string? failureMessage,
        DateTime utcNow)
    {
        if (!string.IsNullOrWhiteSpace(providerPaymentIntentId))
        {
            ProviderPaymentIntentId = providerPaymentIntentId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            ClientSecret = clientSecret.Trim();
        }

        return TransitionTo(status, failureCode, failureMessage, utcNow);
    }

    public Result ApplyProviderConfirmation(
        PaymentIntentStatus status,
        string? failureCode,
        string? failureMessage,
        DateTime utcNow)
    {
        return TransitionTo(status, failureCode, failureMessage, utcNow);
    }

    public Result ApplyProviderCancellation(string? reason, DateTime utcNow)
    {
        return TransitionTo(PaymentIntentStatus.Cancelled, "cancelled", reason, utcNow);
    }

    public Result ApplyProviderRefund(bool partial, DateTime utcNow)
    {
        return TransitionTo(
            partial ? PaymentIntentStatus.PartiallyRefunded : PaymentIntentStatus.Refunded,
            null,
            null,
            utcNow);
    }

    private Result TransitionTo(
        PaymentIntentStatus nextStatus,
        string? failureCode,
        string? failureMessage,
        DateTime utcNow)
    {
        if (Status == nextStatus)
        {
            FailureCode = string.IsNullOrWhiteSpace(failureCode) ? FailureCode : failureCode.Trim();
            FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? FailureMessage : failureMessage.Trim();
            Touch(utcNow);
            return Result.Success();
        }

        if (!IsValidTransition(Status, nextStatus))
        {
            return Result.Failure(new Error(
                "payments.status.transition.invalid",
                $"Cannot move payment intent from '{Status}' to '{nextStatus}'."));
        }

        Status = nextStatus;
        FailureCode = string.IsNullOrWhiteSpace(failureCode) ? null : failureCode.Trim();
        FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim();

        if (nextStatus is
            PaymentIntentStatus.Captured or
            PaymentIntentStatus.Failed or
            PaymentIntentStatus.Cancelled or
            PaymentIntentStatus.Refunded or
            PaymentIntentStatus.PartiallyRefunded)
        {
            CompletedAtUtc ??= utcNow;
        }

        Touch(utcNow);
        RaiseTransitionDomainEvent(nextStatus);

        return Result.Success();
    }

    private bool IsValidTransition(PaymentIntentStatus current, PaymentIntentStatus next)
    {
        return current switch
        {
            PaymentIntentStatus.Created => next is
                PaymentIntentStatus.Pending or
                PaymentIntentStatus.RequiresAction or
                PaymentIntentStatus.Authorized or
                PaymentIntentStatus.Captured or
                PaymentIntentStatus.Failed or
                PaymentIntentStatus.Cancelled,
            PaymentIntentStatus.Pending => next is
                PaymentIntentStatus.RequiresAction or
                PaymentIntentStatus.Authorized or
                PaymentIntentStatus.Captured or
                PaymentIntentStatus.Failed or
                PaymentIntentStatus.Cancelled,
            PaymentIntentStatus.RequiresAction => next is
                PaymentIntentStatus.Pending or
                PaymentIntentStatus.Authorized or
                PaymentIntentStatus.Captured or
                PaymentIntentStatus.Failed or
                PaymentIntentStatus.Cancelled,
            PaymentIntentStatus.Authorized => next is
                PaymentIntentStatus.Captured or
                PaymentIntentStatus.Failed or
                PaymentIntentStatus.Cancelled,
            PaymentIntentStatus.Captured => next is
                PaymentIntentStatus.Refunded or
                PaymentIntentStatus.PartiallyRefunded,
            PaymentIntentStatus.PartiallyRefunded => next == PaymentIntentStatus.Refunded,
            _ => false,
        };
    }

    private void RaiseTransitionDomainEvent(PaymentIntentStatus status)
    {
        switch (status)
        {
            case PaymentIntentStatus.Pending:
                RaiseDomainEvent(new PaymentPending(Id, OrderId, Provider));
                break;
            case PaymentIntentStatus.RequiresAction:
                RaiseDomainEvent(new PaymentRequiresAction(Id, OrderId, Provider));
                break;
            case PaymentIntentStatus.Authorized:
                RaiseDomainEvent(new PaymentAuthorized(Id, OrderId, Provider));
                break;
            case PaymentIntentStatus.Captured:
                RaiseDomainEvent(new PaymentCaptured(Id, OrderId, Provider, Amount, Currency));
                break;
            case PaymentIntentStatus.Failed:
                RaiseDomainEvent(new PaymentFailed(Id, OrderId, Provider, FailureCode, FailureMessage));
                break;
            case PaymentIntentStatus.Cancelled:
                RaiseDomainEvent(new PaymentCancelled(Id, OrderId, Provider, FailureMessage));
                break;
            case PaymentIntentStatus.Refunded:
                RaiseDomainEvent(new PaymentRefunded(Id, OrderId, Provider, Amount, Currency));
                break;
            case PaymentIntentStatus.PartiallyRefunded:
                RaiseDomainEvent(new PaymentPartiallyRefunded(Id, OrderId, Provider, Amount, Currency));
                break;
        }
    }

    private void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        RowVersion++;
    }
}
