using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Webhooks.ProcessWebhook;

public sealed record ProcessWebhookCommand(string Provider, string Payload) : ICommand<bool>;
