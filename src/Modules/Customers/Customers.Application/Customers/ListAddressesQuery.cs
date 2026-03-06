using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Customers;

public sealed record ListAddressesQuery(Guid UserId) : IQuery<IReadOnlyCollection<AddressDto>>;
