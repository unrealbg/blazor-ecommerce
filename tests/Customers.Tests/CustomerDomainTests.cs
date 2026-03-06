using Customers.Domain.Customers;

namespace Customers.Tests;

public sealed class CustomerDomainTests
{
    [Fact]
    public void CreateGuest_Should_NormalizeEmail()
    {
        var result = Customer.CreateGuest(" User@Example.com ", "John", "Doe", null);

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.Equal("USER@EXAMPLE.COM", result.Value.NormalizedEmail);
    }

    [Fact]
    public void AddAddress_Should_KeepSingleDefaultShippingAndBilling()
    {
        var customer = CreateCustomer();

        var firstAddressResult = customer.AddAddress(new AddressData(
            "Home",
            "John",
            "Doe",
            null,
            "Street 1",
            null,
            "Sofia",
            "1000",
            "BG",
            null,
            true,
            true));

        Assert.True(firstAddressResult.IsSuccess);

        var secondAddressResult = customer.AddAddress(new AddressData(
            "Office",
            "John",
            "Doe",
            "Contoso",
            "Street 2",
            null,
            "Sofia",
            "1000",
            "BG",
            null,
            true,
            true));

        Assert.True(secondAddressResult.IsSuccess);

        var defaults = customer.Addresses.Where(address => address.IsDefaultShipping || address.IsDefaultBilling).ToArray();
        Assert.Single(defaults, address => address.IsDefaultShipping);
        Assert.Single(defaults, address => address.IsDefaultBilling);
        Assert.Equal(secondAddressResult.Value, defaults.Single(address => address.IsDefaultShipping).Id);
        Assert.Equal(secondAddressResult.Value, defaults.Single(address => address.IsDefaultBilling).Id);
    }

    [Fact]
    public void UpdateAddress_Should_SwitchDefaultFlags()
    {
        var customer = CreateCustomer();

        var homeAddressId = customer.AddAddress(new AddressData(
            "Home",
            "John",
            "Doe",
            null,
            "Street 1",
            null,
            "Sofia",
            "1000",
            "BG",
            null,
            true,
            false)).Value;

        var officeAddressId = customer.AddAddress(new AddressData(
            "Office",
            "John",
            "Doe",
            null,
            "Street 2",
            null,
            "Sofia",
            "1000",
            "BG",
            null,
            false,
            true)).Value;

        var updateResult = customer.UpdateAddress(
            homeAddressId,
            new AddressData(
                "Home",
                "John",
                "Doe",
                null,
                "Street 1",
                null,
                "Sofia",
                "1000",
                "BG",
                null,
                true,
                true));

        Assert.True(updateResult.IsSuccess);
        Assert.Equal(homeAddressId, customer.Addresses.Single(address => address.IsDefaultShipping).Id);
        Assert.Equal(homeAddressId, customer.Addresses.Single(address => address.IsDefaultBilling).Id);
        Assert.DoesNotContain(customer.Addresses, address => address.Id == officeAddressId && address.IsDefaultBilling);
    }

    [Fact]
    public void DeleteAddress_Should_RemoveAddress()
    {
        var customer = CreateCustomer();
        var addressId = customer.AddAddress(new AddressData(
            "Home",
            "John",
            "Doe",
            null,
            "Street 1",
            null,
            "Sofia",
            "1000",
            "BG",
            null,
            false,
            false)).Value;

        var removeResult = customer.DeleteAddress(addressId);

        Assert.True(removeResult.IsSuccess);
        Assert.DoesNotContain(customer.Addresses, address => address.Id == addressId);
    }

    private static Customer CreateCustomer()
    {
        return Customer.CreateGuest("customer@example.com", "John", "Doe", null).Value;
    }
}
