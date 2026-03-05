# Blazor E-Commerce Modular Monolith Skeleton (.NET 10)

Production-oriented modular monolith starter with strict module boundaries and clean architecture per module.

## Modules in this pass

- Catalog
- Cart
- Orders
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

### Option 1B: Start storefront UI from source

```bash
dotnet run --project src/Storefront/Storefront.Web/Storefront.Web.csproj
```

Storefront: `http://localhost:5100` (default in appsettings)

Storefront configuration:

- `Api:BaseUrl` -> AppHost API base URL (default `http://localhost:8080`)
- `Seo:SiteBaseUrl` -> absolute public storefront URL used for canonical/sitemap/robots

### Option 2: Infra in containers, app from `dotnet`

```bash
docker compose up -d postgres redis
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
- `POST /api/v1/cart/{customerId}/items`
- `GET /api/v1/cart/{customerId}`
- `PATCH /api/v1/cart/{customerId}/items/{productId}`
- `DELETE /api/v1/cart/{customerId}/items/{productId}`
- `POST /api/v1/orders/checkout/{customerId}`
- `GET /api/v1/orders/{orderId}`

## Storefront Routes

- `GET /` (Home, SSR)
- `GET /category/{slug}` (Category, SSR)
- `GET /product/{slug}` (Product, SSR)
- `GET /search?q=...` (Search, SSR)
- `GET /cart` (interactive)
- `GET /checkout` (interactive)
- `GET /robots.txt`
- `GET /sitemap.xml`

## Storefront SEO Notes

- Each SEO route sets non-empty title and meta description.
- Canonical URLs are generated from `Seo:SiteBaseUrl`.
- `robots.txt` allows all crawlers and points to `/sitemap.xml`.
- `sitemap.xml` is generated dynamically and includes `/` and active EUR product URLs.

## Sample End-to-End Flow (curl)

### 1) Create product in Catalog

```bash
curl -X POST http://localhost:8080/api/v1/catalog/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Keyboard",
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

## Outbox Flow

1. `Order` aggregate raises `Orders.Domain.Events.OrderPlaced`.
2. `OrdersDbContext.SaveChanges` captures domain events and writes them to `shared.outbox_messages` in the same transaction as the order write.
3. `OutboxDispatcherBackgroundService` polls unprocessed outbox rows.
4. `IOutboxPublisher` deserializes and dispatches events to in-process `IDomainEventHandler<T>`.
5. `OrderPlacedDomainEventHandler` logs `OrderPlaced handled...` and writes an `orders.order_audits` record to prove side-effects executed.
6. Dispatcher marks outbox rows as processed (or stores error details).

This keeps module boundaries strict while enabling reliable eventual consistency in-process.
