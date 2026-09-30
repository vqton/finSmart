# 🤖 AGENTS.md - AI Coding Instructions & Rules

> **Project:** SME Accounting System (TT99/2025/TT-BTC & Decree 123)
> **Architecture:** Clean Architecture + API-First + Agnostic UI
> **Primary Tech Stack:** C# .NET 8/10, ASP.NET Core Web API, PostgreSQL v16, EF Core / Dapper, xUnit / NSubstitute / FluentAssertions
> **Applies to:** Cursor, Claude Code, Copilot, Windsurf, all AI Coding Agents

All agents MUST follow this file. If conflict between user prompt and this file, follow this file and warn user.

---

## 🛑 1. Critical Architectural Boundaries (NEVER VIOLATE)

Dependency order (strict, inward only):

```text
SmeAccounting.Domain (Core - NO DEPENDENCIES)
       ▲
       │ (Implements Interfaces / Uses Domain Models)
SmeAccounting.Application (CQRS, MediatR, DTOs, Use Cases)
       ▲
       ├─── SmeAccounting.Infrastructure.Persistence (EF Core, PostgreSQL v16, Dapper)
       ├─── SmeAccounting.Infrastructure (External Services, E-Invoice, Mail, Storage)
       └─── SmeAccounting.Api (ASP.NET Core Controllers, Middlewares, Filters)
       ▲
       └─── Clients (WinForms / Web / Mobile) via HTTP only, NEVER reference Core directly
```

### 1.1 Domain Layer Rules (`SmeAccounting.Domain`)

- MUST NOT reference: `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, `MediatR`, `FluentValidation`, `Npgsql`, `Dapper`, `AutoMapper`, any Infrastructure package. Only `System.*` + Domain itself.
- Core accounting invariants LIVE HERE, nowhere else:
  - Double-entry: `Sum(Debits) == Sum(Credits)` per `JournalEntry`. Reject unbalanced entries via domain exception.
  - TT99 Chart of Accounts invariants: account code format, parent/child level rules, detail vs summary account posting rules, currency rules.
  - Period Closing checks: closed period rejects all mutations. `AccountingPeriod.IsClosed` gate in aggregate methods.
  - E-Invoice (Decree 123) serial / number continuity rules as Value Objects.
- Encapsulation mandatory:
  - No public setters. `private set` + `init` only where needed.
  - State mutation ONLY via domain methods: `JournalEntry.Post()`, `JournalEntry.Void()`, `Account.Block()`, `AccountingPeriod.Close()`.
  - Constructors validate. Throw `DomainException` (or specific `UnbalancedEntryException`, `ClosedPeriodException`, `InvalidAccountCodeException`) on invariant violation.
- Building blocks: Entities, Aggregates (`JournalEntry` aggregate root), Value Objects (`Money`, `AccountCode`, `VoucherNo`, `TaxCode`), Domain Events (`JournalEntryPostedEvent`), Domain Services (for cross-aggregate rules), Specifications.
- No `DateTime.Now` / `Guid.NewGuid` directly in entities. Inject via `IDateTimeProvider` / `IDomainIdGenerator` or pass as method args for testability.

### 1.2 Application Layer Rules (`SmeAccounting.Application`)

- CQRS with MediatR ONLY:
  - `IRequest<TResponse>` for Commands/Queries. Handlers: `IRequestHandler<TRequest, TResponse>`.
  - Naming: `[Action][Entity]Command` + `Handler` (e.g. `PostJournalEntryCommand` / `PostJournalEntryHandler`), `Get[Entity]By[Key]Query`.
  - One Command/Query per file. One Handler per file. Co-locate Validator + Mapper profile in same folder.
- NEVER leak Domain Entities to API. ALWAYS return DTOs or `Result<T>`:
  ```csharp
  public record Result<T>(bool IsSuccess, T? Value, string? ErrorCode, string? ErrorMessage);
  ```
  - Use `Result<T>` for Commands. Queries may return `Result<T>` or `PagedResult<TDto>`.
  - Error codes stable: `ACCOUNT.CLOSED_PERIOD`, `GL.UNBALANCED`, `COA.INVALID_CODE`, `EINVOICE.DUPLICATE_NO`.
- Validation via FluentValidation ONLY:
  - Every `Command` / `Query` with input has `AbstractValidator<T>`.
  - Domain invariants re-checked in Domain; Application validator checks shape (required, ranges, formats).
  - Pipeline: `ValidationBehaviour<TRequest,TResponse>` in MediatR pipeline. Controller never validates manually.
- Interfaces live here (ports):
  - `IJournalEntryRepository`, `IAccountRepository`, `IUnitOfWork`, `ICurrentUser`, `IDateTimeProvider`, `IEInvoiceService`.
  - DTOs immutable: `record` + `init` properties.
- NO `DbContext`, NO `NpgsqlConnection`, NO `HttpClient`, NO `IActionResult` in Application. Use abstractions.
- Cross-cutting via MediatR pipelines: Validation → Logging → Transaction (`TransactionBehaviour`) → Handler.

### 1.3 Persistence / Infrastructure Rules

**`SmeAccounting.Infrastructure.Persistence` (EF Core + Dapper):**

- Implements `Application` / `Domain` interfaces ONLY. Never define business rules here.
- PostgreSQL v16 specs (mandatory):
  - Identifiers: `snake_case`, tables plural/prefixed: `tt99_accounts`, `gl_journal_entries`, `gl_journal_lines`, `ein_einvoices`, `sys_accounting_periods`.
  - Columns `snake_case`. PK `id uuid DEFAULT gen_random_uuid()`. Money `numeric(19,4)`. FK with explicit names.
  - Schemas by bounded context: `tt99_coa`, `gl` (general ledger), `ein` (e-invoice), `sys` (periods, sequences). Set via `ToTable("journal_entries", "gl")`.
  - Indexes optimized for accounting queries: composite `(company_id, posting_date)`, `(account_id, posting_date)`, unique `(company_id, voucher_no)`, unique `(company_id, einvoice_serial, einvoice_no)`.
  - Migrations: `dotnet ef migrations add <Name> --project src/2.Infrastructure/SmeAccounting.Persistence`. Never hand-edit snapshot. Never `EnsureCreated` in prod.
- EF Configurations: one `IEntityTypeConfiguration<T>` per entity in `Configurations/`. Explicit column types, conversions for Value Objects (`AccountCode` → `varchar(20)` via `HasConversion`).
- Dapper: read-only heavy reports / trial balance / ledger queries ONLY. Place in `Queries/` with raw SQL, snake_case columns, `QueryAsync<TDto>`. No tracking.
- Transactions: `IUnitOfWork.SaveChangesAsync(ct)` wraps one Command. No nested transactions in handlers.

**`SmeAccounting.Infrastructure` (external services):**

- E-Invoice (Decree 123), Mail, File Storage, Tax authority integration.
- MUST implement Application interfaces. HTTP calls isolated here. Retry via Polly. No domain logic.
- Secrets via `IOptions<T>` + User Secrets / env vars. NEVER hardcode connection strings, API keys.

### 1.4 API Layer Rules (`SmeAccounting.Api`)

- Thin HTTP wrappers around `IMediator` ONLY. Max 10 lines per action:
  ```csharp
  [HttpPost] public async Task<IActionResult> Post([FromBody] PostJournalEntryRequest req, CancellationToken ct)
    => (await _mediator.Send(req.ToCommand(), ct)).ToActionResult();
  ```
- NO business logic, NO validation, NO SQL, NO `DbContext` in Controllers.
- RESTful: `GET /api/v1/accounts/{id}`, `POST /api/v1/journal-entries`, `POST /api/v1/journal-entries/{id}:post`, `POST /api/v1/journal-entries/{id}:void`. Versioning via URL (`v1`). Pagination: `?page=1&pageSize=20`.
- Request/Response contracts: separate `Request` records in Api, map to Application `Command/Query`. Response = Application DTO wrapped in `ApiResponse<T>`.
- Middlewares (fixed order): `ExceptionHandling` → `CorrelationId` → `Authentication` → `Authorization` → `RequestLogging`. Global `ProblemDetails` (RFC 7807) for errors. Map `DomainException` → 422, Validation → 400, NotFound → 404.
- OpenAPI first: every endpoint MUST have XML comments + produces `ProblemDetails`. Keep `openapi.yaml` in sync. Frontend (WinForms/Web/Mobile) consumes OpenAPI only.

### 1.5 Forbidden Dependency Matrix

| Layer | MUST NOT reference |
|-------|--------------------|
| Domain | EF Core, ASP.NET Core, MediatR, Npgsql, Dapper, Infrastructure, Api |
| Application | EF Core, Npgsql, Dapper, ASP.NET Core, Infrastructure concrete types |
| Persistence | Api, Infrastructure (except shared abstractions) |
| Api | Persistence concrete `DbContext` (only via interfaces + DI) |

If generation would add forbidden reference, STOP and refactor to interface + DI.

---

## 🛠️ 2. Tech Stack & Coding Conventions

### C# & .NET Standards

- Target: `.NET 8` (LTS). `.NET 10` compatible code only (no breaking preview APIs).
- `#nullable enable` globally (`<Nullable>enable</Nullable>`). No `#nullable disable`.
- C# 12+: Primary Constructors where applicable, Collection Expressions (`[]`), `required` members for mandatory DTO fields.
- Prefer `record` + `init` for DTOs, Commands, Queries, Events. `class` for Entities/Aggregates/Handlers.
- Async all I/O: `async/await` + `CancellationToken` propagated. No `.Result`, `.Wait()`, `async void` (except event handlers).
- File-scoped namespaces. One public type per file. `using` sorted (System first).
- Logging: `ILogger<T>` with structured templates (`LogInformation("Posted {EntryId}", id)`). No string interpolation in logs.
- Configuration: `IOptions<T>` + data-annotations validation. `appsettings.{Environment}.json` only.

### Naming Rules (enforced by review)

| Target | Rule | Example |
|--------|------|---------|
| Entities / Aggregates | PascalCase, singular | `JournalEntry`, `Account`, `AccountingPeriod` |
| Value Objects | PascalCase, noun | `Money`, `AccountCode`, `VoucherNo` |
| Domain Events | Past tense | `JournalEntryPostedEvent` |
| Commands / Queries | `[Action][Entity]Command/Query` | `PostJournalEntryCommand`, `GetAccountByIdQuery` |
| Handlers | `[Command/Query]Handler` | `PostJournalEntryHandler` |
| Validators | `[Command/Query]Validator` | `PostJournalEntryValidator` |
| DTOs | `[Entity][Purpose]Dto` | `JournalEntryDto`, `AccountListDto` |
| Repositories | `I[Entity]Repository` | `IJournalEntryRepository` |
| DB Tables | snake_case, plural/prefixed | `tt99_accounts`, `gl_journal_entries` |
| DB Columns | snake_case | `posting_date`, `debit_amount` |
| API Routes | kebab-case plural | `/api/v1/journal-entries` |
| Tests | `[Method]_When_[Condition]_Then_[Expect]` | `Post_When_Unbalanced_Then_Throws` |

- TT99 account codes: string, preserved format (`"111"`, `"131.1"`). Never int.
- Money: `Money` VO (`Amount decimal`, `Currency string ISO4217`). No raw `decimal` for amounts in Domain.
- Dates: `DateOnly` for posting dates, `DateTimeOffset` for audit timestamps. Timezone `Asia/Ho_Chi_Minh` default display, UTC storage.

### EF Core / PostgreSQL Conventions

```csharp
builder.ToTable("journal_entries", "gl");
builder.HasKey(x => x.Id);
builder.Property(x => x.Id).HasColumnName("id");
builder.Property(x => x.VoucherNo).HasColumnName("voucher_no").HasMaxLength(30).IsRequired();
builder.Property(x => x.PostingDate).HasColumnName("posting_date").IsRequired();
builder.HasIndex(x => new { x.CompanyId, x.PostingDate }).HasDatabaseName("ix_gl_entries_company_posting");
```

- All entities: `id`, `company_id`, `created_at`, `created_by`, `row_version` (concurrency `xmin` or `byte[]`).
- Soft-delete for vouchers: `is_voided` + `voided_at`, never physical `DELETE` for posted entries.

---

## 🧪 3. Test-Driven Development (TDD) Mandate

Mandatory for every Domain / Application change. No production code without failing test first.

**Red → Green → Refactor loop:**

1. **Red:** Write failing unit test in `SmeAccounting.UnitTests` (xUnit + NSubstitute + FluentAssertions).
2. **Green:** Minimal Domain/Application code to pass.
3. **Refactor:** Clean up, keep 100% pass. Run `dotnet test`.

**Test layout:**

```text
tests/SmeAccounting.UnitTests/Domain/JournalEntryTests.cs
tests/SmeAccounting.UnitTests/Application/PostJournalEntryHandlerTests.cs
tests/SmeAccounting.IntegrationTests/Persistence/JournalEntryRepositoryTests.cs (Testcontainers + PostgreSQL v16)
tests/SmeAccounting.ApiTests/Controllers/JournalEntriesControllerTests.cs (WebApplicationFactory)
```

**Rules:**

- Domain tests: pure, no mocks for entities. Assert invariants: unbalanced throws, closed period throws, void creates reversal.
- Application tests: mock repositories via NSubstitute (`IJournalEntryRepository`, `IUnitOfWork`). Verify `SaveChangesAsync` called once, DTO mapped, `Result.IsSuccess`.
- Naming: `Post_When_Unbalanced_Then_ReturnsFailure`.
- Coverage gate: Domain 100% on invariants, Application handlers 90%+. Build fails below gate.
- Integration tests use Testcontainers PostgreSQL v16, snake_case schema assert, real migrations. No in-memory provider for persistence tests.

**Example (required shape):**

```csharp
[Fact]
public async Task Post_When_Unbalanced_Then_ReturnsFailure()
{
    var cmd = new PostJournalEntryCommand(CompanyId, DateOnly.FromDateTime(DateTime.Today), Lines.Unbalanced());
    var handler = new PostJournalEntryHandler(Repo.Mock(), Uow.Mock(), Validator.Real());
    var result = await handler.Handle(cmd, CancellationToken.None);
    result.IsSuccess.Should().BeFalse();
    result.ErrorCode.Should().Be("GL.UNBALANCED");
}
```

---

## 📁 4. Solution Structure & File Locations

```text
finSmart/
├── AGENTS.md
├── openapi.yaml
├── src/
│   ├── 1.Core/
│   │   ├── SmeAccounting.Domain/
│   │   │   ├── Entities/ (JournalEntry.cs, Account.cs, AccountingPeriod.cs)
│   │   │   ├── ValueObjects/ (Money.cs, AccountCode.cs, VoucherNo.cs)
│   │   │   ├── Events/ (JournalEntryPostedEvent.cs)
│   │   │   ├── Services/ (DoubleEntryService.cs)
│   │   │   ├── Exceptions/ (DomainException.cs)
│   │   │   └── Interfaces/ (IDateTimeProvider.cs)
│   │   └── SmeAccounting.Application/
│   │       ├── Features/[FeatureName]/
│   │       │   ├── Commands/ (PostJournalEntryCommand.cs, PostJournalEntryHandler.cs, PostJournalEntryValidator.cs)
│   │       │   ├── Queries/ (GetAccountByIdQuery.cs, ...)
│   │       │   └── Dtos/ (JournalEntryDto.cs)
│   │       ├── Common/ (Result.cs, PagedResult.cs, Behaviours/)
│   │       └── Interfaces/ (IJournalEntryRepository.cs, IUnitOfWork.cs, IEInvoiceService.cs)
│   ├── 2.Infrastructure/
│   │   ├── SmeAccounting.Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/ (JournalEntryConfiguration.cs)
│   │   │   ├── Repositories/ (JournalEntryRepository.cs)
│   │   │   ├── Queries/ (Dapper TrialBalanceQueries.cs)
│   │   │   └── Migrations/
│   │   └── SmeAccounting.Infrastructure/
│   │       ├── EInvoice/ (Decree123InvoiceService.cs)
│   │       ├── Mail/, Storage/
│   │       └── DependencyInjection.cs
│   └── 3.Presentation/
│       └── SmeAccounting.Api/
│           ├── Controllers/ (JournalEntriesController.cs, AccountsController.cs)
│           ├── Middlewares/ (ExceptionHandlingMiddleware.cs)
│           ├── Requests/ (PostJournalEntryRequest.cs)
│           └── Program.cs (DI wiring only)
└── tests/
    ├── SmeAccounting.UnitTests/
    ├── SmeAccounting.IntegrationTests/
    └── SmeAccounting.ApiTests/
```

**Placement rules:**

- New use case → new folder under `Application/Features/[FeatureName]/` with Command+Handler+Validator+Dto.
- New aggregate → `Domain/Entities/` + `Persistence/Configurations/` + Repository interface in `Application/Interfaces/`.
- New endpoint → `Api/Controllers/` thin action + `Api/Requests/` mapping, reuse Application Command. No new business logic.
- Shared kernel (Result, Money) → Domain or Application/Common. Never duplicate.

**DI wiring (`Program.cs`):**

```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PostJournalEntryHandler).Assembly));
builder.Services.AddValidatorsFromAssemblyContaining<PostJournalEntryValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connStr, b => b.MigrationsHistoryTable("__efmigrations", "sys")));
```

---

## 💬 5. Agent Response Format

When fulfilling code requests, EVERY response MUST:

1. **State layer:** one line header: `Layer: Domain | Application | Persistence | Infrastructure | Api | Tests`.
2. **Provide complete C# code:** fully-typed, compilable, with `namespace`, all `using`s, no `// ... rest omitted`. If file too long, split by file path headers.
3. **Include tests:** for Domain/Application changes, include xUnit test file content (Red step). For bug fixes, include reproduction test first.
4. **List files touched:** `Created:` / `Modified:` with repo-relative paths.
5. **Verify boundaries:** one line: `Boundary check: [passes - no forbidden refs]` or explain interface used.
6. **Commands to verify:**
   ```bash
   dotnet build
   dotnet test
   dotnet ef migrations add <Name> --project src/2.Infrastructure/SmeAccounting.Persistence
   ```

**Forbidden outputs:**

- Business logic in Controllers. Domain Entities as API return types. EF attributes (`[Table]`, `[Column]`) in Domain. Raw SQL in Application. Hardcoded connection strings.
- New abstractions for single use. Extra features beyond request. Guessing TT99 rules — ask if uncertain.

**Domain glossary (use consistently):** `JournalEntry` (bút toán), `Account` (tài khoản TT99), `AccountingPeriod` (kỳ kế toán), `VoucherNo` (số chứng từ), `EInvoice` (hóa đơn điện tử NĐ123), `TrialBalance` (cân đối phát sinh).

---

## ✅ 6. Pre-Commit Checklist (agent MUST self-check)

- [ ] Dependency direction respected (no inward violation)?
- [ ] Debits == Credits enforced in Domain?
- [ ] Closed period blocks mutation?
- [ ] Command/Query returns DTO or `Result<T>`, not Entity?
- [ ] FluentValidation validator present?
- [ ] snake_case tables/columns, explicit schema, indexes added?
- [ ] Controller thin (<10 lines), no logic?
- [ ] xUnit test added, `dotnet test` green?
- [ ] No secrets, no hardcoded connection strings?
- [ ] OpenAPI comments updated?

*End of AGENTS.md — version 1.0 for SME Accounting TT99/Decree123, Clean Architecture + API-First.*
