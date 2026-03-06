using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Customers;

public sealed record DeleteAddressCommand(Guid UserId, Guid AddressId) : ICommand<bool>;
