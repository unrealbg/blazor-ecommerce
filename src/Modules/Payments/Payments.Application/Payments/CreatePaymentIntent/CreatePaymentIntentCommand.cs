using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.CreatePaymentIntent;

public sealed record CreatePaymentIntentCommand(
    Guid OrderId,
    string? Provider,
    string IdempotencyKey,
    string? CustomerEmail) : ICommand<PaymentIntentActionResult>;
