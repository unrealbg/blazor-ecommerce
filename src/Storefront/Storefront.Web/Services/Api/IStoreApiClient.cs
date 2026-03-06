namespace Storefront.Web.Services.Api;

public interface IStoreApiClient
{
    Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken);

    Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken);

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

    Task<StoreRedirectMatch?> ResolveRedirectAsync(string path, CancellationToken cancellationToken);

    Task<StoreRedirectRulePage> GetRedirectRulesAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Guid?> CreateRedirectRuleAsync(
        string fromPath,
        string toPath,
        int statusCode,
        CancellationToken cancellationToken);

    Task<bool> DeactivateRedirectRuleAsync(Guid redirectRuleId, CancellationToken cancellationToken);
}
