# ECommerce Modular Monolith

Aplicacao e-commerce modular monolith construida com **.NET 10**, seguindo **Domain-Driven Design (DDD)**, **Clean Architecture** e principios **SOLID**.

## Arquitetura

A aplicacao e organizada em tres **Bounded Contexts** independentes, cada um implementado como um modulo autocontido com seu proprio dominio, camada de aplicacao, infraestrutura e endpoints:

```
src/
├── ECommerce.API/                    # Host — conecta modulos, middleware, init do BD
├── ECommerce.Shared/                 # Kernel compartilhado (Result, Entity, Value Objects, Paginacao)
├── ECommerce.Modules.Catalog/        # Gerenciamento de Produtos e Categorias
├── ECommerce.Modules.Ordering/       # Criacao e ciclo de vida de Pedidos
└── ECommerce.Modules.Billing/        # Pagamentos e Faturas (orientado a eventos)

tests/
├── ECommerce.Tests.Unit/             # 80 testes unitarios
├── ECommerce.Tests.Integration/      # 21 testes de integracao
└── ECommerce.Tests.EndToEnd/         # 18 testes ponta a ponta
```

### Estrutura Interna dos Modulos

Cada modulo segue Clean Architecture:

```
Modulo/
├── Domain/            # Entidades, Value Objects, interfaces de Repositorio
├── Application/       # Commands, Queries, Handlers, Validators (CQRS via MediatR)
├── Infrastructure/    # DbContext, implementacoes de Repositorio
├── Endpoints/         # Mapeamento de rotas via Minimal API
└── ModuleRegistration # Ponto de entrada para registro de DI
```

## Stack Tecnologica

| Aspecto | Biblioteca |
|---|---|
| Framework | .NET 10 / C# (latest) |
| API | ASP.NET Core Minimal APIs |
| CQRS / Mediator | MediatR 12 |
| Validacao | FluentValidation 11 |
| ORM | Entity Framework Core 10 |
| Banco de dados | SQLite (dev) — design agnostico de provider |
| Testes | xUnit, FluentAssertions, NSubstitute |
| Cobertura | Coverlet + ReportGenerator |

## Design Patterns e Principios

- **Result Pattern** — `Result<T>` / `Error` no lugar de excecoes para controle de fluxo
- **Repository Pattern** — `IRepository<T>` generico com interfaces especificas por modulo
- **Unit of Work** — escopo por modulo (`ICatalogUnitOfWork`, `IOrderingUnitOfWork`, `IBillingUnitOfWork`)
- **Value Objects** — `Money`, `Email`, `Sku` com validacao via factory method
- **Domain Events** — `IDomainEvent` despachados apos `SaveChanges`
- **Integration Events** — comunicacao entre modulos via `OrderCreatedIntegrationEvent`
- **CQRS** — Commands (escrita) e Queries (leitura) separados atraves do MediatR
- **Paginacao** — `PagedRequest` / `PagedResult<T>` com extensao para `IQueryable`
- **Pipeline de Validacao** — `ValidationBehavior<TRequest, TResponse>` como behavior do MediatR
- **SRP (Responsabilidade Unica)** — Event handlers separados por responsabilidade (ex: `ProcessPaymentOnOrderCreated`, `GenerateInvoiceOnOrderCreated`)
- **Inversao de Dependencia** — Handlers dependem de abstracoes, nao do DbContext concreto

## Pre-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Como Executar

```bash
# Clonar o repositorio
git clone <repository-url>
cd ecommerce-modular

# Restaurar dependencias
dotnet restore

# Executar a aplicacao
dotnet run --project src/ECommerce.API
```

A API inicia em `http://localhost:5225` (veja `Properties/launchSettings.json`).

O banco SQLite (`ecommerce.db`) e criado automaticamente na primeira execucao.

## Endpoints da API

### Catalogo

| Metodo | Rota | Descricao |
|---|---|---|
| `POST` | `/api/catalog/categories` | Criar uma categoria |
| `GET` | `/api/catalog/products` | Listar produtos (paginado) |
| `GET` | `/api/catalog/products/{id}` | Buscar produto por ID |
| `POST` | `/api/catalog/products` | Criar um produto |
| `PUT` | `/api/catalog/products/{id}/stock` | Atualizar quantidade em estoque |

### Pedidos

| Metodo | Rota | Descricao |
|---|---|---|
| `POST` | `/api/orders` | Realizar um pedido |
| `GET` | `/api/orders` | Listar pedidos (paginado) |
| `GET` | `/api/orders/{id}` | Buscar pedido por ID |

### Faturamento

| Metodo | Rota | Descricao |
|---|---|---|
| `GET` | `/api/billing/payments/{orderId}` | Buscar pagamentos de um pedido |
| `GET` | `/api/billing/invoices/{orderId}` | Buscar faturas de um pedido |

> Pagamentos e faturas sao criados automaticamente quando um pedido e realizado, via event handlers de dominio.

### Paginacao

Endpoints de listagem aceitam parametros opcionais:

```
GET /api/catalog/products?page=1&pageSize=10
```

Formato da resposta:

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

## Exemplos de Uso

```bash
# Criar uma categoria
curl -X POST http://localhost:5225/api/catalog/categories \
  -H "Content-Type: application/json" \
  -d '{"name": "Eletronicos", "description": "Gadgets e dispositivos"}'

# Criar um produto
curl -X POST http://localhost:5225/api/catalog/products \
  -H "Content-Type: application/json" \
  -d '{"name": "Notebook", "sku": "NOT-001", "price": 4999.99, "stockQuantity": 50, "categoryId": "<category-id>"}'

# Realizar um pedido
curl -X POST http://localhost:5225/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerEmail": "joao@exemplo.com", "lines": [{"productId": "<product-id>", "productName": "Notebook", "unitPrice": 4999.99, "quantity": 1}]}'

# Verificar que o pagamento foi criado automaticamente
curl http://localhost:5225/api/billing/payments/<order-id>
```

## Testes

```bash
# Executar todos os testes
dotnet test

# Executar com cobertura de codigo
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Gerar relatorio HTML de cobertura (requer dotnet-reportgenerator-globaltool)
reportgenerator -reports:"coverage/*/coverage.cobertura.xml" -targetdir:coverage/report -reporttypes:Html
```

### Cobertura de Testes

| Projeto | Testes | Cobertura |
|---|---|---|
| Unitarios | 80 | Dominio, Value Objects, Validators |
| Integracao | 21 | Handlers com SQLite in-memory |
| Ponta a Ponta | 18 | Pipeline HTTP completo via WebApplicationFactory |
| **Total** | **119** | **87.9% metodos** |

Os modulos de negocio atingem **97-100%** de cobertura de metodos.

## Isolamento de Dados

Cada modulo possui seu proprio `DbContext` com schema dedicado (`catalog`, `ordering`, `billing`). No modo de desenvolvimento com SQLite, schemas sao ignorados, mas o design esta preparado para isolamento de schema com PostgreSQL.

A comunicacao entre modulos acontece exclusivamente atraves de integration events no Shared kernel — modulos nunca referenciam tipos internos de outros modulos.

## Estrutura do Projeto

```
ecommerce-modular/
├── ECommerce.slnx                    # Arquivo de solucao
├── Directory.Build.props             # Propriedades MSBuild compartilhadas (net10.0)
├── src/
│   ├── ECommerce.API/                # Aplicacao host
│   │   ├── Program.cs               # Composition root
│   │   └── DatabaseInitializer.cs   # EnsureCreated para multi-context SQLite
│   ├── ECommerce.Shared/            # Kernel compartilhado
│   │   ├── Domain/                  # Entity, Result, Error, IDomainEvent, IRepository
│   │   │   └── ValueObjects/        # Money, Email, Sku
│   │   ├── Application/             # ValidationBehavior, Paginacao
│   │   └── Infrastructure/          # Repository<T>, DomainEventDispatcher
│   ├── ECommerce.Modules.Catalog/
│   │   ├── Domain/                  # Product, Category, ICatalogRepositories
│   │   ├── Application/
│   │   │   ├── Commands/            # CreateCategory, CreateProduct, UpdateStock
│   │   │   └── Queries/             # GetProducts (paginado), GetProductById
│   │   ├── Infrastructure/          # CatalogDbContext, ProductRepository, CategoryRepository
│   │   └── Endpoints/               # CatalogEndpoints (Minimal API)
│   ├── ECommerce.Modules.Ordering/
│   │   ├── Domain/                  # Order, OrderItem, OrderStatus, IOrderRepository
│   │   ├── Application/
│   │   │   ├── Commands/            # PlaceOrder
│   │   │   └── Queries/             # GetOrders (paginado), GetOrderById
│   │   ├── Infrastructure/          # OrderingDbContext, OrderRepository
│   │   └── Endpoints/               # OrderingEndpoints
│   └── ECommerce.Modules.Billing/
│       ├── Domain/                  # Payment, Invoice, PaymentStatus, IPaymentRepository
│       ├── Application/
│       │   ├── Events/              # ProcessPaymentOnOrderCreated, GenerateInvoiceOnOrderCreated
│       │   └── Queries/             # GetPayments, GetInvoices
│       ├── Infrastructure/          # BillingDbContext, PaymentRepository, InvoiceRepository
│       └── Endpoints/               # BillingEndpoints
└── tests/
    ├── ECommerce.Tests.Unit/        # Dominio, Value Objects, Validators, Behaviors
    ├── ECommerce.Tests.Integration/  # Testes de handlers com SQLite in-memory
    └── ECommerce.Tests.EndToEnd/     # Testes HTTP via WebApplicationFactory
```

## Licenca

Este projeto e fornecido para fins educacionais e de demonstracao.
