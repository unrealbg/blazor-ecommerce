using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.ConfirmPaymentIntent;

public sealed record ConfirmPaymentIntentCommand(
    Guid PaymentIntentId,
    string IdempotencyKey) : ICommand<PaymentIntentActionResult>;
