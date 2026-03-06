using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.CancelPaymentIntent;

public sealed record CancelPaymentIntentCommand(
    Guid PaymentIntentId,
    string? Reason) : ICommand<PaymentIntentActionResult>;
