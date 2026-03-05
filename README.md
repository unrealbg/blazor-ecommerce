# Blazor E-Commerce Modular Monolith Skeleton (.NET 9)

Production-oriented modular monolith starter with strict module boundaries and clean architecture per module.

## Modules in this pass

- Catalog
- Cart
- Orders

## Architecture

- **Host**: `src/AppHost` (ASP.NET Core minimal API)
- **Building blocks**:
  - `BuildingBlocks.Domain` (domain primitives, `Result`, `Money`, `IClock`)
  - `BuildingBlocks.Application` (CQRS abstractions, handlers, validation pipeline)
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
- Cross-module communication is event-driven through shared contracts/domain events (outbox + dispatcher).

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

- .NET SDK 10.x (or newer SDK that can target `net10.0`)
- Docker + Docker Compose

## Run

### Option 1: Full stack with Docker Compose (postgres + redis + app)

```bash
docker compose up --build
```

App: `http://localhost:8080`
Redis (host port): `16379`

### Option 2: Infra in containers, app from `dotnet`

```bash
docker compose up -d postgres redis
dotnet run --project src/AppHost/AppHost.csproj
```

## Health Endpoints

- Liveness: `GET /health/live`
- Readiness (DB): `GET /health/ready`

## API Route Shape

- Versioned base path: `/api/v1/{module}`
- Examples:
  - `GET /api/v1/catalog/products`
  - `POST /api/v1/cart`
  - `POST /api/v1/cart/{cartId}/checkout`
  - `GET /api/v1/orders`

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

1. A module aggregate raises a domain event.
2. On `SaveChanges`, module `DbContext` captures domain events and writes `shared.outbox_messages` in the same transaction.
3. `OutboxDispatcherBackgroundService` polls unprocessed messages.
4. `IOutboxPublisher` deserializes events and dispatches them to registered in-process `IDomainEventHandler<T>`.
5. Dispatcher marks outbox rows as processed (or stores error details).

This keeps module boundaries strict while enabling reliable eventual consistency in-process.

