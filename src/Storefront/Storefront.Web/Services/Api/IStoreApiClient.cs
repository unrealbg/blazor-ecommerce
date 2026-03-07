namespace Storefront.Web.Services.Api;

public interface IStoreApiClient
{
    Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken);

    Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<StoreReviewSummary?> GetProductReviewSummaryAsync(Guid productId, CancellationToken cancellationToken);

    Task<StoreReviewPage> GetProductReviewsAsync(
        Guid productId,
        int page,
        int pageSize,
        string? sort,
        int? rating,
        CancellationToken cancellationToken);

    Task<StoreQuestionPage> GetProductQuestionsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

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

    Task<bool> AddItemToCartAsync(
        string customerId,
        Guid productId,
        Guid variantId,
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

    Task<bool> ApplyCouponAsync(
        string customerId,
        string couponCode,
        CancellationToken cancellationToken);

    Task<bool> RemoveCouponAsync(
        string customerId,
        CancellationToken cancellationToken);

    Task<Guid?> SubmitReviewAsync(
        Guid productId,
        StoreSubmitReviewRequest request,
        CancellationToken cancellationToken);

    Task<bool> UpdateMyReviewAsync(
        Guid reviewId,
        StoreSubmitReviewRequest request,
        CancellationToken cancellationToken);

    Task<StoreReviewVoteResult?> VoteReviewAsync(
        Guid reviewId,
        string voteType,
        CancellationToken cancellationToken);

    Task<Guid?> ReportReviewAsync(
        Guid reviewId,
        string reasonType,
        string? message,
        CancellationToken cancellationToken);

    Task<Guid?> SubmitQuestionAsync(
        Guid productId,
        StoreSubmitQuestionRequest request,
        CancellationToken cancellationToken);

    Task<Guid?> SubmitAnswerAsync(
        Guid questionId,
        StoreSubmitAnswerRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreMyReview>> GetMyReviewsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreMyQuestion>> GetMyQuestionsAsync(CancellationToken cancellationToken);

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

    Task<IReadOnlyCollection<StorePriceList>> GetPriceListsAsync(CancellationToken cancellationToken);

    Task<Guid?> CreatePriceListAsync(StorePriceListRequest request, CancellationToken cancellationToken);

    Task<bool> UpdatePriceListAsync(
        Guid priceListId,
        StorePriceListRequest request,
        CancellationToken cancellationToken);

    Task<StoreVariantPrice?> GetVariantPriceAsync(Guid variantId, CancellationToken cancellationToken);

    Task<Guid?> CreateVariantPriceAsync(StoreVariantPriceRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateVariantPriceAsync(
        Guid variantPriceId,
        StoreVariantPriceRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StorePromotion>> GetPromotionsAsync(CancellationToken cancellationToken);

    Task<StorePromotion?> GetPromotionAsync(Guid promotionId, CancellationToken cancellationToken);

    Task<Guid?> CreatePromotionAsync(StorePromotionRequest request, CancellationToken cancellationToken);

    Task<bool> UpdatePromotionAsync(
        Guid promotionId,
        StorePromotionRequest request,
        CancellationToken cancellationToken);

    Task<bool> ActivatePromotionAsync(Guid promotionId, CancellationToken cancellationToken);

    Task<bool> ArchivePromotionAsync(Guid promotionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StoreCoupon>> GetCouponsAsync(CancellationToken cancellationToken);

    Task<Guid?> CreateCouponAsync(StoreCouponRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateCouponAsync(
        Guid couponId,
        StoreCouponRequest request,
        CancellationToken cancellationToken);

    Task<bool> DisableCouponAsync(Guid couponId, CancellationToken cancellationToken);

    Task<StoreReviewModerationPage> GetAdminReviewsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ApproveReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken);

    Task<bool> RejectReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken);

    Task<bool> HideReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken);

    Task<StoreQuestionModerationPage> GetAdminQuestionsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ApproveQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken);

    Task<bool> RejectQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken);

    Task<bool> HideQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken);

    Task<Guid?> AddOfficialAnswerAsync(
        Guid questionId,
        string displayName,
        string answerText,
        CancellationToken cancellationToken);

    Task<StoreAnswerModerationPage> GetAdminAnswersAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ApproveAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken);

    Task<bool> RejectAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken);

    Task<bool> HideAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken);

    Task<StoreReviewReportPage> GetReviewReportsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> ResolveReviewReportAsync(
        Guid reportId,
        bool dismiss,
        string? notes,
        CancellationToken cancellationToken);
}
