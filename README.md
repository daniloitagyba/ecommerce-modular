# ECommerce Modular Monolith

A modular monolith e-commerce application built with **.NET 10**, following **Domain-Driven Design (DDD)**, **Clean Architecture**, and **SOLID** principles.

## Architecture

The application is organized into three independent **Bounded Contexts**, each implemented as a self-contained module with its own domain, application layer, infrastructure, and endpoints:

```
src/
├── ECommerce.API/                    # Host — wires modules, middleware, DB init
├── ECommerce.Shared/                 # Shared kernel (Result, Entity, Value Objects, Pagination)
├── ECommerce.Modules.Catalog/        # Product & Category management
├── ECommerce.Modules.Ordering/       # Order placement & lifecycle
└── ECommerce.Modules.Billing/        # Payments & Invoices (async via Outbox)

tests/
├── ECommerce.Tests.Unit/             # 80 unit tests
├── ECommerce.Tests.Integration/      # 21 integration tests
└── ECommerce.Tests.EndToEnd/         # 18 end-to-end tests
```

### Module Internal Structure

Each module follows Clean Architecture:

```
Module/
├── Domain/            # Entities, Value Objects, Repository interfaces
├── Application/       # Commands, Queries, Handlers, Validators (CQRS via MediatR)
├── Infrastructure/    # DbContext, Repository implementations
├── Endpoints/         # Minimal API route mappings
└── ModuleRegistration # DI registration entry point
```

## Tech Stack

| Concern | Library |
|---|---|
| Framework | .NET 10 / C# (latest) |
| API | ASP.NET Core Minimal APIs |
| CQRS / Mediator | MediatR 12 |
| Validation | FluentValidation 11 |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL (via Npgsql) |
| Messaging | MassTransit (InMemory transport) |
| Background Jobs | Quartz.NET |
| Testing | xUnit, FluentAssertions, NSubstitute |
| Coverage | Coverlet + ReportGenerator |

## Design Patterns & Principles

- **Result Pattern** — `Result<T>` / `Error` instead of exceptions for flow control
- **Repository Pattern** — generic `IRepository<T>` with module-specific interfaces
- **Unit of Work** — module-scoped (`ICatalogUnitOfWork`, `IOrderingUnitOfWork`, `IBillingUnitOfWork`)
- **Value Objects** — `Money`, `Email`, `Sku` with factory validation
- **Domain Events** — `IDomainEvent` dispatched after `SaveChanges`
- **Transactional Outbox Pattern** — integration events saved to `OutboxMessages` table in the same ACID transaction
- **MassTransit Consumers** — async cross-module communication via `OrderCreatedIntegrationEvent`
- **Quartz.NET Background Job** — `ProcessOutboxJob` reads outbox, publishes to MassTransit bus
- **CQRS** — Commands (write) and Queries (read) separated through MediatR
- **Pagination** — `PagedRequest` / `PagedResult<T>` with `IQueryable` extension
- **Validation Pipeline** — `ValidationBehavior<TRequest, TResponse>` as MediatR pipeline behavior
- **SRP** — Event handlers split by responsibility (e.g., `ProcessPaymentOnOrderCreated`, `GenerateInvoiceOnOrderCreated`)
- **Dependency Inversion** — Handlers depend on abstractions, not concrete DbContext

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) (running on localhost:5432)

## Getting Started

```bash
# Clone the repository
git clone <repository-url>
cd ecommerce-modular

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/ECommerce.API
```

The API starts at `http://localhost:5225` (see `Properties/launchSettings.json`).

PostgreSQL database is created automatically on first run. Default connection: `Host=localhost;Port=5432;Database=ecommerce;Username=postgres;Password=postgres` (see `appsettings.json`).

## API Endpoints

### Catalog

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/catalog/categories` | Create a category |
| `GET` | `/api/catalog/products` | List products (paginated) |
| `GET` | `/api/catalog/products/{id}` | Get product by ID |
| `POST` | `/api/catalog/products` | Create a product |
| `PUT` | `/api/catalog/products/{id}/stock` | Update stock quantity |

### Ordering

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/orders` | Place an order |
| `GET` | `/api/orders` | List orders (paginated) |
| `GET` | `/api/orders/{id}` | Get order by ID |

### Billing

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/billing/payments/{orderId}` | Get payments for an order |
| `GET` | `/api/billing/invoices/{orderId}` | Get invoices for an order |

> Payments and invoices are created **asynchronously** when an order is placed, via the Transactional Outbox Pattern + Quartz.NET + MassTransit.

### Pagination

List endpoints accept optional query parameters:

```
GET /api/catalog/products?page=1&pageSize=10
```

Response shape:

```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPrevious": false,
  "hasNext": true
}
```

## Usage Examples

```bash
# Create a category
curl -X POST http://localhost:5225/api/catalog/categories \
  -H "Content-Type: application/json" \
  -d '{"name": "Electronics", "description": "Gadgets and devices"}'

# Create a product
curl -X POST http://localhost:5225/api/catalog/products \
  -H "Content-Type: application/json" \
  -d '{"name": "Laptop", "sku": "LAP-001", "price": 999.99, "stockQuantity": 50, "categoryId": "<category-id>"}'

# Place an order
curl -X POST http://localhost:5225/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerEmail": "john@example.com", "lines": [{"productId": "<product-id>", "productName": "Laptop", "unitPrice": 999.99, "quantity": 1}]}'

# Check payment was auto-created
curl http://localhost:5225/api/billing/payments/<order-id>
```

## Testing

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML coverage report (requires dotnet-reportgenerator-globaltool)
reportgenerator -reports:"coverage/*/coverage.cobertura.xml" -targetdir:coverage/report -reporttypes:Html
```

### Test Coverage

| Project | Tests | Method Coverage |
|---|---|---|
| Unit | 80 | Domain, Value Objects, Validators |
| Integration | 21 | Handlers with in-memory SQLite |
| End-to-End | 18 | Full HTTP pipeline via WebApplicationFactory |
| **Total** | **119** | **87.9%** |

Business modules achieve **97-100%** method coverage.

## Transactional Outbox Pattern

The billing flow uses the **Transactional Outbox Pattern** for reliable async processing:

```
1. PlaceOrder handler saves Order + OutboxMessage in the SAME transaction (ACID)
2. Quartz.NET job (ProcessOutboxJob) reads unprocessed OutboxMessages every 10s
3. Job deserializes the event and publishes to MassTransit InMemory bus
4. MassTransit consumer (OrderCreatedConsumer) creates Payment + Invoice in Billing module
5. OutboxMessage is marked as processed
```

This guarantees that no billing event is lost even if the application crashes after saving the order.

## Data Isolation

Each module has its own `DbContext` with a dedicated PostgreSQL schema (`catalog`, `ordering`, `billing`).

Cross-module communication happens exclusively through the outbox + MassTransit — modules never reference each other's internal types.

## Project Structure

```
ecommerce-modular/
├── ECommerce.slnx                    # Solution file
├── Directory.Build.props             # Shared MSBuild properties (net10.0)
├── src/
│   ├── ECommerce.API/                # Host application
│   │   ├── Program.cs               # Composition root
│   │   ├── DatabaseInitializer.cs   # EnsureCreated for multi-context PostgreSQL
│   │   └── BackgroundJobs/
│   │       └── ProcessOutboxJob.cs  # Quartz job: outbox → MassTransit
│   ├── ECommerce.Shared/            # Shared kernel
│   │   ├── Domain/                  # Entity, Result, Error, IDomainEvent, IRepository, OutboxMessage
│   │   │   └── ValueObjects/        # Money, Email, Sku
│   │   ├── Application/             # ValidationBehavior, Pagination
│   │   └── Infrastructure/          # Repository<T>, DomainEventDispatcher
│   ├── ECommerce.Modules.Catalog/
│   │   ├── Domain/                  # Product, Category, ICatalogRepositories
│   │   ├── Application/
│   │   │   ├── Commands/            # CreateCategory, CreateProduct, UpdateStock
│   │   │   └── Queries/             # GetProducts (paged), GetProductById
│   │   ├── Infrastructure/          # CatalogDbContext, ProductRepository, CategoryRepository
│   │   └── Endpoints/               # CatalogEndpoints (Minimal API)
│   ├── ECommerce.Modules.Ordering/
│   │   ├── Domain/                  # Order, OrderItem, OrderStatus, IOrderRepository
│   │   ├── Application/
│   │   │   ├── Commands/            # PlaceOrder
│   │   │   └── Queries/             # GetOrders (paged), GetOrderById
│   │   ├── Infrastructure/          # OrderingDbContext, OrderRepository
│   │   └── Endpoints/               # OrderingEndpoints
│   └── ECommerce.Modules.Billing/
│       ├── Domain/                  # Payment, Invoice, PaymentStatus, IPaymentRepository
│       ├── Application/
│       │   ├── Events/              # OrderCreatedConsumer (MassTransit)
│       │   └── Queries/             # GetPayments, GetInvoices
│       ├── Infrastructure/          # BillingDbContext, PaymentRepository, InvoiceRepository
│       └── Endpoints/               # BillingEndpoints
└── tests/
    ├── ECommerce.Tests.Unit/        # Domain, Value Objects, Validators, Behaviors
    ├── ECommerce.Tests.Integration/  # Handler tests with real SQLite in-memory
    └── ECommerce.Tests.EndToEnd/     # HTTP tests via WebApplicationFactory
```

## License

This project is provided for educational and demonstration purposes.
