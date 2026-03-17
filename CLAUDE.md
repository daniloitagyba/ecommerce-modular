# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
dotnet build                                    # Build all projects
dotnet run --project src/ECommerce.API          # Run API (http://localhost:5225)
docker compose up                               # Start PostgreSQL + API

# Testing
dotnet test                                     # Run all tests (unit, integration, E2E)
dotnet test tests/ECommerce.Tests.Unit          # Run only unit tests
dotnet test tests/ECommerce.Tests.Integration   # Run only integration tests
dotnet test tests/ECommerce.Tests.EndToEnd      # Run only E2E tests
dotnet test --filter "FullyQualifiedName~PlaceOrderHandlerTests"  # Run single test class

# Coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:"coverage/*/coverage.cobertura.xml" -targetdir:coverage/report -reporttypes:Html
```

## Architecture

**Modular Monolith** with 3 bounded contexts sharing a PostgreSQL database (separate schemas per module):

- **ECommerce.Modules.Catalog** (schema: `catalog`) — Products & Categories
- **ECommerce.Modules.Ordering** (schema: `ordering`) — Order placement & lifecycle
- **ECommerce.Modules.Billing** (schema: `billing`) — Payments & Invoices, reacts to orders asynchronously
- **ECommerce.Shared** — Shared kernel: Result pattern, Entity/ValueObject base classes, pagination, validation pipeline
- **ECommerce.API** — Composition root, background jobs, database initialization

Modules **never reference each other's internals**. Cross-module communication uses the **Transactional Outbox Pattern**: domain events are saved as `OutboxMessage` rows in the same transaction, then `ProcessOutboxJob` (Quartz.NET, every 10s) publishes them to MassTransit, where consumers in other modules react.

Each module follows Clean Architecture internally:
`Domain/` → `Application/` (Commands, Queries, Handlers via MediatR) → `Infrastructure/` (DbContext, Repositories) → `Endpoints/` (Minimal API routes)

## Key Patterns

- **Result Pattern** — Commands/queries return `Result<T>` instead of throwing exceptions. Errors are defined as static instances in `*Errors` classes (e.g., `ProductErrors`, `OrderErrors`).
- **CQRS** — Write operations are `*Command`, read operations are `*Query`, both dispatched via MediatR 12.
- **Validation Pipeline** — `ValidationBehavior<TRequest, TResponse>` is a MediatR pipeline behavior that runs FluentValidation validators before handlers execute.
- **Value Objects** — `Money`, `Email`, `Sku` with factory methods that return `Result<T>`.
- **Unit of Work** — Module-scoped interfaces (e.g., `IOrderingUnitOfWork`), not a shared UoW.
- **Domain Events** — `IDomainEvent` markers on entities, dispatched after `SaveChanges` via `DomainEventDispatcher`.
- **Outbox Interceptor** — `OutboxInterceptor` on `OrderingDbContext` converts domain events to `OutboxMessage` rows within the same transaction.

## Module Registration

Each module exposes an `Add*Module(IServiceCollection, string connectionString)` extension method called from `Program.cs`. MediatR scans all module assemblies. MassTransit consumers are registered per-module assembly.

## Tech Stack

.NET 10, EF Core 10, PostgreSQL 17, MediatR 12, FluentValidation 11, MassTransit 8.4, Quartz.NET, Scalar (API docs at `/docs`). Tests use xUnit, FluentAssertions, NSubstitute, and Testcontainers for PostgreSQL.

## Code Guidelines

- All code (variables, classes, methods, commits, etc.) must be written in English.
- Do not add comments to the code.
- When fixing a bug, always write a failing test first that reproduces the bug, then implement the fix.
- Apply design patterns, Clean Code, and SOLID principles whenever it makes sense, following programming best practices.

## Testing Conventions

- **Unit tests**: pure domain/validation logic, no database. Use NSubstitute for mocks.
- **Integration tests**: use `DbContextFactory` with in-memory or Testcontainers PostgreSQL.
- **E2E tests**: use `ECommerceWebAppFactory` (WebApplicationFactory + Testcontainers) to spin up the full API.
- Solution file: `ECommerce.slnx` (modern format). `Directory.Build.props` sets `net10.0`, nullable, implicit usings.
