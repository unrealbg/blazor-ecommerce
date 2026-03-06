using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Customers.Domain.Customers.Events;

namespace Customers.Domain.Customers;

public sealed class Customer : AggregateRoot<Guid>
{
    private readonly List<Address> _addresses = [];

    private Customer()
    {
    }

    private Customer(
        Guid id,
        string email,
        string normalizedEmail,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        Guid? userId)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        UserId = userId;
        IsEmailVerified = userId is not null;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerRegistered(Id, UserId, Email));
    }

    public Guid? UserId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool IsEmailVerified { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    public static Result<Customer> CreateGuest(
        string email,
        string? firstName,
        string? lastName,
        string? phoneNumber)
    {
        return Create(email, firstName, lastName, phoneNumber, userId: null);
    }

    public static Result<Customer> CreateRegistered(
        string email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        Guid userId)
    {
        return Create(email, firstName, lastName, phoneNumber, userId);
    }

    public Result UpdateProfile(string? firstName, string? lastName, string? phoneNumber)
    {
        FirstName = NormalizeNullable(firstName);
        LastName = NormalizeNullable(lastName);
        PhoneNumber = NormalizeNullable(phoneNumber);
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result LinkUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(new Error("customers.user_id.invalid", "User id is invalid."));
        }

        if (UserId is not null && UserId != userId)
        {
            return Result.Failure(new Error("customers.user_id.conflict", "Customer is already linked to another user."));
        }

        UserId = userId;
        IsEmailVerified = true;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result<Guid> AddAddress(AddressData data)
    {
        var validation = ValidateAddressData(data);
        if (validation.IsFailure)
        {
            return Result<Guid>.Failure(validation.Error);
        }

        var address = Address.Create(Id, data);

        if (address.IsDefaultShipping)
        {
            SetSingleDefaultShipping(address.Id);
        }

        if (address.IsDefaultBilling)
        {
            SetSingleDefaultBilling(address.Id);
        }

        _addresses.Add(address);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerAddressAdded(Id, address.Id));

        return Result<Guid>.Success(address.Id);
    }

    public Result UpdateAddress(Guid addressId, AddressData data)
    {
        var validation = ValidateAddressData(data);
        if (validation.IsFailure)
        {
            return validation;
        }

        var address = _addresses.FirstOrDefault(item => item.Id == addressId);
        if (address is null)
        {
            return Result.Failure(new Error("customers.address.not_found", "Address was not found."));
        }

        address.Update(data);

        if (address.IsDefaultShipping)
        {
            SetSingleDefaultShipping(address.Id);
        }

        if (address.IsDefaultBilling)
        {
            SetSingleDefaultBilling(address.Id);
        }

        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerAddressUpdated(Id, address.Id));
        return Result.Success();
    }

    public Result DeleteAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(item => item.Id == addressId);
        if (address is null)
        {
            return Result.Failure(new Error("customers.address.not_found", "Address was not found."));
        }

        _addresses.Remove(address);
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void RecordLogin(Guid userId)
    {
        RaiseDomainEvent(new CustomerLoggedIn(Id, userId));
    }

    private static Result<Customer> Create(
        string email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        Guid? userId)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<Customer>.Failure(new Error("customers.email.required", "Email is required."));
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var canonicalEmail = email.Trim().ToLowerInvariant();

        if (canonicalEmail.Length > 320)
        {
            return Result<Customer>.Failure(new Error("customers.email.invalid", "Email is invalid."));
        }

        var customer = new Customer(
            Guid.NewGuid(),
            canonicalEmail,
            normalizedEmail,
            NormalizeNullable(firstName),
            NormalizeNullable(lastName),
            NormalizeNullable(phoneNumber),
            userId);

        return Result<Customer>.Success(customer);
    }

    private static Result ValidateAddressData(AddressData data)
    {
        if (string.IsNullOrWhiteSpace(data.Label))
        {
            return Result.Failure(new Error("customers.address.label.required", "Address label is required."));
        }

        if (string.IsNullOrWhiteSpace(data.FirstName) || string.IsNullOrWhiteSpace(data.LastName))
        {
            return Result.Failure(new Error("customers.address.name.required", "Address first and last names are required."));
        }

        if (string.IsNullOrWhiteSpace(data.Street1))
        {
            return Result.Failure(new Error("customers.address.street.required", "Street is required."));
        }

        if (string.IsNullOrWhiteSpace(data.City))
        {
            return Result.Failure(new Error("customers.address.city.required", "City is required."));
        }

        if (string.IsNullOrWhiteSpace(data.PostalCode))
        {
            return Result.Failure(new Error("customers.address.postal_code.required", "Postal code is required."));
        }

        if (string.IsNullOrWhiteSpace(data.CountryCode))
        {
            return Result.Failure(new Error("customers.address.country.required", "Country code is required."));
        }

        return Result.Success();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void SetSingleDefaultShipping(Guid activeAddressId)
    {
        foreach (var address in _addresses)
        {
            address.MarkDefaultShipping(address.Id == activeAddressId);
        }
    }

    private void SetSingleDefaultBilling(Guid activeAddressId)
    {
        foreach (var address in _addresses)
        {
            address.MarkDefaultBilling(address.Id == activeAddressId);
        }
    }
}
