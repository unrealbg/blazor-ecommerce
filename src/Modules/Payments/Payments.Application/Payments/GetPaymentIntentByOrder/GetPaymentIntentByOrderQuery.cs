using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.GetPaymentIntentByOrder;

public sealed record GetPaymentIntentByOrderQuery(Guid OrderId) : IQuery<PaymentIntentDetailsDto?>;
