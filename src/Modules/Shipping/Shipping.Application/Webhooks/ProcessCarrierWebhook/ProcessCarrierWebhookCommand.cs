using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Webhooks.ProcessCarrierWebhook;

public sealed record ProcessCarrierWebhookCommand(
    string Provider,
    string Payload) : ICommand<bool>;
