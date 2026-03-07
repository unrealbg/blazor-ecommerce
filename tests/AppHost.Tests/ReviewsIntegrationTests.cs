using System.Net;
using System.Net.Http.Json;
using Catalog.Api;
using Catalog.Infrastructure.Persistence;
using Customers.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppHost.Tests;

public sealed class ReviewsIntegrationTests
{
    [Fact]
    public async Task SubmitReviewEndpoint_ShouldRequireAuthorization()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Review auth product", 89m);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/reviews/products/{product.ProductId:D}",
            new
            {
                variantId = product.VariantId,
                title = "Unauthorized",
                body = "This should be rejected because there is no authenticated user.",
                rating = 5,
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PendingReview_ShouldNotAppearInPublicAggregate()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Pending review product", 79m);
        await AuthenticateAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/reviews/products/{product.ProductId:D}",
            new
            {
                variantId = product.VariantId,
                title = "Pending review",
                body = "This review is waiting for moderation and must stay out of aggregates.",
                rating = 4,
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var summary = await client.GetFromJsonAsync<ReviewSummaryPayload>($"/api/v1/reviews/products/{product.ProductId:D}/summary");

        Assert.NotNull(summary);
        Assert.Equal(0, summary!.ApprovedReviewCount);
        Assert.Equal(0m, summary.AverageRating);
    }

    [Fact]
    public async Task ApprovingReview_ShouldExposeItPublicly_AndUpdateAggregate()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Approved review product", 99m);
        await AuthenticateAsync(client);
        var reviewId = await SubmitReviewAsync(client, product.ProductId, product.VariantId, 5, "Excellent", "Excellent keyboard for everyday use.");

        var approveResponse = await client.PostAsJsonAsync(
            $"/api/v1/reviews/admin/reviews/{reviewId:D}/approve",
            new { notes = "Looks good" });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var reviews = await client.GetFromJsonAsync<ReviewPagePayload>($"/api/v1/reviews/products/{product.ProductId:D}");
        var summary = await client.GetFromJsonAsync<ReviewSummaryPayload>($"/api/v1/reviews/products/{product.ProductId:D}/summary");

        Assert.NotNull(reviews);
        Assert.NotNull(summary);
        Assert.Single(reviews!.Items);
        Assert.Equal(reviewId, reviews.Items.Single().Id);
        Assert.Equal(1, summary!.ApprovedReviewCount);
        Assert.Equal(5m, summary.AverageRating);
    }

    [Fact]
    public async Task VerifiedPurchaserReview_ShouldBeMarkedVerified()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Verified purchase product", 109m);
        await AuthenticateAsync(client);
        var customer = await client.GetFromJsonAsync<CustomerProfilePayload>("/api/v1/customers/me");
        Assert.NotNull(customer);

        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customer!.Id:N}/items",
            new Cart.Api.CartModuleExtensions.AddItemRequest(product.ProductId, product.VariantId, 1));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        using var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/checkout/{customer.Id:N}");
        checkoutRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var checkoutResponse = await client.SendAsync(checkoutRequest);
        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        var orderPayload = await checkoutResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(orderPayload);

        using var paymentRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/intents")
        {
            Content = JsonContent.Create(new
            {
                orderId = orderPayload!.Id,
                provider = "Demo",
                customerEmail = customer.Email,
            }),
        };
        paymentRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var paymentResponse = await client.SendAsync(paymentRequest);
        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);

        await WaitForConditionAsync(
            async () =>
            {
                var order = await client.GetFromJsonAsync<OrderStatusPayload>($"/api/v1/orders/{orderPayload.Id:D}");
                return order is not null && string.Equals(order.Status, "Paid", StringComparison.Ordinal);
            },
            "Order was not marked as Paid after payment creation.");

        var reviewId = await SubmitReviewAsync(
            client,
            product.ProductId,
            product.VariantId,
            5,
            "Verified",
            "This review should be marked as a verified purchase.");

        await client.PostAsJsonAsync($"/api/v1/reviews/admin/reviews/{reviewId:D}/approve", new { notes = "Approved" });
        var reviews = await client.GetFromJsonAsync<ReviewPagePayload>($"/api/v1/reviews/products/{product.ProductId:D}");

        Assert.NotNull(reviews);
        var review = Assert.Single(reviews!.Items);
        Assert.True(review.IsVerifiedPurchase);
        Assert.NotNull(review.VerifiedPurchaseOrderId);
    }

    [Fact]
    public async Task ReviewReport_ShouldAppearInAdminQueue()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Reported review product", 59m);
        await AuthenticateAsync(client);
        var reviewId = await SubmitReviewAsync(client, product.ProductId, product.VariantId, 2, "Bad", "This is a problematic review.");
        await client.PostAsJsonAsync($"/api/v1/reviews/admin/reviews/{reviewId:D}/approve", new { notes = "Approved" });

        var reportResponse = await client.PostAsJsonAsync(
            $"/api/v1/reviews/{reviewId:D}/report",
            new
            {
                reasonType = "Spam",
                message = "Looks suspicious",
            });
        Assert.Equal(HttpStatusCode.Created, reportResponse.StatusCode);

        var reports = await client.GetFromJsonAsync<ReviewReportPagePayload>("/api/v1/reviews/admin/reports?status=Open");

        Assert.NotNull(reports);
        Assert.Contains(reports!.Items, item => item.ReviewId == reviewId && item.ReasonType == "Spam");
    }

    [Fact]
    public async Task ApprovedQuestionAndOfficialAnswer_ShouldAppearPublicly()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Question product", 45m);
        await AuthenticateAsync(client);

        var questionResponse = await client.PostAsJsonAsync(
            $"/api/v1/reviews/products/{product.ProductId:D}/questions",
            new { questionText = "Does this work with Windows and macOS?" });
        Assert.Equal(HttpStatusCode.Created, questionResponse.StatusCode);
        var questionPayload = await questionResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(questionPayload);

        var approveQuestion = await client.PostAsJsonAsync(
            $"/api/v1/reviews/admin/questions/{questionPayload!.Id:D}/approve",
            new { notes = "Approved" });
        Assert.Equal(HttpStatusCode.OK, approveQuestion.StatusCode);

        var officialAnswer = await client.PostAsJsonAsync(
            $"/api/v1/reviews/admin/questions/{questionPayload.Id:D}/official-answer",
            new
            {
                displayName = "Support Team",
                answerText = "Yes. It supports both platforms.",
            });
        Assert.Equal(HttpStatusCode.Created, officialAnswer.StatusCode);

        var questions = await client.GetFromJsonAsync<QuestionPagePayload>($"/api/v1/reviews/products/{product.ProductId:D}/questions");

        Assert.NotNull(questions);
        var question = Assert.Single(questions!.Items);
        Assert.Equal("Approved", question.Status);
        var answer = Assert.Single(question.Answers);
        Assert.True(answer.IsOfficialAnswer);
        Assert.Equal("Approved", answer.Status);
    }

    [Fact]
    public async Task AdminModerationEndpoints_ShouldRequireAuthorization()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/reviews/admin/reviews");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"reviews-{Guid.NewGuid():N}@example.com";
        const string password = "Reviews!Pass123";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CustomersModuleExtensions.RegisterRequest(
                Email: email,
                Password: password,
                FirstName: "Alex",
                LastName: "Mercer",
                PhoneNumber: "+359888000000"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new CustomersModuleExtensions.LoginRequest(email, password, RememberMe: false));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    private static async Task<ProductInfo> CreateProductAsync(
        AppHostWebApplicationFactory factory,
        HttpClient client,
        string name,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CatalogModuleExtensions.CreateProductRequest(
                Name: name,
                Description: $"{name} description",
                Currency: "EUR",
                Amount: amount,
                IsActive: true,
                Brand: "Contoso",
                Sku: $"SKU-{Guid.NewGuid():N}",
                ImageUrl: "/images/test.png",
                IsInStock: true,
                CategorySlug: $"reviews-{Guid.NewGuid():N}",
                CategoryName: "Reviews Tests"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(payload);

        await using var scope = factory.Services.CreateAsyncScope();
        var catalogDbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await catalogDbContext.Products.SingleAsync(item => item.Id == payload!.Id);
        return new ProductInfo(product.Id, product.DefaultVariantId);
    }

    private static async Task<Guid> SubmitReviewAsync(
        HttpClient client,
        Guid productId,
        Guid variantId,
        int rating,
        string title,
        string body)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/reviews/products/{productId:D}",
            new
            {
                variantId,
                title,
                body,
                rating,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(payload);
        return payload!.Id;
    }

    private sealed record ProductInfo(Guid ProductId, Guid VariantId);

    private sealed record CreateEntityPayload(Guid Id);

    private sealed record CustomerProfilePayload(Guid Id, string Email);

    private sealed record OrderStatusPayload(string Status);

    private sealed record ReviewSummaryPayload(Guid ProductId, int ApprovedReviewCount, decimal AverageRating);

    private sealed record ReviewPayload(Guid Id, bool IsVerifiedPurchase, Guid? VerifiedPurchaseOrderId);

    private sealed record ReviewPagePayload(IReadOnlyCollection<ReviewPayload> Items);

    private sealed record QuestionAnswerPayload(Guid Id, string Status, bool IsOfficialAnswer);

    private sealed record QuestionPayload(Guid Id, string Status, IReadOnlyCollection<QuestionAnswerPayload> Answers);

    private sealed record QuestionPagePayload(IReadOnlyCollection<QuestionPayload> Items);

    private sealed record ReviewReportPayload(Guid ReviewId, string ReasonType);

    private sealed record ReviewReportPagePayload(IReadOnlyCollection<ReviewReportPayload> Items);

    private static async Task WaitForConditionAsync(Func<Task<bool>> predicate, string timeoutMessage)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail(timeoutMessage);
    }
}
