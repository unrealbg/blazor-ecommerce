namespace Storefront.Web.Services.Api;

public sealed record StoreAuthResponse(Guid UserId, Guid CustomerId, string Email);
