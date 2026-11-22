---
name: modern-csharp-coding-standards
description: Write, refactor, and review modern C# and .NET code using C# 12+ language standards, explicit built-in Dependency Injection (IServiceCollection), repository conventions (AGENTS.md, RULES.md, and dotnet-collin workflows), N-layered and feature-oriented architecture, Result pattern, stage configuration, untangling refactors, async safety, and modular skill routing without Scrutor. Use when creating, editing, reviewing, or architecting C# files, .NET solutions (.slnx), APIs, services, Blazor UIs, or project configurations.
---

# Modern C# Coding Standards & Repository Rules

Apply these standards whenever authoring, modifying, or reviewing C# and .NET codebases so code is type-safe, maintainable, predictable, and compliant with repository rules. User instructions take precedence over all defaults.

## 0. Preview-First, Scoping & Untangling Workflow

- **Show Result First (Burmese Preview):** Before editing files, explain the understood request, proposed resulting behavior or output, affected files, and material impacts in Burmese unless the user requests another language. Label the proposal clearly as a preview, not a verified result. For review/preview-only requests, stop and wait; when implementation is already authorized, present the preview and proceed without asking again.
- **Inspect Before Coding:** Read touched project files first (`.slnx`, `.sln`, `.csproj`, `Program.cs`, `Config/`, tests, and nearby code). Keep changes small and focused within the requested scope.
- **Codebase Untangling (Explicit Request Only):** Ordinary feature and bug-fix requests do not authorize a whole-codebase refactor. When the user explicitly requests codebase untangling or structural refactoring, follow `references/untangle-codebase.md` (establish build/test baseline, ask whether to include optional **Comments** and **Tests** cleanup, record a decision log for hidden/needless links, surplus code, and special cases, and verify within scope).

## 1. Core Modern C# Language Standards

- **Immutable Data Modeling:** Prefer `record` for reference-type DTOs/messages and `readonly record struct` for small value types to enforce value equality and immutability.
- **Strict Nullability:** Treat nullable reference types (`#nullable enable`) as strict contracts. Eliminate null-forgiving operators (`!`) unless proven safe; use pattern matching (`is not null`, `is { }`) instead of null checks.
- **High-Performance APIs:** Use `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>`, and `ReadOnlyMemory<T>` on hot paths to avoid heap allocations and unnecessary string/array slicing.
- **Modern Syntax & Formatting:** Adopt primary constructors for concise dependency injection, collection expressions (`[...]`), and switch expressions. Use Allman-style braces on separate lines, clear descriptive naming (no single-letter variables outside simple loops), and enums starting with `None = 0` (valid business values start at `1`; never silently renumber persisted enum values).

## 2. Dependency Injection, Architecture & Stage Config (No Scrutor)

- **Layered & Feature Architecture:** Respect existing project architecture - either three-layer (`Client -> Presentation -> Business -> Database`, see `references/n-layered-architecture.md`) or feature-oriented (`*.Api`, `*.Domain`, `*.Database`, `*.App`, see `references/feature-oriented-architecture.md`). Keep controllers thin and database projects independent of Business/Presentation.
- **Interface-Backed & Direct Registration:** Define explicit interfaces for domain/application services where boundaries, multiple implementations, or project conventions require them, and inject dependencies via primary constructors. Never instantiate services with `new` or use the Service Locator anti-pattern (`IServiceProvider.GetService` / `GetRequiredService` inside business logic).
- **Explicit Built-In Registration:** Do NOT use Scrutor or assembly-scanning reflection libraries. Register every service explicitly in `IServiceCollection` (via `Program.cs` or modular `Add<Feature>Services(this IServiceCollection services)` extension methods).
- **Strict Lifetime Discipline:** Match lifetimes intentionally (`AddScoped` for per-request/EF Core `DbContext` and business services, `AddTransient` for lightweight stateless operations, `AddSingleton` only for thread-safe stateless or shared state). Never inject a `Scoped` service or `DbContext` into a `Singleton`, and never dispose container-owned injected instances.
- **Stage Configuration (`AddStageConfig`):** Use the root `Config` folder (`Config/appsettings.json` containing `Stage` and defaults, plus `Config/custom-settings-{stage}.json`, see `references/stage-config-pattern.md`). Call `builder.AddStageConfig()` immediately after `WebApplication.CreateBuilder(args)` before reading config values, and bind typed settings via `IOptions<TOptions>` / `IOptionsMonitor<TOptions>`.
- **Outbound HTTP Clients:** Register clients with `AddHttpClient` (typed or named clients), inject `IHttpClientFactory` or the typed client, and create clients via `CreateClient()` - never instantiate or inject raw `HttpClient` directly. Pass `CancellationToken` on every outbound call and never blindly retry non-idempotent financial operations.

## 3. Result Pattern, Service Logging & Exception Handling

- **Transport-Neutral `Result<T>` Pattern:** Use the existing shared `Result<T>` pattern (`references/result-pattern.md`) to represent expected business success or failure explicitly (`Validation`, `NotFound`, `Conflict`, etc.). Do NOT throw exceptions for expected business or validation failures, and never return `null`, empty collections, or default values to disguise failures. Keep HTTP status mapping in the presentation/controller layer.
- **Mandatory `*Service.cs` Structure (Wallet / ABank Projects):** When working in projects following `RULES.md` / `references/wallet-project-rules.md`, every method in a concrete `*Service.cs` implementation must wrap execution in `try`/`catch` with structured `ILogger` logging:
  - Prefix every log message with `[ProjectName] MethodName => ` using the exact emitting method name and structured placeholders (never string interpolation, and never log passwords, PINs, OTPs, tokens, keys, or full account numbers).
  - Log `Information` for method start, processing, completion, and safe result metadata (including early returns).
  - Log `Warning` when validation fails before returning the failure result.
  - In `catch`, emit exactly one `Error` log with the caught exception and return a safe `Result<T>.SystemError(...)` without leaking exception internals.
- **Serilog & Middleware Pipeline:** Web applications must register `builder.Services.AddSerilog()` logging at `Information` level to console and hourly rolling `logs/{Assembly.GetEntryAssembly()?.GetName().Name}.log`, log startup/fatal errors with `[ProjectName] Main => ...`, and call `Log.CloseAndFlush()` in `finally`. Preserve middleware ordering (`ExceptionHandler -> HSTS/HTTPS -> StaticFiles -> Routing -> CORS -> Authentication -> Authorization -> Endpoints`).

## 4. API Contracts, Validation & Data Access

- **First-Party API Contracts:** Every new or modified first-party endpoint must accept one named endpoint-specific request model (including route, query, and body inputs) and return `Result<TResponse>` with a named, endpoint-specific response model. Never return EF Core entities, primitives, raw collections, or bare `PagedResult<T>` across the HTTP boundary. Listing endpoints must paginate and order results descending by default.
- **Domain-Authoritative Validation:** Keep the domain layer as the single authoritative source for CRUD validation. In wallet/ABank projects (`references/wallet-project-rules.md`), do NOT add field-length validation rules or `Maximum*Length` constants in `{Feature}ValidationPolicy` classes (operational limits like `MaximumPageSize` are allowed).
- **EF Core, Dapper & Stored Procedures:** Do NOT add pass-through repository abstractions over EF Core or Dapper; use EF Core directly for simple CRUD. Use `AsNoTracking()` for read-only queries, `FirstOrDefaultAsync` (never `SingleOrDefaultAsync`), include `--no-onconfiguring` on every `dotnet ef dbcontext scaffold` command, and use stored procedures (or existing Dapper patterns) for complex queries, reports, or multi-table joins (never write LINQ `join` / `Join` / `GroupJoin` in wallet projects). Use DB transactions and transaction log rows for balance/money updates.
- **Health Checks & Auth:** Every web project must register ASP.NET Core health checks and expose `/health` with `app.MapHealthChecks("/health")` reachable anonymously. For JWT dynamic RBAC, validate bearer tokens before authorization and support both JWT `permission` claims and optional database permission checks (`checkFromDb: true`).

## 5. Shared Utilities, Security & UI/Display Conventions

- **Shared Utilities (`DevCode.cs`):** Keep shared utilities and centralized page-size options/validation in `DevCode.cs` (in the Shared project) using `this` extension methods.
- **Server-to-Server Encryption:** For wallet/IBanking projects (`references/wallet-project-rules.md`), encrypt IBanking <-> Gateway <-> Backend payloads on both hops using platform `AesGcm` (AES-256-GCM: 32-byte key, fresh 12-byte nonce, 16-byte tag) in `DevCode.cs` with independent key rings, `keyId` routing, authenticated additional data, and startup fail-closed validation.
- **Secret Preservation:** Preserve existing development credentials and connection strings during refactoring unless explicitly asked to rotate them; never introduce new hardcoded secrets.
- **Blazor UI & Notifications:** In wallet projects, style all Admin and IBanking controls consistently using shared `WalletIbmb.Shared/Styles/boardui-inspired.css` classes (`gtb-input`, `gtb-input-label`), and implement reusable confirmation, alert, and toast components purely in C#/Razor without custom JS or JS interop (popups for alerts/confirmations, corner notifications for routine success toasts).
- **Display & Layout Formatting:** Format dates as `dd-MM-yyyy` and date-times as `dd-MM-yyyy hh:mm tt`. Format numbers with thousand separators and MMK amounts as suffix (`1,000.00 MMK`). Align text left, numbers/amounts right, and checkboxes/actions centered. Keep large dialogs scrollable within the viewport with close buttons in both header and footer, and let cards in multi-card grids keep natural heights without forced equal-height stretching.

## 6. Asynchronous Safety, Verification & Documentation

- **Async & Concurrency:** Propagate `CancellationToken` to all I/O boundaries; never block with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`; prefer `ValueTask<T>` on synchronous hot paths and `IAsyncEnumerable<T>` for streams.
- **Build, Test & Quality:** Verify changes with `dotnet build`, full `dotnet test`, and `dotnet format`, maintaining zero compiler warnings.
- **Git, Releases & Summaries:** Make focused Conventional Commits per task (`feat(scope): ...`, `fix(scope): ...`). When asked for a Backlog work summary, output a short title, `---`, one concise bullet per verified task, and `Task count: N`.
- **Junior-Developer Documentation:** When documenting a project or feature flow in `README.md`, include a concise overview plus Mermaid workflow and code-flow diagrams.

## Reference Files

- `references/agents-and-rules.md` & `references/project-rules.md`: Detailed repository rules (`AGENTS.md` and `RULES.md`).
- `references/wallet-project-rules.md`: Opt-in wallet/ABank project-specific rules (`mob-wallet-agent-merchant`).
- `references/untangle-codebase.md`: Scoped structural refactoring, decision logs, optional comment/test cleanup, and verification.
- `references/n-layered-architecture.md` & `references/feature-oriented-architecture.md`: Three-layer and feature-oriented architectural blueprints.
- `references/result-pattern.md`: Transport-neutral `Result<T>` invariants and presentation-layer HTTP mapping.
- `references/stage-config-pattern.md`: Root `Config/` stage-based configuration loading (`AddStageConfig`).
- `references/dependency-injection.md`, `references/aspnet-core-middleware.md`, `references/http-client-factory.md`, `references/jwt-dynamic-rbac.md`, `references/design-style-theme-color-theory.md`, `references/meta-tags.md`: Specialized implementation guides.
