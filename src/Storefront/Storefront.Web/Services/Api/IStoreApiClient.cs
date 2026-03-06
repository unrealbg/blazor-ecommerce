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

    Task<StoreOrderSummary?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<StorePaymentIntentAction?> CreatePaymentIntentAsync(
        Guid orderId,
        string? provider,
        string idempotencyKey,
        string? customerEmail,
        CancellationToken cancellationToken);

    Task<StorePaymentIntentAction?> ConfirmPaymentIntentAsync(
        Guid paymentIntentId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<StorePaymentIntentAction?> CancelPaymentIntentAsync(
        Guid paymentIntentId,
        string? reason,
        CancellationToken cancellationToken);

    Task<StorePaymentIntentAction?> RefundPaymentIntentAsync(
        Guid paymentIntentId,
        decimal? amount,
        string? reason,
        CancellationToken cancellationToken);

    Task<StorePaymentIntentDetails?> GetPaymentIntentAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken);

    Task<StorePaymentIntentDetails?> GetPaymentIntentByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<StorePaymentIntentPage> GetPaymentIntentsAsync(
        int page,
        int pageSize,
        string? provider,
        string? status,
        CancellationToken cancellationToken);

    Task<StoreRedirectMatch?> ResolveRedirectAsync(string path, CancellationToken cancellationToken);

    Task<StoreRedirectRulePage> GetRedirectRulesAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<Guid?> CreateRedirectRuleAsync(
        string fromPath,
        string toPath,
        int statusCode,
        CancellationToken cancellationToken);

    Task<bool> DeactivateRedirectRuleAsync(Guid redirectRuleId, CancellationToken cancellationToken);

    Task<StoreInventoryProductDetails?> GetInventoryProductAsync(
        Guid productId,
        CancellationToken cancellationToken);

    Task<bool> AdjustInventoryStockAsync(
        Guid productId,
        int quantityDelta,
        string? reason,
        CancellationToken cancellationToken);

    Task<StoreStockMovementPage> GetInventoryMovementsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<StoreStockReservationPage> GetActiveInventoryReservationsAsync(
        Guid? productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreShippingQuoteMethod>> GetShippingQuotesAsync(
        string countryCode,
        decimal subtotalAmount,
        string currency,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreShippingMethod>> GetShippingMethodsAsync(
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreShippingZone>> GetShippingZonesAsync(
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreShippingRateRule>> GetShippingRateRulesAsync(
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<Guid?> CreateShippingMethodAsync(
        StoreShippingMethod request,
        CancellationToken cancellationToken);

    Task<bool> UpdateShippingMethodAsync(
        Guid shippingMethodId,
        StoreShippingMethod request,
        CancellationToken cancellationToken);

    Task<Guid?> CreateShippingZoneAsync(
        StoreShippingZone request,
        CancellationToken cancellationToken);

    Task<bool> UpdateShippingZoneAsync(
        Guid shippingZoneId,
        StoreShippingZone request,
        CancellationToken cancellationToken);

    Task<Guid?> CreateShippingRateRuleAsync(
        StoreShippingRateRule request,
        CancellationToken cancellationToken);

    Task<bool> UpdateShippingRateRuleAsync(
        Guid shippingRateRuleId,
        StoreShippingRateRule request,
        CancellationToken cancellationToken);

    Task<StoreShipmentPage> GetShipmentsAsync(
        string? status,
        Guid? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<StoreShipment?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<StoreShipment?> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Guid?> CreateShipmentAsync(
        Guid orderId,
        string? shippingMethodCode,
        CancellationToken cancellationToken);

    Task<bool> CreateShipmentLabelAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<bool> MarkShipmentShippedAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<bool> CancelShipmentAsync(
        Guid shipmentId,
        string? reason,
        CancellationToken cancellationToken);
}
