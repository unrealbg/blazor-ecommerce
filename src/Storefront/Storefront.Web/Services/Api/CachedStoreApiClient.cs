using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Storefront.Web.Configuration;
using Storefront.Web.Services.Caching;

namespace Storefront.Web.Services.Api;

public sealed class CachedStoreApiClient(
    StoreApiClient inner,
    IDistributedCache distributedCache,
    IOptions<StorefrontCacheOptions> cacheOptions,
    IOptions<StorefrontFeatureFlagsOptions> featureFlags,
    ILogger<CachedStoreApiClient> logger)
    : IStoreApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly StorefrontCacheOptions cacheOptions = cacheOptions.Value;
    private readonly StorefrontFeatureFlagsOptions featureFlags = featureFlags.Value;

    public Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken)
    {
        return GetOrSetAsync(
            StorefrontCacheKeyFactory.Products(),
            () => inner.GetProductsAsync(cancellationToken),
            TimeSpan.FromSeconds(Math.Max(5, cacheOptions.HomePageSeconds)),
            cancellationToken);
    }

    public Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return GetOrSetAsync(
            StorefrontCacheKeyFactory.Product(slug),
            () => inner.GetProductBySlugAsync(slug, cancellationToken),
            TimeSpan.FromSeconds(Math.Max(5, cacheOptions.ProductProjectionSeconds)),
            cancellationToken);
    }

    public Task<StoreReviewSummary?> GetProductReviewSummaryAsync(Guid productId, CancellationToken cancellationToken)
    {
        return GetOrSetAsync(
            StorefrontCacheKeyFactory.ReviewSummary(productId),
            () => inner.GetProductReviewSummaryAsync(productId, cancellationToken),
            TimeSpan.FromSeconds(Math.Max(5, cacheOptions.ReviewSummarySeconds)),
            cancellationToken);
    }

    public Task<StoreReviewPage> GetProductReviewsAsync(Guid productId, int page, int pageSize, string? sort, int? rating, CancellationToken cancellationToken)
        => inner.GetProductReviewsAsync(productId, page, pageSize, sort, rating, cancellationToken);

    public Task<StoreQuestionPage> GetProductQuestionsAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken)
        => inner.GetProductQuestionsAsync(productId, page, pageSize, cancellationToken);

    public Task<StoreSearchProductsResponse> SearchProductsAsync(StoreSearchProductsRequest request, CancellationToken cancellationToken)
    {
        return GetOrSetAsync(
            StorefrontCacheKeyFactory.Search(request),
            () => inner.SearchProductsAsync(request, cancellationToken),
            TimeSpan.FromSeconds(Math.Max(5, cacheOptions.SearchResultSeconds)),
            cancellationToken);
    }

    public Task<StoreSearchSuggestionsResponse> SuggestProductsAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (!featureFlags.EnableSearchSuggestions)
        {
            return Task.FromResult(new StoreSearchSuggestionsResponse(query, []));
        }

        return GetOrSetAsync(
            StorefrontCacheKeyFactory.Suggest(query, limit),
            () => inner.SuggestProductsAsync(query, limit, cancellationToken),
            TimeSpan.FromSeconds(Math.Max(5, cacheOptions.SuggestionSeconds)),
            cancellationToken);
    }

    public Task<StoreCart?> GetCartAsync(string customerId, CancellationToken cancellationToken) => inner.GetCartAsync(customerId, cancellationToken);
    public Task<bool> AddItemToCartAsync(string customerId, Guid productId, int quantity, CancellationToken cancellationToken) => inner.AddItemToCartAsync(customerId, productId, quantity, cancellationToken);
    public Task<bool> AddItemToCartAsync(string customerId, Guid productId, Guid variantId, int quantity, CancellationToken cancellationToken) => inner.AddItemToCartAsync(customerId, productId, variantId, quantity, cancellationToken);
    public Task<bool> UpdateCartItemQuantityAsync(string customerId, Guid productId, int quantity, CancellationToken cancellationToken) => inner.UpdateCartItemQuantityAsync(customerId, productId, quantity, cancellationToken);
    public Task<bool> RemoveCartItemAsync(string customerId, Guid productId, CancellationToken cancellationToken) => inner.RemoveCartItemAsync(customerId, productId, cancellationToken);
    public Task<bool> ApplyCouponAsync(string customerId, string couponCode, CancellationToken cancellationToken) => inner.ApplyCouponAsync(customerId, couponCode, cancellationToken);
    public Task<bool> RemoveCouponAsync(string customerId, CancellationToken cancellationToken) => inner.RemoveCouponAsync(customerId, cancellationToken);
    public Task<Guid?> SubmitReviewAsync(Guid productId, StoreSubmitReviewRequest request, CancellationToken cancellationToken) => inner.SubmitReviewAsync(productId, request, cancellationToken);
    public Task<bool> UpdateMyReviewAsync(Guid reviewId, StoreSubmitReviewRequest request, CancellationToken cancellationToken) => inner.UpdateMyReviewAsync(reviewId, request, cancellationToken);
    public Task<StoreReviewVoteResult?> VoteReviewAsync(Guid reviewId, string voteType, CancellationToken cancellationToken) => inner.VoteReviewAsync(reviewId, voteType, cancellationToken);
    public Task<Guid?> ReportReviewAsync(Guid reviewId, string reasonType, string? message, CancellationToken cancellationToken) => inner.ReportReviewAsync(reviewId, reasonType, message, cancellationToken);
    public Task<Guid?> SubmitQuestionAsync(Guid productId, StoreSubmitQuestionRequest request, CancellationToken cancellationToken) => inner.SubmitQuestionAsync(productId, request, cancellationToken);
    public Task<Guid?> SubmitAnswerAsync(Guid questionId, StoreSubmitAnswerRequest request, CancellationToken cancellationToken) => inner.SubmitAnswerAsync(questionId, request, cancellationToken);
    public Task<IReadOnlyCollection<StoreMyReview>> GetMyReviewsAsync(CancellationToken cancellationToken) => inner.GetMyReviewsAsync(cancellationToken);
    public Task<IReadOnlyCollection<StoreMyQuestion>> GetMyQuestionsAsync(CancellationToken cancellationToken) => inner.GetMyQuestionsAsync(cancellationToken);
    public Task<Guid?> CheckoutAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken) => inner.CheckoutAsync(customerId, idempotencyKey, cancellationToken);
    public Task<Guid?> CheckoutAsync(StoreCheckoutRequest request, string idempotencyKey, CancellationToken cancellationToken) => inner.CheckoutAsync(request, idempotencyKey, cancellationToken);
    public Task<StoreAuthResponse?> RegisterAsync(string email, string password, string? firstName, string? lastName, string? phoneNumber, CancellationToken cancellationToken) => inner.RegisterAsync(email, password, firstName, lastName, phoneNumber, cancellationToken);
    public Task<StoreAuthResponse?> LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken) => inner.LoginAsync(email, password, rememberMe, cancellationToken);
    public Task<bool> LogoutAsync(CancellationToken cancellationToken) => inner.LogoutAsync(cancellationToken);
    public Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken) => inner.ForgotPasswordAsync(email, cancellationToken);
    public Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken) => inner.ResetPasswordAsync(email, token, newPassword, cancellationToken);
    public Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken) => inner.VerifyEmailAsync(userId, token, cancellationToken);
    public Task<StoreCustomerProfile?> GetCurrentCustomerAsync(CancellationToken cancellationToken) => inner.GetCurrentCustomerAsync(cancellationToken);
    public Task<bool> UpdateCurrentCustomerAsync(StoreUpdateProfileRequest request, CancellationToken cancellationToken) => inner.UpdateCurrentCustomerAsync(request, cancellationToken);
    public Task<IReadOnlyCollection<StoreCustomerAddress>> GetCurrentCustomerAddressesAsync(CancellationToken cancellationToken) => inner.GetCurrentCustomerAddressesAsync(cancellationToken);
    public Task<Guid?> AddCurrentCustomerAddressAsync(StoreAddressRequest request, CancellationToken cancellationToken) => inner.AddCurrentCustomerAddressAsync(request, cancellationToken);
    public Task<bool> UpdateCurrentCustomerAddressAsync(Guid addressId, StoreAddressRequest request, CancellationToken cancellationToken) => inner.UpdateCurrentCustomerAddressAsync(addressId, request, cancellationToken);
    public Task<bool> DeleteCurrentCustomerAddressAsync(Guid addressId, CancellationToken cancellationToken) => inner.DeleteCurrentCustomerAddressAsync(addressId, cancellationToken);
    public Task<IReadOnlyCollection<StoreOrderSummary>> GetMyOrdersAsync(CancellationToken cancellationToken) => inner.GetMyOrdersAsync(cancellationToken);
    public Task<StoreOrderSummary?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken) => inner.GetOrderAsync(orderId, cancellationToken);
    public Task<StorePaymentIntentAction?> CreatePaymentIntentAsync(Guid orderId, string? provider, string idempotencyKey, string? customerEmail, CancellationToken cancellationToken) => inner.CreatePaymentIntentAsync(orderId, provider, idempotencyKey, customerEmail, cancellationToken);
    public Task<StorePaymentIntentAction?> ConfirmPaymentIntentAsync(Guid paymentIntentId, string idempotencyKey, CancellationToken cancellationToken) => inner.ConfirmPaymentIntentAsync(paymentIntentId, idempotencyKey, cancellationToken);
    public Task<StorePaymentIntentAction?> CancelPaymentIntentAsync(Guid paymentIntentId, string? reason, CancellationToken cancellationToken) => inner.CancelPaymentIntentAsync(paymentIntentId, reason, cancellationToken);
    public Task<StorePaymentIntentAction?> RefundPaymentIntentAsync(Guid paymentIntentId, decimal? amount, string? reason, CancellationToken cancellationToken) => inner.RefundPaymentIntentAsync(paymentIntentId, amount, reason, cancellationToken);
    public Task<StorePaymentIntentDetails?> GetPaymentIntentAsync(Guid paymentIntentId, CancellationToken cancellationToken) => inner.GetPaymentIntentAsync(paymentIntentId, cancellationToken);
    public Task<StorePaymentIntentDetails?> GetPaymentIntentByOrderAsync(Guid orderId, CancellationToken cancellationToken) => inner.GetPaymentIntentByOrderAsync(orderId, cancellationToken);
    public Task<StorePaymentIntentPage> GetPaymentIntentsAsync(int page, int pageSize, string? provider, string? status, CancellationToken cancellationToken) => inner.GetPaymentIntentsAsync(page, pageSize, provider, status, cancellationToken);
    public Task<StoreRedirectMatch?> ResolveRedirectAsync(string path, CancellationToken cancellationToken) => inner.ResolveRedirectAsync(path, cancellationToken);
    public Task<StoreRedirectRulePage> GetRedirectRulesAsync(int page, int pageSize, CancellationToken cancellationToken) => inner.GetRedirectRulesAsync(page, pageSize, cancellationToken);
    public Task<Guid?> CreateRedirectRuleAsync(string fromPath, string toPath, int statusCode, CancellationToken cancellationToken) => inner.CreateRedirectRuleAsync(fromPath, toPath, statusCode, cancellationToken);
    public Task<bool> DeactivateRedirectRuleAsync(Guid redirectRuleId, CancellationToken cancellationToken) => inner.DeactivateRedirectRuleAsync(redirectRuleId, cancellationToken);
    public Task<StoreInventoryProductDetails?> GetInventoryProductAsync(Guid productId, CancellationToken cancellationToken) => inner.GetInventoryProductAsync(productId, cancellationToken);
    public Task<bool> AdjustInventoryStockAsync(Guid productId, int quantityDelta, string? reason, CancellationToken cancellationToken) => inner.AdjustInventoryStockAsync(productId, quantityDelta, reason, cancellationToken);
    public Task<StoreStockMovementPage> GetInventoryMovementsAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken) => inner.GetInventoryMovementsAsync(productId, page, pageSize, cancellationToken);
    public Task<StoreStockReservationPage> GetActiveInventoryReservationsAsync(Guid? productId, int page, int pageSize, CancellationToken cancellationToken) => inner.GetActiveInventoryReservationsAsync(productId, page, pageSize, cancellationToken);
    public Task<IReadOnlyCollection<StoreShippingQuoteMethod>> GetShippingQuotesAsync(string countryCode, decimal subtotalAmount, string currency, CancellationToken cancellationToken) => inner.GetShippingQuotesAsync(countryCode, subtotalAmount, currency, cancellationToken);
    public Task<IReadOnlyCollection<StoreShippingMethod>> GetShippingMethodsAsync(bool activeOnly, CancellationToken cancellationToken) => inner.GetShippingMethodsAsync(activeOnly, cancellationToken);
    public Task<IReadOnlyCollection<StoreShippingZone>> GetShippingZonesAsync(bool activeOnly, CancellationToken cancellationToken) => inner.GetShippingZonesAsync(activeOnly, cancellationToken);
    public Task<IReadOnlyCollection<StoreShippingRateRule>> GetShippingRateRulesAsync(bool activeOnly, CancellationToken cancellationToken) => inner.GetShippingRateRulesAsync(activeOnly, cancellationToken);
    public Task<Guid?> CreateShippingMethodAsync(StoreShippingMethod request, CancellationToken cancellationToken) => inner.CreateShippingMethodAsync(request, cancellationToken);
    public Task<bool> UpdateShippingMethodAsync(Guid shippingMethodId, StoreShippingMethod request, CancellationToken cancellationToken) => inner.UpdateShippingMethodAsync(shippingMethodId, request, cancellationToken);
    public Task<Guid?> CreateShippingZoneAsync(StoreShippingZone request, CancellationToken cancellationToken) => inner.CreateShippingZoneAsync(request, cancellationToken);
    public Task<bool> UpdateShippingZoneAsync(Guid shippingZoneId, StoreShippingZone request, CancellationToken cancellationToken) => inner.UpdateShippingZoneAsync(shippingZoneId, request, cancellationToken);
    public Task<Guid?> CreateShippingRateRuleAsync(StoreShippingRateRule request, CancellationToken cancellationToken) => inner.CreateShippingRateRuleAsync(request, cancellationToken);
    public Task<bool> UpdateShippingRateRuleAsync(Guid shippingRateRuleId, StoreShippingRateRule request, CancellationToken cancellationToken) => inner.UpdateShippingRateRuleAsync(shippingRateRuleId, request, cancellationToken);
    public Task<StoreShipmentPage> GetShipmentsAsync(string? status, Guid? orderId, int page, int pageSize, CancellationToken cancellationToken) => inner.GetShipmentsAsync(status, orderId, page, pageSize, cancellationToken);
    public Task<StoreShipment?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken) => inner.GetShipmentAsync(shipmentId, cancellationToken);
    public Task<StoreShipment?> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken) => inner.GetShipmentByOrderAsync(orderId, cancellationToken);
    public Task<Guid?> CreateShipmentAsync(Guid orderId, string? shippingMethodCode, CancellationToken cancellationToken) => inner.CreateShipmentAsync(orderId, shippingMethodCode, cancellationToken);
    public Task<bool> CreateShipmentLabelAsync(Guid shipmentId, CancellationToken cancellationToken) => inner.CreateShipmentLabelAsync(shipmentId, cancellationToken);
    public Task<bool> MarkShipmentShippedAsync(Guid shipmentId, CancellationToken cancellationToken) => inner.MarkShipmentShippedAsync(shipmentId, cancellationToken);
    public Task<bool> CancelShipmentAsync(Guid shipmentId, string? reason, CancellationToken cancellationToken) => inner.CancelShipmentAsync(shipmentId, reason, cancellationToken);
    public Task<IReadOnlyCollection<StorePriceList>> GetPriceListsAsync(CancellationToken cancellationToken) => inner.GetPriceListsAsync(cancellationToken);
    public Task<Guid?> CreatePriceListAsync(StorePriceListRequest request, CancellationToken cancellationToken) => inner.CreatePriceListAsync(request, cancellationToken);
    public Task<bool> UpdatePriceListAsync(Guid priceListId, StorePriceListRequest request, CancellationToken cancellationToken) => inner.UpdatePriceListAsync(priceListId, request, cancellationToken);
    public Task<StoreVariantPrice?> GetVariantPriceAsync(Guid variantId, CancellationToken cancellationToken) => inner.GetVariantPriceAsync(variantId, cancellationToken);
    public Task<Guid?> CreateVariantPriceAsync(StoreVariantPriceRequest request, CancellationToken cancellationToken) => inner.CreateVariantPriceAsync(request, cancellationToken);
    public Task<bool> UpdateVariantPriceAsync(Guid variantPriceId, StoreVariantPriceRequest request, CancellationToken cancellationToken) => inner.UpdateVariantPriceAsync(variantPriceId, request, cancellationToken);
    public Task<IReadOnlyCollection<StorePromotion>> GetPromotionsAsync(CancellationToken cancellationToken) => inner.GetPromotionsAsync(cancellationToken);
    public Task<StorePromotion?> GetPromotionAsync(Guid promotionId, CancellationToken cancellationToken) => inner.GetPromotionAsync(promotionId, cancellationToken);
    public Task<Guid?> CreatePromotionAsync(StorePromotionRequest request, CancellationToken cancellationToken) => inner.CreatePromotionAsync(request, cancellationToken);
    public Task<bool> UpdatePromotionAsync(Guid promotionId, StorePromotionRequest request, CancellationToken cancellationToken) => inner.UpdatePromotionAsync(promotionId, request, cancellationToken);
    public Task<bool> ActivatePromotionAsync(Guid promotionId, CancellationToken cancellationToken) => inner.ActivatePromotionAsync(promotionId, cancellationToken);
    public Task<bool> ArchivePromotionAsync(Guid promotionId, CancellationToken cancellationToken) => inner.ArchivePromotionAsync(promotionId, cancellationToken);
    public Task<IReadOnlyCollection<StoreCoupon>> GetCouponsAsync(CancellationToken cancellationToken) => inner.GetCouponsAsync(cancellationToken);
    public Task<Guid?> CreateCouponAsync(StoreCouponRequest request, CancellationToken cancellationToken) => inner.CreateCouponAsync(request, cancellationToken);
    public Task<bool> UpdateCouponAsync(Guid couponId, StoreCouponRequest request, CancellationToken cancellationToken) => inner.UpdateCouponAsync(couponId, request, cancellationToken);
    public Task<bool> DisableCouponAsync(Guid couponId, CancellationToken cancellationToken) => inner.DisableCouponAsync(couponId, cancellationToken);
    public Task<StoreReviewModerationPage> GetAdminReviewsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken) => inner.GetAdminReviewsAsync(status, page, pageSize, cancellationToken);
    public Task<bool> ApproveReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken) => inner.ApproveReviewAsync(reviewId, notes, cancellationToken);
    public Task<bool> RejectReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken) => inner.RejectReviewAsync(reviewId, notes, cancellationToken);
    public Task<bool> HideReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken) => inner.HideReviewAsync(reviewId, notes, cancellationToken);
    public Task<StoreQuestionModerationPage> GetAdminQuestionsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken) => inner.GetAdminQuestionsAsync(status, page, pageSize, cancellationToken);
    public Task<bool> ApproveQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken) => inner.ApproveQuestionAsync(questionId, notes, cancellationToken);
    public Task<bool> RejectQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken) => inner.RejectQuestionAsync(questionId, notes, cancellationToken);
    public Task<bool> HideQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken) => inner.HideQuestionAsync(questionId, notes, cancellationToken);
    public Task<Guid?> AddOfficialAnswerAsync(Guid questionId, string displayName, string answerText, CancellationToken cancellationToken) => inner.AddOfficialAnswerAsync(questionId, displayName, answerText, cancellationToken);
    public Task<StoreAnswerModerationPage> GetAdminAnswersAsync(string? status, int page, int pageSize, CancellationToken cancellationToken) => inner.GetAdminAnswersAsync(status, page, pageSize, cancellationToken);
    public Task<bool> ApproveAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken) => inner.ApproveAnswerAsync(answerId, notes, cancellationToken);
    public Task<bool> RejectAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken) => inner.RejectAnswerAsync(answerId, notes, cancellationToken);
    public Task<bool> HideAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken) => inner.HideAnswerAsync(answerId, notes, cancellationToken);
    public Task<StoreReviewReportPage> GetReviewReportsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken) => inner.GetReviewReportsAsync(status, page, pageSize, cancellationToken);
    public Task<bool> ResolveReviewReportAsync(Guid reportId, bool dismiss, string? notes, CancellationToken cancellationToken) => inner.ResolveReviewReportAsync(reportId, dismiss, notes, cancellationToken);

    private async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await distributedCache.GetJsonAsync<T>(key, SerializerOptions, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read storefront cache key {CacheKey}", key);
        }

        var fresh = await factory();
        if (fresh is null)
        {
            return fresh;
        }

        try
        {
            await distributedCache.SetJsonAsync(key, fresh, ttl, SerializerOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to write storefront cache key {CacheKey}", key);
        }

        return fresh;
    }
}