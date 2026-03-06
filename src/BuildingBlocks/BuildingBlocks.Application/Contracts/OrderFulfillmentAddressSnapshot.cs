namespace BuildingBlocks.Application.Contracts;

public sealed record OrderFulfillmentAddressSnapshot(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone);
