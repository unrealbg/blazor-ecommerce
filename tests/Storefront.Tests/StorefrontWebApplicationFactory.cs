using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Storefront.Web.Services.Api;

namespace Storefront.Tests;

public sealed class StorefrontWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Api:BaseUrl", "http://localhost:8080"),
                new KeyValuePair<string, string?>("Seo:SiteBaseUrl", "https://shop.example.com"),
            ]);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStoreApiClient>();
            services.AddSingleton<IStoreApiClient, FakeStoreApiClient>();
        });
    }

    private sealed class FakeStoreApiClient : IStoreApiClient
    {
        private static readonly IReadOnlyCollection<StoreProduct> Products = BuildProducts();

        public Task<bool> AddItemToCartAsync(
            string customerId,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<Guid?> CheckoutAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(Guid.Parse("7e840d4c-4994-4993-b344-e8219be85656"));
        }

        public Task<StoreCart?> GetCartAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreCart?>(new StoreCart(Guid.NewGuid(), customerId, []));
        }

        public Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var product = Products.SingleOrDefault(item => string.Equals(item.Slug, slug, StringComparison.Ordinal));
            return Task.FromResult(product);
        }

        public Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<StoreProduct>>(Products);
        }

        public Task<bool> RemoveCartItemAsync(string customerId, Guid productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateCartItemQuantityAsync(
            string customerId,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        private static IReadOnlyCollection<StoreProduct> BuildProducts()
        {
            var products = new List<StoreProduct>
            {
                new(
                    Guid.Parse("6d4bf032-1b4f-4daa-8902-90f268cb378b"),
                    "mechanical-keyboard",
                    "Mechanical Keyboard",
                    "RGB mechanical keyboard for gamers.",
                    "Contoso",
                    "KEY-0001",
                    "/images/mechanical-keyboard.png",
                    true,
                    "keyboards",
                    "Keyboards",
                    "EUR",
                    89.00m,
                    true),
            };

            for (var index = 2; index <= 56; index++)
            {
                products.Add(
                    new StoreProduct(
                        Guid.NewGuid(),
                        $"keyboard-{index}",
                        $"Keyboard {index}",
                        $"Keyboard model {index} with tactile switches.",
                        "Contoso",
                        $"KEY-{index:0000}",
                        $"/images/keyboard-{index}.png",
                        true,
                        "keyboards",
                        "Keyboards",
                        "EUR",
                        70 + index,
                        true));
            }

            products.Add(
                new StoreProduct(
                    Guid.NewGuid(),
                    "wireless-mouse",
                    "Wireless Mouse",
                    "Compact wireless mouse.",
                    "Fabrikam",
                    "MOU-0001",
                    "/images/wireless-mouse.png",
                    true,
                    "mice",
                    "Mice",
                    "EUR",
                    39.00m,
                    true));

            return products;
        }
    }
}
