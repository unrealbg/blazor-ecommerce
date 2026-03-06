using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Customers;

public sealed record AddAddressCommand(Guid UserId, AddressInput Address) : ICommand<Guid>;
