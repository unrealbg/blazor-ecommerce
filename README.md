# Blazor E-Commerce Modular Monolith Skeleton (.NET 10)

Production-oriented modular monolith starter with strict module boundaries and clean architecture per module.

## Modules in this pass

- Catalog
- Cart
- Orders
- Redirects
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
  - `redirects`
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
- `GET /api/v1/orders/{orderId}`
- `POST /api/v1/redirects`
- `GET /api/v1/redirects?page=1&pageSize=20`
- `PUT /api/v1/redirects/{redirectRuleId}/deactivate`
- `GET /api/v1/redirects/resolve?path=/blog/old-slug`
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
- `GET /admin/redirects` (admin redirect management UI)
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
  - `<meta name="robots" content="noindex,nofollow">` when CMS content has `no_index=true`
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
- Search canonical:
  - keeps only `q` and `page` (`page` only when `>1`)
  - excludes `sort` and `pageSize`
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

### 4) Checkout: create Order from Cart

```bash
curl -X POST http://localhost:8080/api/v1/orders/checkout/customer-123 \
  -H "Idempotency-Key: checkout-customer-123-001"
```

### 5) Fetch created order

```bash
curl http://localhost:8080/api/v1/orders/PUT_ORDER_ID_HERE
```

## Checkout Idempotency

- The checkout endpoint requires `Idempotency-Key` header.
- Reusing the same key for the same customer returns the same `orderId` and does not create a duplicate order.
- Reusing the same key for a different customer returns a business error.
- Storefront checkout (`/checkout`) sends an `Idempotency-Key` automatically per submit.

### Example

```bash
# First call creates an order
curl -X POST http://localhost:8080/api/v1/orders/checkout/customer-123 \
  -H "Idempotency-Key: checkout-customer-123-001"
```

```bash
# Second call with the same key returns the same order id
curl -X POST http://localhost:8080/api/v1/orders/checkout/customer-123 \
  -H "Idempotency-Key: checkout-customer-123-001"
```

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

## Outbox Flow

1. `Order` aggregate raises `Orders.Domain.Events.OrderPlaced`.
2. `OrdersDbContext.SaveChanges` captures domain events and writes them to `shared.outbox_messages` in the same transaction as the order write.
3. `OutboxDispatcherBackgroundService` polls unprocessed outbox rows.
4. `IOutboxPublisher` deserializes and dispatches events to in-process `IDomainEventHandler<T>`.
5. `OrderPlacedDomainEventHandler` logs `OrderPlaced handled...` and writes an `orders.order_audits` record to prove side-effects executed.
6. Dispatcher marks outbox rows as processed (or stores error details).

This keeps module boundaries strict while enabling reliable eventual consistency in-process.
