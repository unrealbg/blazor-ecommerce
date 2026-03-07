using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace Storefront.Web.Services.Api;

public sealed class StoreApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor)
    : IStoreApiClient
{
    public async Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await httpClient.GetFromJsonAsync<IReadOnlyCollection<StoreProduct>>(
            "/api/v1/catalog/products",
            cancellationToken);

        return products ?? [];
    }

    public Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StoreProduct>($"/api/v1/catalog/products/by-slug/{slug}", cancellationToken);
    }

    public Task<StoreReviewSummary?> GetProductReviewSummaryAsync(Guid productId, CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StoreReviewSummary>(
            $"/api/v1/reviews/products/{productId:D}/summary",
            cancellationToken);
    }

    public async Task<StoreReviewPage> GetProductReviewsAsync(
        Guid productId,
        int page,
        int pageSize,
        string? sort,
        int? rating,
        CancellationToken cancellationToken)
    {
        var queryParameters = new List<KeyValuePair<string, string?>>
        {
            new("page", Math.Max(1, page).ToString(CultureInfo.InvariantCulture)),
            new("pageSize", Math.Clamp(pageSize, 1, 50).ToString(CultureInfo.InvariantCulture)),
        };

        if (!string.IsNullOrWhiteSpace(sort))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("sort", sort.Trim()));
        }

        if (rating is >= 1 and <= 5)
        {
            queryParameters.Add(new KeyValuePair<string, string?>("rating", rating.Value.ToString(CultureInfo.InvariantCulture)));
        }

        var uri = $"/api/v1/reviews/products/{productId:D}{QueryString.Create(queryParameters)}";
        var response = await httpClient.GetFromJsonAsync<StoreReviewPage>(uri, cancellationToken);

        return response ?? new StoreReviewPage(1, 10, 0, 1, []);
    }

    public async Task<StoreQuestionPage> GetProductQuestionsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var queryString = QueryString.Create(
        [
            new KeyValuePair<string, string?>("page", Math.Max(1, page).ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string?>("pageSize", Math.Clamp(pageSize, 1, 50).ToString(CultureInfo.InvariantCulture)),
        ]);

        var response = await httpClient.GetFromJsonAsync<StoreQuestionPage>(
            $"/api/v1/reviews/products/{productId:D}/questions{queryString}",
            cancellationToken);

        return response ?? new StoreQuestionPage(1, 10, 0, 1, []);
    }

    public async Task<StoreSearchProductsResponse> SearchProductsAsync(
        StoreSearchProductsRequest request,
        CancellationToken cancellationToken)
    {
        var uri = BuildSearchUri(request);
        var response = await httpClient.GetFromJsonAsync<StoreSearchProductsResponse>(uri, cancellationToken);

        return response ?? new StoreSearchProductsResponse(
            [],
            0,
            1,
            24,
            1,
            new StoreSearchFacets([], [], 0, new StoreSearchPriceSummary(null, null)),
            new StoreSearchAppliedFilters(null, null, [], null, null, null, "popular", 1, 24));
    }

    public async Task<StoreSearchSuggestionsResponse> SuggestProductsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var normalizedLimit = Math.Clamp(limit, 1, 10);
        var uri = $"/api/v1/search/suggest?q={encodedQuery}&limit={normalizedLimit}";

        var response = await httpClient.GetFromJsonAsync<StoreSearchSuggestionsResponse>(uri, cancellationToken);
        return response ?? new StoreSearchSuggestionsResponse(query, []);
    }

    public Task<StoreCart?> GetCartAsync(string customerId, CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StoreCart>($"/api/v1/cart/{customerId}", cancellationToken);
    }

    public async Task<bool> AddItemToCartAsync(
        string customerId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        return await AddItemToCartAsync(
            customerId,
            productId,
            Guid.Empty,
            quantity,
            cancellationToken);
    }

    public async Task<bool> AddItemToCartAsync(
        string customerId,
        Guid productId,
        Guid variantId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new AddCartItemRequest(productId, variantId, quantity),
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateCartItemQuantityAsync(
        string customerId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"/api/v1/cart/{customerId}/items/{productId}",
            new UpdateCartItemQuantityRequest(quantity),
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveCartItemAsync(
        string customerId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"/api/v1/cart/{customerId}/items/{productId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ApplyCouponAsync(
        string customerId,
        string couponCode,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/coupon",
            new ApplyCouponRequest(couponCode),
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveCouponAsync(
        string customerId,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"/api/v1/cart/{customerId}/coupon", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> SubmitReviewAsync(
        Guid productId,
        StoreSubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reviews/products/{productId:D}")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateReviewEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdateMyReviewAsync(
        Guid reviewId,
        StoreSubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/reviews/me/{reviewId:D}")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<StoreReviewVoteResult?> VoteReviewAsync(
        Guid reviewId,
        string voteType,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reviews/{reviewId:D}/vote")
        {
            Content = JsonContent.Create(new VoteReviewRequest(voteType)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<StoreReviewVoteResult>(cancellationToken);
    }

    public async Task<Guid?> ReportReviewAsync(
        Guid reviewId,
        string reasonType,
        string? message,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reviews/{reviewId:D}/report")
        {
            Content = JsonContent.Create(new ReportReviewRequest(reasonType, message)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateReviewEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<Guid?> SubmitQuestionAsync(
        Guid productId,
        StoreSubmitQuestionRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reviews/products/{productId:D}/questions")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateReviewEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<Guid?> SubmitAnswerAsync(
        Guid questionId,
        StoreSubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reviews/questions/{questionId:D}/answers")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateReviewEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<IReadOnlyCollection<StoreMyReview>> GetMyReviewsAsync(CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreMyReview>>(
            "/api/v1/reviews/me",
            cancellationToken);
        return response ?? [];
    }

    public async Task<IReadOnlyCollection<StoreMyQuestion>> GetMyQuestionsAsync(CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreMyQuestion>>(
            "/api/v1/reviews/me/questions",
            cancellationToken);
        return response ?? [];
    }

    public async Task<Guid?> CheckoutAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/checkout/{customerId}");
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CheckoutResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<Guid?> CheckoutAsync(
        StoreCheckoutRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CheckoutResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<StoreAuthResponse?> RegisterAsync(
        string email,
        string password,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequest(email, password, firstName, lastName, phoneNumber)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<StoreAuthResponse>(cancellationToken);
    }

    public async Task<StoreAuthResponse?> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, password, rememberMe)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<StoreAuthResponse>(cancellationToken);
    }

    public async Task<bool> LogoutAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/forgot-password")
        {
            Content = JsonContent.Create(new ForgotPasswordRequest(email)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/reset-password")
        {
            Content = JsonContent.Create(new ResetPasswordRequest(email, token, newPassword)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var uri = $"/api/v1/auth/verify-email?userId={userId}&token={Uri.EscapeDataString(token)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public Task<StoreCustomerProfile?> GetCurrentCustomerAsync(CancellationToken cancellationToken)
    {
        return GetAuthorizedOrDefaultAsync<StoreCustomerProfile>("/api/v1/customers/me", cancellationToken);
    }

    public async Task<bool> UpdateCurrentCustomerAsync(
        StoreUpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/customers/me")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyCollection<StoreCustomerAddress>> GetCurrentCustomerAddressesAsync(CancellationToken cancellationToken)
    {
        var addresses = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreCustomerAddress>>(
            "/api/v1/customers/me/addresses",
            cancellationToken);

        return addresses ?? [];
    }

    public async Task<Guid?> AddCurrentCustomerAddressAsync(
        StoreAddressRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/customers/me/addresses")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateAddressResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdateCurrentCustomerAddressAsync(
        Guid addressId,
        StoreAddressRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/customers/me/addresses/{addressId}")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCurrentCustomerAddressAsync(Guid addressId, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/customers/me/addresses/{addressId}");
        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyCollection<StoreOrderSummary>> GetMyOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreOrderSummary>>(
            "/api/v1/orders/my",
            cancellationToken);

        return orders ?? [];
    }

    public Task<StoreOrderSummary?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StoreOrderSummary>($"/api/v1/orders/{orderId}", cancellationToken);
    }

    public async Task<StorePaymentIntentAction?> CreatePaymentIntentAsync(
        Guid orderId,
        string? provider,
        string idempotencyKey,
        string? customerEmail,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/intents")
        {
            Content = JsonContent.Create(new CreatePaymentIntentRequest(orderId, provider, customerEmail)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StorePaymentIntentAction>(cancellationToken);
    }

    public async Task<StorePaymentIntentAction?> ConfirmPaymentIntentAsync(
        Guid paymentIntentId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/intents/{paymentIntentId}/confirm");
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StorePaymentIntentAction>(cancellationToken);
    }

    public async Task<StorePaymentIntentAction?> CancelPaymentIntentAsync(
        Guid paymentIntentId,
        string? reason,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/intents/{paymentIntentId}/cancel")
        {
            Content = JsonContent.Create(new CancelPaymentIntentRequest(reason)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StorePaymentIntentAction>(cancellationToken);
    }

    public async Task<StorePaymentIntentAction?> RefundPaymentIntentAsync(
        Guid paymentIntentId,
        decimal? amount,
        string? reason,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/intents/{paymentIntentId}/refund")
        {
            Content = JsonContent.Create(new RefundPaymentIntentRequest(amount, reason)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StorePaymentIntentAction>(cancellationToken);
    }

    public Task<StorePaymentIntentDetails?> GetPaymentIntentAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StorePaymentIntentDetails>(
            $"/api/v1/payments/intents/{paymentIntentId}",
            cancellationToken);
    }

    public Task<StorePaymentIntentDetails?> GetPaymentIntentByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StorePaymentIntentDetails>(
            $"/api/v1/payments/intents/by-order/{orderId}",
            cancellationToken);
    }

    public async Task<StorePaymentIntentPage> GetPaymentIntentsAsync(
        int page,
        int pageSize,
        string? provider,
        string? status,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(100, pageSize);

        var queryParameters = new List<KeyValuePair<string, string?>>
        {
            new("page", normalizedPage.ToString()),
            new("pageSize", normalizedPageSize.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(provider))
        {
            queryParameters.Add(new("provider", provider.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParameters.Add(new("status", status.Trim()));
        }

        var uri = $"/api/v1/payments/intents{QueryString.Create(queryParameters)}";
        var response = await GetAuthorizedOrDefaultAsync<StorePaymentIntentPage>(uri, cancellationToken);

        return response ?? new StorePaymentIntentPage(normalizedPage, normalizedPageSize, 0, []);
    }

    public Task<StoreRedirectMatch?> ResolveRedirectAsync(string path, CancellationToken cancellationToken)
    {
        var encodedPath = Uri.EscapeDataString(path);
        return GetOrDefaultAsync<StoreRedirectMatch>($"/api/v1/redirects/resolve?path={encodedPath}", cancellationToken);
    }

    public async Task<StoreRedirectRulePage> GetRedirectRulesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : pageSize;

        var response = await httpClient.GetFromJsonAsync<StoreRedirectRulePage>(
            $"/api/v1/redirects?page={normalizedPage}&pageSize={normalizedPageSize}",
            cancellationToken);

        return response ?? new StoreRedirectRulePage(normalizedPage, normalizedPageSize, 0, []);
    }

    public async Task<Guid?> CreateRedirectRuleAsync(
        string fromPath,
        string toPath,
        int statusCode,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/v1/redirects",
            new CreateRedirectRuleRequest(fromPath, toPath, statusCode),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateRedirectRuleResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> DeactivateRedirectRuleAsync(Guid redirectRuleId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/redirects/{redirectRuleId}/deactivate");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public Task<StoreInventoryProductDetails?> GetInventoryProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StoreInventoryProductDetails>(
            $"/api/v1/inventory/products/{productId}",
            cancellationToken);
    }

    public async Task<bool> AdjustInventoryStockAsync(
        Guid productId,
        int quantityDelta,
        string? reason,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/inventory/products/{productId}/adjust")
        {
            Content = JsonContent.Create(new AdjustInventoryStockRequest(quantityDelta, reason)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<StoreStockMovementPage> GetInventoryMovementsAsync(
        Guid productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 50 : pageSize;
        var uri = $"/api/v1/inventory/products/{productId}/movements?page={normalizedPage}&pageSize={normalizedPageSize}";

        var response = await GetAuthorizedOrDefaultAsync<StoreStockMovementPage>(uri, cancellationToken);
        return response ?? new StoreStockMovementPage(normalizedPage, normalizedPageSize, 0, []);
    }

    public async Task<StoreStockReservationPage> GetActiveInventoryReservationsAsync(
        Guid? productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 50 : pageSize;
        var query = productId is null
            ? $"/api/v1/inventory/reservations/active?page={normalizedPage}&pageSize={normalizedPageSize}"
            : $"/api/v1/inventory/reservations/active?productId={productId.Value}&page={normalizedPage}&pageSize={normalizedPageSize}";

        var response = await GetAuthorizedOrDefaultAsync<StoreStockReservationPage>(query, cancellationToken);
        return response ?? new StoreStockReservationPage(normalizedPage, normalizedPageSize, 0, []);
    }

    public async Task<IReadOnlyCollection<StoreShippingQuoteMethod>> GetShippingQuotesAsync(
        string countryCode,
        decimal subtotalAmount,
        string currency,
        CancellationToken cancellationToken)
    {
        var normalizedCountryCode = string.IsNullOrWhiteSpace(countryCode) ? "BG" : countryCode.Trim().ToUpperInvariant();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();
        var normalizedSubtotal = subtotalAmount < 0m ? 0m : decimal.Round(subtotalAmount, 2, MidpointRounding.AwayFromZero);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipping/quotes")
        {
            Content = JsonContent.Create(new QuoteShippingRequest(
                normalizedCountryCode,
                normalizedSubtotal,
                normalizedCurrency)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<ShippingQuoteResponse>(cancellationToken);
        return payload?.Methods ?? [];
    }

    public async Task<IReadOnlyCollection<StoreShippingMethod>> GetShippingMethodsAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreShippingMethod>>(
            $"/api/v1/shipping/methods?activeOnly={(activeOnly ? "true" : "false")}",
            cancellationToken);
        return response ?? [];
    }

    public async Task<IReadOnlyCollection<StoreShippingZone>> GetShippingZonesAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreShippingZone>>(
            $"/api/v1/shipping/zones?activeOnly={(activeOnly ? "true" : "false")}",
            cancellationToken);
        return response ?? [];
    }

    public async Task<IReadOnlyCollection<StoreShippingRateRule>> GetShippingRateRulesAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreShippingRateRule>>(
            $"/api/v1/shipping/rules?activeOnly={(activeOnly ? "true" : "false")}",
            cancellationToken);
        return response ?? [];
    }

    public async Task<Guid?> CreateShippingMethodAsync(
        StoreShippingMethod request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipping/methods")
        {
            Content = JsonContent.Create(new CreateShippingMethodRequest(
                request.Code,
                request.Name,
                request.Description,
                request.Provider,
                request.Type,
                request.BasePriceAmount,
                request.Currency,
                request.SupportsTracking,
                request.SupportsPickupPoint,
                request.EstimatedMinDays,
                request.EstimatedMaxDays,
                request.Priority)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateShippingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdateShippingMethodAsync(
        Guid shippingMethodId,
        StoreShippingMethod request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/shipping/methods/{shippingMethodId}")
        {
            Content = JsonContent.Create(new UpdateShippingMethodRequest(
                request.Name,
                request.Description,
                request.Provider,
                request.Type,
                request.BasePriceAmount,
                request.Currency,
                request.SupportsTracking,
                request.SupportsPickupPoint,
                request.EstimatedMinDays,
                request.EstimatedMaxDays,
                request.Priority,
                request.IsActive)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> CreateShippingZoneAsync(
        StoreShippingZone request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipping/zones")
        {
            Content = JsonContent.Create(new CreateShippingZoneRequest(
                request.Code,
                request.Name,
                request.CountryCodes)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateShippingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdateShippingZoneAsync(
        Guid shippingZoneId,
        StoreShippingZone request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/shipping/zones/{shippingZoneId}")
        {
            Content = JsonContent.Create(new UpdateShippingZoneRequest(
                request.Name,
                request.CountryCodes,
                request.IsActive)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> CreateShippingRateRuleAsync(
        StoreShippingRateRule request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipping/rules")
        {
            Content = JsonContent.Create(new CreateShippingRateRuleRequest(
                request.ShippingMethodId,
                request.ShippingZoneId,
                request.MinOrderAmount,
                request.MaxOrderAmount,
                request.MinWeightKg,
                request.MaxWeightKg,
                request.PriceAmount,
                request.FreeShippingThresholdAmount,
                request.Currency)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateShippingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdateShippingRateRuleAsync(
        Guid shippingRateRuleId,
        StoreShippingRateRule request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/shipping/rules/{shippingRateRuleId}")
        {
            Content = JsonContent.Create(new UpdateShippingRateRuleRequest(
                request.MinOrderAmount,
                request.MaxOrderAmount,
                request.MinWeightKg,
                request.MaxWeightKg,
                request.PriceAmount,
                request.FreeShippingThresholdAmount,
                request.Currency,
                request.IsActive)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<StoreShipmentPage> GetShipmentsAsync(
        string? status,
        Guid? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(100, pageSize);

        var queryParameters = new List<KeyValuePair<string, string?>>
        {
            new("page", normalizedPage.ToString()),
            new("pageSize", normalizedPageSize.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParameters.Add(new("status", status.Trim()));
        }

        if (orderId is not null)
        {
            queryParameters.Add(new("orderId", orderId.Value.ToString("D")));
        }

        var uri = $"/api/v1/shipping/shipments{QueryString.Create(queryParameters)}";
        var response = await GetAuthorizedOrDefaultAsync<StoreShipmentPage>(uri, cancellationToken);
        return response ?? new StoreShipmentPage(normalizedPage, normalizedPageSize, 0, []);
    }

    public Task<StoreShipment?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        return GetAuthorizedOrDefaultAsync<StoreShipment>(
            $"/api/v1/shipping/shipments/{shipmentId:D}",
            cancellationToken);
    }

    public Task<StoreShipment?> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return GetAuthorizedOrDefaultAsync<StoreShipment>(
            $"/api/v1/shipping/shipments/by-order/{orderId:D}",
            cancellationToken);
    }

    public async Task<Guid?> CreateShipmentAsync(
        Guid orderId,
        string? shippingMethodCode,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipping/shipments")
        {
            Content = JsonContent.Create(new CreateShipmentRequest(orderId, shippingMethodCode)),
        };

        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateShippingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> CreateShipmentLabelAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/shipping/shipments/{shipmentId:D}/create-label");
        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MarkShipmentShippedAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/shipping/shipments/{shipmentId:D}/mark-shipped");
        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelShipmentAsync(
        Guid shipmentId,
        string? reason,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/shipping/shipments/{shipmentId:D}/cancel")
        {
            Content = JsonContent.Create(new CancelShipmentRequest(reason)),
        };
        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyCollection<StorePriceList>> GetPriceListsAsync(CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StorePriceList>>(
            "/api/v1/pricing/price-lists",
            cancellationToken);
        return response ?? [];
    }

    public async Task<Guid?> CreatePriceListAsync(StorePriceListRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pricing/price-lists")
        {
            Content = JsonContent.Create(new CreatePriceListRequest(
                request.Name,
                request.Code,
                request.Currency,
                request.IsDefault,
                request.IsActive,
                request.Priority)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatePricingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdatePriceListAsync(
        Guid priceListId,
        StorePriceListRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/pricing/price-lists/{priceListId:D}")
        {
            Content = JsonContent.Create(new UpdatePriceListRequest(
                request.Name,
                request.Currency,
                request.IsDefault,
                request.IsActive,
                request.Priority)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public Task<StoreVariantPrice?> GetVariantPriceAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return GetAuthorizedOrDefaultAsync<StoreVariantPrice>(
            $"/api/v1/pricing/variant-prices/by-variant/{variantId:D}",
            cancellationToken);
    }

    public async Task<Guid?> CreateVariantPriceAsync(
        StoreVariantPriceRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pricing/variant-prices")
        {
            Content = JsonContent.Create(new CreateVariantPriceRequest(
                request.PriceListId,
                request.VariantId,
                request.BasePriceAmount,
                request.CompareAtPriceAmount,
                request.Currency,
                request.IsActive,
                request.ValidFromUtc,
                request.ValidToUtc)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatePricingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdateVariantPriceAsync(
        Guid variantPriceId,
        StoreVariantPriceRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/pricing/variant-prices/{variantPriceId:D}")
        {
            Content = JsonContent.Create(new UpdateVariantPriceRequest(
                request.BasePriceAmount,
                request.CompareAtPriceAmount,
                request.Currency,
                request.IsActive,
                request.ValidFromUtc,
                request.ValidToUtc)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyCollection<StorePromotion>> GetPromotionsAsync(CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StorePromotion>>(
            "/api/v1/pricing/promotions",
            cancellationToken);
        return response ?? [];
    }

    public Task<StorePromotion?> GetPromotionAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        return GetAuthorizedOrDefaultAsync<StorePromotion>(
            $"/api/v1/pricing/promotions/{promotionId:D}",
            cancellationToken);
    }

    public async Task<Guid?> CreatePromotionAsync(StorePromotionRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pricing/promotions")
        {
            Content = JsonContent.Create(new CreatePromotionRequest(
                request.Name,
                request.Code,
                request.Type,
                request.Description,
                request.Priority,
                request.IsExclusive,
                request.AllowWithCoupons,
                request.StartAtUtc,
                request.EndAtUtc,
                request.UsageLimitTotal,
                request.UsageLimitPerCustomer,
                request.Scopes,
                request.Conditions,
                request.Benefits)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatePricingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdatePromotionAsync(
        Guid promotionId,
        StorePromotionRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/pricing/promotions/{promotionId:D}")
        {
            Content = JsonContent.Create(new UpdatePromotionRequest(
                request.Name,
                request.Code,
                request.Description,
                request.Priority,
                request.IsExclusive,
                request.AllowWithCoupons,
                request.StartAtUtc,
                request.EndAtUtc,
                request.UsageLimitTotal,
                request.UsageLimitPerCustomer,
                request.Scopes,
                request.Conditions,
                request.Benefits)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ActivatePromotionAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pricing/promotions/{promotionId:D}/activate");
        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ArchivePromotionAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pricing/promotions/{promotionId:D}/archive");
        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyCollection<StoreCoupon>> GetCouponsAsync(CancellationToken cancellationToken)
    {
        var response = await GetAuthorizedOrDefaultAsync<IReadOnlyCollection<StoreCoupon>>(
            "/api/v1/pricing/coupons",
            cancellationToken);
        return response ?? [];
    }

    public async Task<Guid?> CreateCouponAsync(StoreCouponRequest request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pricing/coupons")
        {
            Content = JsonContent.Create(new CreateCouponRequest(
                request.Code,
                request.Description,
                request.PromotionId,
                request.StartAtUtc,
                request.EndAtUtc,
                request.UsageLimitTotal,
                request.UsageLimitPerCustomer)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreatePricingEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> UpdateCouponAsync(
        Guid couponId,
        StoreCouponRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/pricing/coupons/{couponId:D}")
        {
            Content = JsonContent.Create(new UpdateCouponRequest(
                request.Description,
                request.StartAtUtc,
                request.EndAtUtc,
                request.UsageLimitTotal,
                request.UsageLimitPerCustomer)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DisableCouponAsync(Guid couponId, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/pricing/coupons/{couponId:D}/disable");
        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<StoreReviewModerationPage> GetAdminReviewsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var uri = BuildReviewAdminUri("/api/v1/reviews/admin/reviews", status, page, pageSize);
        var response = await GetAuthorizedOrDefaultAsync<StoreReviewModerationPage>(uri, cancellationToken);
        return response ?? new StoreReviewModerationPage(1, 20, 0, 1, []);
    }

    public Task<bool> ApproveReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/reviews/{reviewId:D}/approve", notes, cancellationToken);
    }

    public Task<bool> RejectReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/reviews/{reviewId:D}/reject", notes, cancellationToken);
    }

    public Task<bool> HideReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/reviews/{reviewId:D}/hide", notes, cancellationToken);
    }

    public async Task<StoreQuestionModerationPage> GetAdminQuestionsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var uri = BuildReviewAdminUri("/api/v1/reviews/admin/questions", status, page, pageSize);
        var response = await GetAuthorizedOrDefaultAsync<StoreQuestionModerationPage>(uri, cancellationToken);
        return response ?? new StoreQuestionModerationPage(1, 20, 0, 1, []);
    }

    public Task<bool> ApproveQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/questions/{questionId:D}/approve", notes, cancellationToken);
    }

    public Task<bool> RejectQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/questions/{questionId:D}/reject", notes, cancellationToken);
    }

    public Task<bool> HideQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/questions/{questionId:D}/hide", notes, cancellationToken);
    }

    public async Task<Guid?> AddOfficialAnswerAsync(
        Guid questionId,
        string displayName,
        string answerText,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/reviews/admin/questions/{questionId:D}/official-answer")
        {
            Content = JsonContent.Create(new OfficialAnswerRequest(displayName, answerText)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateReviewEntityResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<StoreAnswerModerationPage> GetAdminAnswersAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var uri = BuildReviewAdminUri("/api/v1/reviews/admin/answers", status, page, pageSize);
        var response = await GetAuthorizedOrDefaultAsync<StoreAnswerModerationPage>(uri, cancellationToken);
        return response ?? new StoreAnswerModerationPage(1, 20, 0, 1, []);
    }

    public Task<bool> ApproveAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/answers/{answerId:D}/approve", notes, cancellationToken);
    }

    public Task<bool> RejectAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/answers/{answerId:D}/reject", notes, cancellationToken);
    }

    public Task<bool> HideAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken)
    {
        return SendReviewModerationAsync($"/api/v1/reviews/admin/answers/{answerId:D}/hide", notes, cancellationToken);
    }

    public async Task<StoreReviewReportPage> GetReviewReportsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var uri = BuildReviewAdminUri("/api/v1/reviews/admin/reports", status, page, pageSize);
        var response = await GetAuthorizedOrDefaultAsync<StoreReviewReportPage>(uri, cancellationToken);
        return response ?? new StoreReviewReportPage(1, 20, 0, 1, []);
    }

    public async Task<bool> ResolveReviewReportAsync(
        Guid reportId,
        bool dismiss,
        string? notes,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reviews/admin/reports/{reportId:D}/resolve")
        {
            Content = JsonContent.Create(new ResolveReviewReportRequest(dismiss, notes)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static string BuildSearchUri(StoreSearchProductsRequest request)
    {
        var queryParameters = new List<KeyValuePair<string, string?>>();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("q", request.Query.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("categorySlug", request.CategorySlug.Trim()));
        }

        foreach (var brand in request.Brands.Where(brand => !string.IsNullOrWhiteSpace(brand)))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("brand", brand.Trim()));
        }

        if (request.MinPrice is not null)
        {
            queryParameters.Add(new KeyValuePair<string, string?>(
                "minPrice",
                request.MinPrice.Value.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        if (request.MaxPrice is not null)
        {
            queryParameters.Add(new KeyValuePair<string, string?>(
                "maxPrice",
                request.MaxPrice.Value.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        if (request.InStock is not null)
        {
            queryParameters.Add(new KeyValuePair<string, string?>("inStock", request.InStock.Value ? "true" : "false"));
        }

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("sort", request.Sort));
        }

        queryParameters.Add(new KeyValuePair<string, string?>("page", Math.Max(1, request.Page).ToString()));
        queryParameters.Add(new KeyValuePair<string, string?>("pageSize", Math.Clamp(request.PageSize, 1, 100).ToString()));

        return queryParameters.Count == 0
            ? "/api/v1/search/products"
            : $"/api/v1/search/products{QueryString.Create(queryParameters)}";
    }

    private static string BuildReviewAdminUri(string path, string? status, int page, int pageSize)
    {
        var queryParameters = new List<KeyValuePair<string, string?>>
        {
            new("page", Math.Max(1, page).ToString(CultureInfo.InvariantCulture)),
            new("pageSize", Math.Clamp(pageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("status", status.Trim()));
        }

        return $"{path}{QueryString.Create(queryParameters)}";
    }

    private async Task<T?> GetOrDefaultAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task<T?> GetAuthorizedOrDefaultAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendWithCookieForwardingAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithCookieForwardingAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is not null && context.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
        }

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (context is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var setCookie in setCookieHeaders)
            {
                context.Response.Headers.Append("Set-Cookie", setCookie);
            }
        }

        return response;
    }

    private async Task<bool> SendReviewModerationAsync(
        string path,
        string? notes,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new ModerationNotesRequest(notes)),
        };

        using var response = await SendWithCookieForwardingAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private sealed record AddCartItemRequest(Guid ProductId, Guid VariantId, int Quantity);

    private sealed record ApplyCouponRequest(string CouponCode);

    private sealed record UpdateCartItemQuantityRequest(int Quantity);

    private sealed record CheckoutResponse(Guid Id);

    private sealed record CreateAddressResponse(Guid Id);

    private sealed record RegisterRequest(
        string Email,
        string Password,
        string? FirstName,
        string? LastName,
        string? PhoneNumber);

    private sealed record LoginRequest(string Email, string Password, bool RememberMe);

    private sealed record ForgotPasswordRequest(string Email);

    private sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

    private sealed record CreateRedirectRuleRequest(string FromPath, string ToPath, int StatusCode);

    private sealed record CreateRedirectRuleResponse(Guid Id);

    private sealed record CreateReviewEntityResponse(Guid Id);

    private sealed record CreatePricingEntityResponse(Guid Id);

    private sealed record VoteReviewRequest(string VoteType);

    private sealed record ReportReviewRequest(string ReasonType, string? Message);

    private sealed record ModerationNotesRequest(string? Notes);

    private sealed record OfficialAnswerRequest(string DisplayName, string AnswerText);

    private sealed record ResolveReviewReportRequest(bool Dismiss, string? Notes);

    private sealed record CreatePaymentIntentRequest(Guid OrderId, string? Provider, string? CustomerEmail);

    private sealed record CancelPaymentIntentRequest(string? Reason);

    private sealed record RefundPaymentIntentRequest(decimal? Amount, string? Reason);

    private sealed record AdjustInventoryStockRequest(int QuantityDelta, string? Reason);

    private sealed record QuoteShippingRequest(string CountryCode, decimal SubtotalAmount, string Currency);

    private sealed record ShippingQuoteResponse(IReadOnlyCollection<StoreShippingQuoteMethod> Methods);

    private sealed record CreateShippingEntityResponse(Guid Id);

    private sealed record CreateShippingMethodRequest(
        string Code,
        string Name,
        string? Description,
        string Provider,
        string Type,
        decimal BasePriceAmount,
        string Currency,
        bool SupportsTracking,
        bool SupportsPickupPoint,
        int? EstimatedMinDays,
        int? EstimatedMaxDays,
        int Priority);

    private sealed record UpdateShippingMethodRequest(
        string Name,
        string? Description,
        string Provider,
        string Type,
        decimal BasePriceAmount,
        string Currency,
        bool SupportsTracking,
        bool SupportsPickupPoint,
        int? EstimatedMinDays,
        int? EstimatedMaxDays,
        int Priority,
        bool IsActive);

    private sealed record CreateShippingZoneRequest(
        string Code,
        string Name,
        IReadOnlyCollection<string> CountryCodes);

    private sealed record UpdateShippingZoneRequest(
        string Name,
        IReadOnlyCollection<string> CountryCodes,
        bool IsActive);

    private sealed record CreateShippingRateRuleRequest(
        Guid ShippingMethodId,
        Guid ShippingZoneId,
        decimal? MinOrderAmount,
        decimal? MaxOrderAmount,
        decimal? MinWeightKg,
        decimal? MaxWeightKg,
        decimal PriceAmount,
        decimal? FreeShippingThresholdAmount,
        string Currency);

    private sealed record UpdateShippingRateRuleRequest(
        decimal? MinOrderAmount,
        decimal? MaxOrderAmount,
        decimal? MinWeightKg,
        decimal? MaxWeightKg,
        decimal PriceAmount,
        decimal? FreeShippingThresholdAmount,
        string Currency,
        bool IsActive);

    private sealed record CreateShipmentRequest(Guid OrderId, string? ShippingMethodCode);

    private sealed record CancelShipmentRequest(string? Reason);

    private sealed record CreatePriceListRequest(
        string Name,
        string Code,
        string Currency,
        bool IsDefault,
        bool IsActive,
        int Priority);

    private sealed record UpdatePriceListRequest(
        string Name,
        string Currency,
        bool IsDefault,
        bool IsActive,
        int Priority);

    private sealed record CreateVariantPriceRequest(
        Guid PriceListId,
        Guid VariantId,
        decimal BasePriceAmount,
        decimal? CompareAtPriceAmount,
        string Currency,
        bool IsActive,
        DateTime? ValidFromUtc,
        DateTime? ValidToUtc);

    private sealed record UpdateVariantPriceRequest(
        decimal BasePriceAmount,
        decimal? CompareAtPriceAmount,
        string Currency,
        bool IsActive,
        DateTime? ValidFromUtc,
        DateTime? ValidToUtc);

    private sealed record CreatePromotionRequest(
        string Name,
        string? Code,
        int Type,
        string? Description,
        int Priority,
        bool IsExclusive,
        bool AllowWithCoupons,
        DateTime? StartAtUtc,
        DateTime? EndAtUtc,
        int? UsageLimitTotal,
        int? UsageLimitPerCustomer,
        IReadOnlyCollection<StorePromotionScope> Scopes,
        IReadOnlyCollection<StorePromotionCondition> Conditions,
        IReadOnlyCollection<StorePromotionBenefit> Benefits);

    private sealed record UpdatePromotionRequest(
        string Name,
        string? Code,
        string? Description,
        int Priority,
        bool IsExclusive,
        bool AllowWithCoupons,
        DateTime? StartAtUtc,
        DateTime? EndAtUtc,
        int? UsageLimitTotal,
        int? UsageLimitPerCustomer,
        IReadOnlyCollection<StorePromotionScope> Scopes,
        IReadOnlyCollection<StorePromotionCondition> Conditions,
        IReadOnlyCollection<StorePromotionBenefit> Benefits);

    private sealed record CreateCouponRequest(
        string Code,
        string? Description,
        Guid PromotionId,
        DateTime? StartAtUtc,
        DateTime? EndAtUtc,
        int? UsageLimitTotal,
        int? UsageLimitPerCustomer);

    private sealed record UpdateCouponRequest(
        string? Description,
        DateTime? StartAtUtc,
        DateTime? EndAtUtc,
        int? UsageLimitTotal,
        int? UsageLimitPerCustomer);
}
