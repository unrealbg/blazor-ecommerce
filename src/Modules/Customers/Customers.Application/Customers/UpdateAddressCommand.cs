using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Customers;

public sealed record UpdateAddressCommand(
    Guid UserId,
    Guid AddressId,
    AddressInput Address) : ICommand<bool>;
