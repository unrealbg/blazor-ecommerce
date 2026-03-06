# Blazor E-Commerce Modular Monolith Skeleton (.NET 10)

Production-oriented modular monolith starter with strict module boundaries and clean architecture per module.

## Modules in this pass

- Catalog
- Cart
- Orders
- Payments
- Inventory
- Customers
- Redirects
- Search
- Storefront.Web (SSR Blazor UI)

## Architecture

- **Host**: `src/AppHost` (ASP.NET Core minimal API)
- **Storefront**: `src/Storefront/Storefront.Web` (Blazor Web App, SSR + interactive components)
- **Building blocks**:
  - `BuildingBlocks.Domain` (domain primitives, `Result`, `Money`, `IClock`)
  - `BuildingBlocks.Application` (CQRS abstractions, handlers, validation pipeline, cross-module contracts)
  - `BuildingBlocks.Infrastructure` (EF infrastructure, outbox, dispatcher, module infrastructure loader)
- **Each module**:
  - `*.Domain`
  - `*.Application`
  - `*.Infrastructure`
  - `*.Api`

## Boundary Rules Enforced

- `AppHost` references only `BuildingBlocks.*` and `*.Api`.
- `*.Api` references only `*.Application` + `BuildingBlocks.*`.
- `*.Application` references only `*.Domain` + `BuildingBlocks.*`.
- `*.Infrastructure` references `*.Application` + `*.Domain` + `BuildingBlocks.Infrastructure`.
- No direct module-to-module domain/infrastructure references.
- Cross-module communication is event-driven through contracts + outbox dispatcher.

## Persistence

- PostgreSQL + EF Core
- One physical DB, multiple schemas:
  - `catalog`
  - `cart`
  - `orders`
  - `inventory`
  - `customers`
  - `identity`
  - `redirects`
  - `search`
  - `shared` (outbox)
- Migrations are separated by bounded context:
  - `src/Modules/*/*Infrastructure/Persistence/Migrations`
  - `src/BuildingBlocks/BuildingBlocks.Infrastructure/Persistence/Migrations` (shared outbox)

## Prerequisites

- .NET SDK 10.x
- Docker + Docker Compose

## Run

### Option 1: Full stack with Docker Compose (postgres + redis + app + storefront)

```bash
docker compose up --build
```

App API: `http://localhost:8080`
Storefront: `http://localhost:5100`
Directus CMS Admin: `http://localhost:8055`

### Option 1B: Start storefront UI from source

```bash
dotnet run --project src/Storefront/Storefront.Web/Storefront.Web.csproj
```

Storefront: `http://localhost:5100` (default in appsettings)

Storefront configuration:

- `Api:BaseUrl` -> AppHost API base URL (default `http://localhost:8080`)
- `Cms:BaseUrl` -> Directus base URL (default `http://localhost:8055`)
- `Cms:ApiToken` -> Directus static API token used for content read access
- `ConnectionStrings:Redis` -> Redis connection used for CMS response cache (60s default)
- `Site:BaseUrl` -> absolute public storefront URL used for canonical/sitemap/robots/rss
- `Media:*` -> media proxy allowlist, transform quality, fetch timeout, and disk cache settings

### Option 2: Infra in containers, app from `dotnet`

```bash
docker compose up -d postgres redis directus-db directus
dotnet run --project src/AppHost/AppHost.csproj
dotnet run --project src/Storefront/Storefront.Web/Storefront.Web.csproj
```

## Health Endpoints

- Liveness: `GET /health/live`
- Readiness (DB): `GET /health/ready`

## API Routes (v1)

- `POST /api/v1/catalog/products`
- `GET /api/v1/catalog/products`
- `GET /api/v1/catalog/products/by-slug/{slug}`
- `PATCH /api/v1/catalog/products/{productId}/slug`
- `POST /api/v1/cart/{customerId}/items`
- `GET /api/v1/cart/{customerId}`
- `PATCH /api/v1/cart/{customerId}/items/{productId}`
- `DELETE /api/v1/cart/{customerId}/items/{productId}`
- `POST /api/v1/orders/checkout/{customerId}`
- `POST /api/v1/orders/checkout`
- `GET /api/v1/orders/{orderId}`
- `GET /api/v1/orders/my`
- `GET /api/v1/customers/me`
- `PUT /api/v1/customers/me`
- `GET /api/v1/customers/me/addresses`
- `POST /api/v1/customers/me/addresses`
- `PUT /api/v1/customers/me/addresses/{addressId}`
- `DELETE /api/v1/customers/me/addresses/{addressId}`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `GET /api/v1/auth/verify-email?userId={guid}&token={token}`
- `POST /api/v1/redirects`
- `GET /api/v1/redirects?page=1&pageSize=20`
- `PUT /api/v1/redirects/{redirectRuleId}/deactivate`
- `GET /api/v1/redirects/resolve?path=/blog/old-slug`
- `GET /api/v1/search/products?q=&categorySlug=&brand=&minPrice=&maxPrice=&inStock=&sort=&page=&pageSize=`
- `GET /api/v1/search/suggest?q=&limit=`
- `POST /api/v1/search/rebuild`
- `GET /api/v1/inventory/products/{productId}`
- `POST /api/v1/inventory/products/{productId}/adjust` (authorized)
- `GET /api/v1/inventory/products/{productId}/movements?page=&pageSize=` (authorized)
- `GET /api/v1/inventory/reservations/active?productId=&page=&pageSize=` (authorized)
- `POST /api/v1/payments/intents` (requires `Idempotency-Key`)
- `GET /api/v1/payments/intents/{id}`
- `GET /api/v1/payments/intents/by-order/{orderId}`
- `POST /api/v1/payments/intents/{id}/confirm` (requires `Idempotency-Key`)
- `POST /api/v1/payments/intents/{id}/cancel`
- `POST /api/v1/payments/intents/{id}/refund` (authorized)
- `GET /api/v1/payments/intents?page=&pageSize=&provider=&status=` (authorized)
- `POST /api/v1/payments/webhooks/{provider}`
- `POST /api/webhooks/directus`

## Storefront Routes

- `GET /` (Home, SSR)
- `GET /category/{slug}` (Category, SSR)
- `GET /product/{slug}` (Product, SSR)
- `GET /search?q=...` (Search, SSR)
- `GET /blog` (Blog index, SSR)
- `GET /blog/{slug}` (Blog post, SSR)
- `GET /p/{slug}` (Landing page, SSR)
- `GET /cart` (interactive)
- `GET /checkout` (interactive)
- `GET /checkout/payment` (interactive)
- `GET /checkout/success`
- `GET /checkout/failure`
- `GET /account` (profile, interactive)
- `GET /account/login` (interactive)
- `GET /account/register` (interactive)
- `GET /account/orders` (interactive)
- `GET /account/orders/{orderId}` (interactive)
- `GET /account/addresses` (interactive)
- `GET /admin/redirects` (admin redirect management UI)
- `GET /admin/inventory` (admin inventory dashboard)
- `GET /admin/inventory/{productId}` (admin inventory details)
- `GET /admin/payments` (admin payments list)
- `GET /admin/payments/{paymentIntentId}` (admin payment details + refund action)
- `GET /media/image?src=...&w=...&h=...&fit=max|cover|contain&format=auto|webp|avif|jpeg|png`
- `GET /robots.txt`
- `GET /sitemap.xml`
- `GET /rss.xml`

## Storefront SEO Notes (SSR)

- All public storefront routes are SSR (`/`, `/category/{slug}`, `/product/{slug}`, `/search`, `/blog`, `/blog/{slug}`, `/p/{slug}`) and return indexable HTML.
- Per page head tags are rendered with:
  - `<title>`
  - `<meta name="description">`
  - `<link rel="canonical">`
  - `<link rel="prev">` and `<link rel="next">` for paginated pages
  - `<meta name="robots" content="noindex,follow">` for low-value filtered/search pages and CMS content with `no_index=true`
  - OpenGraph + Twitter basic tags
- Canonical and sitemap absolute URLs are generated from `Site:BaseUrl`.
- `robots.txt` allows all crawlers and points to `/sitemap.xml`.
- `sitemap.xml` is generated dynamically and includes `/`, `/blog`, active EUR product URLs, published blog post URLs, and landing page URLs where `no_index=false`.
- `rss.xml` is generated from published blog posts only.

### Canonical Rules

- Tracking parameters are never canonicalized (`utm_*`, `gclid`, `fbclid`, etc.).
- Category canonical:
  - `page=1` -> `/category/{slug}`
  - `page>1` -> `/category/{slug}?page=N`
  - filtered category URLs (`brand`, `minPrice`, `maxPrice`, `inStock`, `sort`, `pageSize`) are `noindex,follow` and canonicalized to clean category URL (plus `page` only when `>1`)
- Search canonical:
  - always `noindex,follow`
  - keeps only `q` and `page` (`page` only when `>1`)
  - excludes `categorySlug`, `brand`, `minPrice`, `maxPrice`, `inStock`, `sort`, `pageSize`
  - `page=1` -> `/search?q=...`
  - `page>1` -> `/search?q=...&page=N`
- Blog/Page canonical:
  - if CMS `canonicalUrl` exists, it is used
  - otherwise canonical falls back to `SiteBaseUrl + current path`

### Structured Data (JSON-LD)

- Home: `WebSite` + `SearchAction`
- Category/Search: `BreadcrumbList`
- Product:
  - `Product` with `name`, `description`, optional `sku`, optional `brand`, optional `image`
  - `offers` with EUR price, availability, canonical product URL, and `NewCondition`
  - `BreadcrumbList`
- Blog post:
  - `BlogPosting` with headline, description, image, published/modified dates, author, and canonical `mainEntityOfPage`
  - `BreadcrumbList`

### SiteBaseUrl Configuration

- Configure `Site:BaseUrl` to the public storefront origin used by crawlers (for example `https://shop.example.com`).
- In development:

```json
{
  "Site": {
    "BaseUrl": "http://localhost:5100"
  }
}
```

## Media Pipeline (SEO + Performance)

The storefront now serves public image URLs through an internal media proxy endpoint:

- `GET /media/image?src={url}&w=1200&h=630&fit=cover&format=webp`

### Why this exists

- avoid exposing fragile raw CMS URLs in public HTML
- centralize image policy (security, format, dimensions, cache)
- improve page weight and Core Web Vitals with responsive image variants
- keep `og:image`, JSON-LD image URLs and rendered `<img>` sources stable and absolute

### Security policy

- only `http`/`https` sources are accepted
- host must be in `Media:AllowedHosts`
- private/loopback sources are rejected unless explicitly allowlisted
- unsupported schemes (`file://`, `ftp://`, etc.) are rejected
- source fetch size is limited by `Media:MaxSourceBytes`
- source fetch timeout is limited by `Media:FetchTimeoutSeconds`

### Transform behavior

- resize supports `w`, `h`, `fit=max|cover|contain`
- metadata is stripped from transformed images
- output formats:
  - `auto` -> prefers modern format when possible, then falls back safely
  - `webp`, `jpeg`, `png` are supported explicitly
  - `avif` parameter is accepted; output currently degrades safely when AVIF encoding is unavailable in runtime

### Cache behavior

- browser cache headers:
  - `Cache-Control: public, max-age=31536000, immutable`
  - `ETag` returned for conditional requests
- transformed image binaries are cached on disk under `Media:CachePath` (default `cache/media`)
- cache files are hashed by source + transform parameters + quality/version

### Clear media cache

- stop app and delete the cache directory (default `src/Storefront/Storefront.Web/cache/media` or your configured `Media:CachePath`)

### Appsettings example

```json
{
  "Media": {
    "AllowedHosts": [
      "localhost:8055",
      "localhost:5100"
    ],
    "CachePath": "cache/media",
    "DefaultQualityJpeg": 82,
    "DefaultQualityWebp": 80,
    "DefaultQualityAvif": 55,
    "EnableAvif": true,
    "MaxSourceBytes": 20971520,
    "FetchTimeoutSeconds": 10,
    "AllowUpscale": false
  }
}
```

### OG / Structured Data image policy

- Product, blog post and landing page metadata now use absolute proxied image URLs (`/media/image...`).
- Product and BlogPosting JSON-LD image fields now use proxied absolute image URLs.
- default fallback image is `wwwroot/images/og-default.jpg` (served through media proxy for SEO tags).

## Directus CMS Setup (Self-Hosted)

1. Start services:

```bash
docker compose up -d directus-db directus
```

2. Open Directus Admin: `http://localhost:8055`
3. Set bootstrap env vars before first run:
   - `DIRECTUS_KEY`
   - `DIRECTUS_SECRET`
   - `ADMIN_EMAIL`
   - `ADMIN_PASSWORD`
   Example:

```bash
set DIRECTUS_KEY=replace-me
set DIRECTUS_SECRET=replace-me
set ADMIN_EMAIL=admin@example.com
set ADMIN_PASSWORD=StrongPassword123!
```
4. Login with `ADMIN_EMAIL` / `ADMIN_PASSWORD`.
5. Create content collections:
   - `blog_posts`
   - `pages`
6. Add `blog_posts` fields (snake_case):
   - `status` (draft/published)
   - `title` (string)
   - `slug` (string, unique)
   - `excerpt` (string, recommended 160-240 chars)
   - `content` (text/markdown)
   - `cover_image_url` (string, optional)
   - `author_name` (string)
   - `published_at` (datetime)
   - `updated_at` (datetime)
   - `tags` (json array of strings)
   - `seo_title` (string, optional)
   - `seo_description` (string, optional)
   - `canonical_url` (string, optional)
   - `no_index` (boolean, default false)
7. Add `pages` fields (snake_case):
   - `status` (draft/published)
   - `title` (string)
   - `slug` (string, unique)
   - `content` (text/markdown)
   - `updated_at` (datetime)
   - `seo_title` (string, optional)
   - `seo_description` (string, optional)
   - `canonical_url` (string, optional)
   - `no_index` (boolean, default false)
8. Directus permissions:
   - Public role: READ only `status = published`
   - Prefer static API token for app access (instead of anonymous read)
9. Generate static token in Directus and set `Cms:ApiToken`.

Example config:

```json
{
  "Cms": {
    "BaseUrl": "http://localhost:8055",
    "ApiToken": "DIRECTUS_STATIC_TOKEN",
    "CacheSeconds": 60
  }
}
```

## Sitemap and RSS

- `/sitemap.xml` includes:
  - `/`
  - `/blog`
  - `/product/{slug}` for active EUR products
  - `/blog/{slug}` for published posts where `no_index=false`
  - `/p/{slug}` for pages where `status=published` and `no_index=false`
- `/rss.xml` includes published blog posts only.

## Sample End-to-End Flow (curl)

### 1) Create product in Catalog

```bash
curl -X POST http://localhost:8080/api/v1/catalog/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Keyboard",
    "brand": "Contoso",
    "sku": "KEY-001",
    "imageUrl": "/images/keyboard.png",
    "isInStock": true,
    "categorySlug": "keyboards",
    "categoryName": "Keyboards",
    "description": "Mechanical keyboard",
    "currency": "EUR",
    "amount": 99.99,
    "isActive": true
  }'
```

### 2) List products

```bash
curl http://localhost:8080/api/v1/catalog/products
```

### 3) Add product to Cart

```bash
curl -X POST http://localhost:8080/api/v1/cart/customer-123/items \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "PUT_PRODUCT_ID_HERE",
    "quantity": 2
  }'
```

```bash
curl http://localhost:8080/api/v1/cart/customer-123
```

### 4) Checkout: create Order from Cart (guest flow)

```bash
curl -X POST http://localhost:8080/api/v1/orders/checkout \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: checkout-customer-123-001" \
  -d '{
    "cartSessionId": "customer-123",
    "email": "guest@example.com",
    "shippingAddress": {
      "firstName": "John",
      "lastName": "Doe",
      "street": "Main St 1",
      "city": "Sofia",
      "postalCode": "1000",
      "country": "BG",
      "phone": "+359888000111"
    },
    "billingAddress": {
      "firstName": "John",
      "lastName": "Doe",
      "street": "Main St 1",
      "city": "Sofia",
      "postalCode": "1000",
      "country": "BG",
      "phone": "+359888000111"
    }
  }'
```

### 5) Fetch created order

```bash
curl http://localhost:8080/api/v1/orders/PUT_ORDER_ID_HERE
```

### 6) Create payment intent for the order

```bash
curl -X POST http://localhost:8080/api/v1/payments/intents \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: payment-order-001-create" \
  -d '{
    "orderId": "PUT_ORDER_ID_HERE",
    "provider": "Demo",
    "customerEmail": "guest@example.com"
  }'
```

### 7) Confirm payment intent (when status is Pending/RequiresAction)

```bash
curl -X POST http://localhost:8080/api/v1/payments/intents/PUT_PAYMENT_INTENT_ID_HERE/confirm \
  -H "Idempotency-Key: payment-order-001-confirm"
```

### 8) Simulate provider webhook (Demo)

```bash
curl -X POST http://localhost:8080/api/v1/payments/webhooks/Demo \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": "evt-demo-001",
    "eventType": "payment.succeeded",
    "providerPaymentIntentId": "PUT_PROVIDER_PAYMENT_INTENT_ID_HERE",
    "status": "Captured"
  }'
```

## Checkout Idempotency

- Checkout endpoints require `Idempotency-Key` header.
- Reusing the same key for the same customer returns the same `orderId` and does not create a duplicate order.
- Reusing the same key for a different customer returns a business error.
- Storefront checkout (`/checkout`) sends an `Idempotency-Key` automatically per submit.
- Preferred endpoint for storefront and guest/account checkout is `POST /api/v1/orders/checkout` with request body (`cartSessionId`, `email`, shipping/billing snapshots).
- Legacy endpoint `POST /api/v1/orders/checkout/{customerId}` remains available for compatibility.

## Payments

- New bounded context:
  - `src/Modules/Payments/Payments.Domain`
  - `src/Modules/Payments/Payments.Application`
  - `src/Modules/Payments/Payments.Infrastructure`
  - `src/Modules/Payments/Payments.Api`
- Schema: `payments`
- Core persistence:
  - `payment_intents`
  - `payment_transactions`
  - `webhook_inbox_messages`
  - `payment_idempotency_records`

### Architecture

- Domain is provider-agnostic (`PaymentIntent`, `PaymentTransaction`, webhook inbox entity, internal status transitions).
- Provider integration is abstracted via:
  - `IPaymentProvider`
  - `IPaymentProviderFactory`
  - `IPaymentWebhookVerifier`
- Infrastructure currently provides:
  - `DemoPaymentProvider` (fully functional for local dev)
  - `StripePaymentProvider` skeleton (not active by default)

### Payment intent lifecycle

- Supported statuses:
  - `Created`, `Pending`, `RequiresAction`, `Authorized`, `Captured`, `Failed`, `Cancelled`, `Refunded`, `PartiallyRefunded`
- Typical local flow (`Demo`, auto-capture enabled):
  1. checkout creates order in `PendingPayment`
  2. create payment intent
  3. provider returns `Captured`
  4. `PaymentCaptured` event marks order as `Paid` and consumes inventory reservations

### Idempotency behavior

- `POST /api/v1/payments/intents` requires `Idempotency-Key`.
- `POST /api/v1/payments/intents/{id}/confirm` requires `Idempotency-Key`.
- Same operation + same key returns the original payment intent result and prevents duplicates.
- Webhooks are deduplicated by unique `(Provider, ExternalEventId)` in `webhook_inbox_messages`.

### Webhook processing

- Endpoint: `POST /api/v1/payments/webhooks/{provider}`
- Flow:
  1. verify (configurable; demo mode can skip strict verification)
  2. persist raw payload in inbox
  3. parse via provider adapter
  4. map to payment intent status + append payment transaction
  5. publish payment domain events through outbox
- Replay-safe:
  - duplicate event id is acknowledged and ignored (`processed=false` response payload).

### Demo provider configuration

```json
{
  "Payments": {
    "DefaultProvider": "Demo",
    "PendingPaymentReservationHoldMinutes": 15,
    "WebhookProcessingRetryCount": 3,
    "RequireWebhookVerification": false,
    "Demo": {
      "AutoCaptureOnCreate": true,
      "SimulateRequiresAction": false,
      "SimulateFailureRate": 0.0
    },
    "Stripe": {
      "SecretKey": "",
      "PublishableKey": "",
      "WebhookSecret": ""
    }
  }
}
```

### Updated order/inventory policy

- Reserve stock in cart.
- Checkout creates order in `PendingPayment` and promotes cart reservations to order-level reservations.
- Inventory is consumed on `PaymentCaptured` (not on order creation).
- On `PaymentFailed` or `PaymentCancelled`, active order reservations are released.
- Refund emits payment events and updates order status (`Refunded` / `PartiallyRefunded`); stock restock is intentionally left as future extension hook.

### Example

```bash
# First call creates an order
curl -X POST http://localhost:8080/api/v1/orders/checkout \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: checkout-customer-123-001" \
  -d '{"cartSessionId":"customer-123","email":"guest@example.com","shippingAddress":{"firstName":"John","lastName":"Doe","street":"Main St 1","city":"Sofia","postalCode":"1000","country":"BG","phone":"+359888000111"},"billingAddress":{"firstName":"John","lastName":"Doe","street":"Main St 1","city":"Sofia","postalCode":"1000","country":"BG","phone":"+359888000111"}}'
```

```bash
# Second call with the same key returns the same order id
curl -X POST http://localhost:8080/api/v1/orders/checkout \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: checkout-customer-123-001" \
  -d '{"cartSessionId":"customer-123","email":"guest@example.com","shippingAddress":{"firstName":"John","lastName":"Doe","street":"Main St 1","city":"Sofia","postalCode":"1000","country":"BG","phone":"+359888000111"},"billingAddress":{"firstName":"John","lastName":"Doe","street":"Main St 1","city":"Sofia","postalCode":"1000","country":"BG","phone":"+359888000111"}}'
```

## Inventory & Reservations

- New bounded context:
  - `src/Modules/Inventory/Inventory.Domain`
  - `src/Modules/Inventory/Inventory.Application`
  - `src/Modules/Inventory/Inventory.Infrastructure`
  - `src/Modules/Inventory/Inventory.Api`
- Schema: `inventory`
- Core model:
  - `StockItem` (`OnHandQuantity`, `ReservedQuantity`, computed available, tracked/backorder flags, `RowVersion`)
  - `StockReservation` (`Active`, `Consumed`, `Released`, `Expired`, expiration timestamp, reservation token)
  - `StockMovement` ledger (`ReservationCreated`, `ReservationReleased`, `ReservationConsumed`, `ReservationExpired`, `Restock`, `ManualAdjustment`, `OrderCompleted`)

### Reservation lifecycle

- Reservations are created/updated on cart mutation through `IInventoryReservationService`.
- Quantity changes in cart synchronize reservation quantity.
- Removing item (or reducing to zero) releases reservation.
- Reservation expiration is handled by `ReservationExpirationWorker` (background sweep).
- Checkout validates active reservations and consumes them.

### Current inventory policy

- Reserve in cart.
- Checkout creates order in `PendingPayment` and moves reservation ownership to the order.
- Consume on successful payment capture.
- Consumption decrements:
  - `ReservedQuantity`
  - `OnHandQuantity`
- Payment failure/cancellation releases active order reservations.

### Oversell protection

- `StockItem.RowVersion` is used as optimistic concurrency token.
- Inventory write paths execute with retry (`InventoryDbContext.ExecuteWithConcurrencyRetryAsync`).
- On repeated conflicts, API returns business conflict:
  - `inventory.stock.concurrency_conflict`

### Search/storefront stock behavior

- Catalog API now enriches products with inventory availability (`IsTracked`, `AllowBackorder`, `AvailableQuantity`).
- Storefront product page shows stock status:
  - In stock
  - Out of stock
  - Backorder available
- Add-to-cart is disabled when item is tracked, out of stock, and backorder disabled.
- Stock availability domain event updates Search read model `IsInStock`.

### Admin inventory operations

- `GET /api/v1/inventory/products/{productId}` (public read)
- `POST /api/v1/inventory/products/{productId}/adjust` (authorized)
- `GET /api/v1/inventory/products/{productId}/movements` (authorized)
- `GET /api/v1/inventory/reservations/active` (authorized)
- Storefront admin UI:
  - `/admin/inventory`
  - `/admin/inventory/{productId}`

### Inventory configuration

```json
{
  "Inventory": {
    "ReservationTtlMinutes": 30,
    "ExpirationSweepSeconds": 60,
    "RefreshReservationOnCartMutation": true,
    "RetryOnConcurrencyCount": 3,
    "ExposeExactStockPublicly": false
  }
}
```

## Customers and Identity

- New bounded context:
  - `src/Modules/Customers/Customers.Domain`
  - `src/Modules/Customers/Customers.Application`
  - `src/Modules/Customers/Customers.Infrastructure`
  - `src/Modules/Customers/Customers.Api`
- Schemas:
  - `customers` for customer profiles, addresses, sessions
  - `identity` for ASP.NET Core Identity (`ApplicationUser : IdentityUser<Guid>`)
- Customer profile:
  - unique email (`NormalizedEmail` indexed)
  - optional link to identity user (`Customer.UserId`)
  - soft deactivation support
- Addresses:
  - multiple addresses per customer
  - one default shipping and one default billing (enforced by filtered unique indexes)
- Guest checkout:
  - checkout with email creates or reuses a guest customer profile
  - no identity user is required for guest orders
  - orders store immutable shipping/billing snapshots (no foreign keys)
- Auth model:
  - cookie auth configured for SSR flows
  - JWT bearer config is present as baseline placeholder
  - protected APIs require authentication (`/api/v1/customers/*`, `/api/v1/orders/my`)
- Session cache:
  - customer session and cart session cached in Redis with 24h TTL
  - graceful fallback when Redis is unavailable
- Domain events emitted through outbox:
  - `CustomerRegistered`
  - `CustomerAddressAdded`
  - `CustomerAddressUpdated`
  - `CustomerLoggedIn`

### Auth and Account API examples

```bash
curl -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"P@ssw0rd123!","firstName":"Alice","lastName":"Doe","phoneNumber":"+359888000111"}'
```

```bash
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"P@ssw0rd123!","rememberMe":true}'
```

```bash
curl http://localhost:8080/api/v1/customers/me
```

```bash
curl -X POST http://localhost:8080/api/v1/customers/me/addresses \
  -H "Content-Type: application/json" \
  -d '{"label":"Home","firstName":"Alice","lastName":"Doe","company":null,"street1":"Main St 1","street2":null,"city":"Sofia","postalCode":"1000","countryCode":"BG","phone":"+359888000111","isDefaultShipping":true,"isDefaultBilling":true}'
```

```bash
curl http://localhost:8080/api/v1/orders/my
```

### Storefront account pages

- `/account`
- `/account/login`
- `/account/register`
- `/account/profile`
- `/account/addresses`
- `/account/orders`

## SEO-Safe Redirects and Slug History

- Redirects are stored in `redirects.redirect_rules` and normalized to lowercase paths.
- The app applies redirect middleware early in the request pipeline.
- Rules are resolved from Redis hash cache (`redirects:rules`) with DB fallback and in-memory fallback.
- Redirect hit counters (`hit_count`, `last_hit_at`) are written asynchronously by a background queue.
- Query string behavior:
  - Matching ignores query string.
  - Incoming query params are preserved on redirect.
  - If target already has query params, they are safely merged.
- Loop safety:
  - Redirect is skipped when normalized source and target paths are equal.

### Manual Redirect Management

- API:
  - `POST /api/v1/redirects`
  - `GET /api/v1/redirects?page=1&pageSize=20`
  - `PUT /api/v1/redirects/{redirectRuleId}/deactivate`
- UI:
  - `GET /admin/redirects`

### Automatic Redirect Creation

- Catalog product slug changes:
  - Update product slug via `PATCH /api/v1/catalog/products/{productId}/slug`.
  - `ProductSlugChanged` domain event is written to outbox with product update transaction.
  - Outbox dispatcher runs `ProductSlugChangedDomainEventHandler`, which creates redirect:
    - `/product/{oldSlug}` -> `/product/{newSlug}`
- Directus content slug changes:
  - Configure Directus webhook to call `POST /api/webhooks/directus` on updates.
  - Supported collections:
    - `blog_posts`: `/blog/{oldSlug}` -> `/blog/{newSlug}`
    - `pages`: `/p/{oldSlug}` -> `/p/{newSlug}`

### Directus Webhook Payload

- Minimal payload accepted by the endpoint:

```json
{
  "collection": "blog_posts",
  "event": "items.update",
  "oldSlug": "old-slug",
  "newSlug": "new-slug"
}
```

- The endpoint also supports `data.slug` and `previous.slug` fields.

## Search and Faceted Filtering

- Search is implemented as a dedicated read-model module (`src/Modules/Search`) with provider abstraction:
  - `ISearchProvider` in Application
  - `PostgresSearchProvider` in Infrastructure
- Read model table:
  - `search.product_search_documents`
  - denormalized searchable product data (`slug`, `name`, `description`, `brand`, `category`, `price`, `stock`, `active`, `image`, timestamps)
- PostgreSQL search-related setup:
  - `pg_trgm` extension enabled in Search migration
  - B-tree indexes on filter/sort fields (`category_slug`, `brand`, `price_amount`, `is_active`, `is_in_stock`, `slug`, `normalized_name`)
  - GIN expression index for full-text vector (`name + description + brand + category`)
  - GIN trigram index on product name
  - `PostgresSearchProvider` uses `websearch_to_tsquery` + trigram similarity for query ranking/suggestions

### Indexing Flow

- Catalog publishes domain events into the shared outbox.
- Search index is updated from event handlers via `IProductSearchIndexer`:
  - `ProductCreated` -> upsert search document
  - `ProductSlugChanged` -> upsert search document
- Rebuild API:
  - `POST /api/v1/search/rebuild`
  - pulls products through `IProductCatalogReader`
  - re-syncs full search read model

### Storefront Search UX

- SSR listing routes:
  - `/search`
  - `/category/{slug}`
- Facets supported:
  - brand
  - category (on search results)
  - in-stock
  - price range (min/max)
- Sorting supported:
  - `relevance`, `popular`, `newest`, `price_asc`, `price_desc`, `name_asc`
- Autocomplete:
  - header search box calls `GET /api/v1/search/suggest`
  - debounced suggestions, links to `/product/{slug}`
- Analytics hooks (structured logs only, no external provider yet):
  - search performed
  - zero results
  - filters applied
  - suggestion clicked

### Future Path

- Search module is isolated behind `ISearchProvider`.
- Replacing PostgreSQL implementation with OpenSearch/Elasticsearch later requires a new provider implementation and DI switch, without changing Storefront/Search API contracts.

## Add a Migration

### Shared outbox (schema `shared`)

```bash
dotnet ef migrations add <MigrationName> \
  --project src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj \
  --context BuildingBlocks.Infrastructure.Persistence.OutboxDbContext \
  --output-dir Persistence/Migrations
```

### Catalog

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj \
  --context Catalog.Infrastructure.Persistence.CatalogDbContext \
  --output-dir Persistence/Migrations
```

### Cart

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Cart/Cart.Infrastructure/Cart.Infrastructure.csproj \
  --context Cart.Infrastructure.Persistence.CartDbContext \
  --output-dir Persistence/Migrations
```

### Orders

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Orders/Orders.Infrastructure/Orders.Infrastructure.csproj \
  --context Orders.Infrastructure.Persistence.OrdersDbContext \
  --output-dir Persistence/Migrations
```

### Redirects

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Redirects/Redirects.Infrastructure/Redirects.Infrastructure.csproj \
  --context Redirects.Infrastructure.Persistence.RedirectsDbContext \
  --output-dir Persistence/Migrations
```

### Search

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Search/Search.Infrastructure/Search.Infrastructure.csproj \
  --startup-project src/Modules/Search/Search.Infrastructure/Search.Infrastructure.csproj \
  --context Search.Infrastructure.Persistence.SearchDbContext \
  --output-dir Persistence/Migrations
```

### Customers

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Customers/Customers.Infrastructure/Customers.Infrastructure.csproj \
  --startup-project src/Modules/Customers/Customers.Infrastructure/Customers.Infrastructure.csproj \
  --context Customers.Infrastructure.Persistence.CustomersDbContext \
  --output-dir Persistence/Migrations
```

### Identity

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Customers/Customers.Infrastructure/Customers.Infrastructure.csproj \
  --startup-project src/Modules/Customers/Customers.Infrastructure/Customers.Infrastructure.csproj \
  --context Customers.Infrastructure.Identity.IdentityAppDbContext \
  --output-dir Identity/Migrations
```

### Inventory

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj \
  --startup-project src/Modules/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj \
  --context Inventory.Infrastructure.Persistence.InventoryDbContext \
  --output-dir Persistence/Migrations
```

### Payments

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Payments/Payments.Infrastructure/Payments.Infrastructure.csproj \
  --startup-project src/Modules/Payments/Payments.Infrastructure/Payments.Infrastructure.csproj \
  --context Payments.Infrastructure.Persistence.PaymentsDbContext \
  --output-dir Persistence/Migrations
```

## Outbox Flow

1. Aggregates (Catalog/Orders/Payments/Inventory/etc.) raise domain events.
2. Module DbContext writes domain events to `shared.outbox_messages` in the same transaction as state changes.
3. `OutboxDispatcherBackgroundService` polls unprocessed outbox rows.
4. `IOutboxPublisher` deserializes and dispatches events to in-process `IDomainEventHandler<T>`.
5. Handlers execute cross-module side effects (for example payment capture -> mark order paid -> consume inventory reservations).
6. Dispatcher marks outbox rows as processed (or stores error details).

This keeps module boundaries strict while enabling reliable eventual consistency in-process.
