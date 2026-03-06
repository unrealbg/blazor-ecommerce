namespace Storefront.Web.Services.Api;

public interface IStoreApiClient
{
    Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken);

    Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<StoreSearchProductsResponse> SearchProductsAsync(
        StoreSearchProductsRequest request,
        CancellationToken cancellationToken);

    Task<StoreSearchSuggestionsResponse> SuggestProductsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);

    Task<StoreCart?> GetCartAsync(string customerId, CancellationToken cancellationToken);

    Task<bool> AddItemToCartAsync(
        string customerId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken);

    Task<bool> UpdateCartItemQuantityAsync(
        string customerId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken);

    Task<bool> RemoveCartItemAsync(
        string customerId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<Guid?> CheckoutAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken);

    Task<Guid?> CheckoutAsync(
        StoreCheckoutRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<StoreAuthResponse?> RegisterAsync(
        string email,
        string password,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        CancellationToken cancellationToken);

    Task<StoreAuthResponse?> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken);

    Task<bool> LogoutAsync(CancellationToken cancellationToken);

    Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken);

    Task<bool> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken);

    Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken);

    Task<StoreCustomerProfile?> GetCurrentCustomerAsync(CancellationToken cancellationToken);

    Task<bool> UpdateCurrentCustomerAsync(
        StoreUpdateProfileRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreCustomerAddress>> GetCurrentCustomerAddressesAsync(CancellationToken cancellationToken);

    Task<Guid?> AddCurrentCustomerAddressAsync(
        StoreAddressRequest request,
        CancellationToken cancellationToken);

    Task<bool> UpdateCurrentCustomerAddressAsync(
        Guid addressId,
        StoreAddressRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteCurrentCustomerAddressAsync(Guid addressId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreOrderSummary>> GetMyOrdersAsync(CancellationToken cancellationToken);

    Task<StoreRedirectMatch?> ResolveRedirectAsync(string path, CancellationToken cancellationToken);

    Task<StoreRedirectRulePage> GetRedirectRulesAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Guid?> CreateRedirectRuleAsync(
        string fromPath,
        string toPath,
        int statusCode,
        CancellationToken cancellationToken);

    Task<bool> DeactivateRedirectRuleAsync(Guid redirectRuleId, CancellationToken cancellationToken);
}
