Read and follow [RULES.md](RULES.md) in this directory.

# Repository Instructions

## BoardUI-inspired controls

Use the shared BoardUI-inspired styles for every textbox and form control in Admin and IBanking while retaining Blazor. Reuse existing styled controls and `gtb-input` / `gtb-input-label` classes; do not introduce unstyled inputs or rely on vendor classes alone. Keep accessible labels, focus states and existing form behavior. The shared styling source is `WalletIbmb.Shared/Styles/boardui-inspired.css`.

## Domain-authoritative validation

Make the domain layer the single authoritative source for CRUD request validation. API DTOs become plain transport contracts, while the Blazor form annotations remain only for immediate user-experience feedback.

Do not add field-length validation rules to `{Feature}ValidationPolicy` classes. Do not define or use constants such as `MaximumIdLength`, `MaximumSearchTermLength`, `MaximumUserNameLength`, `MaximumFullNameLength`, `MaximumRoleCodeLength`, `MaximumBranchCodeLength`, `MaximumStaffIdLength`, `MaximumPhoneNumberLength`, or `MaximumEmailLength` for validation. Do not reject requests based on string or identifier length in these policies.

Operational limits that are not field-length validation, such as `MaximumPageSize`, are still allowed.

## Development configuration preservation

Do not remove, redact, replace, or rotate existing development credentials or connection passwords during refactoring. Preserve the current development configuration and documentation values unless the user explicitly requests a credential change.

This exception applies only to the existing development values. Do not introduce new production secrets or expose additional sensitive credentials in source control.

## Shared utilities

Keep shared utility code in the Shared project in a file named `DevCode.cs`. Use `this` on the first parameter of applicable static utility methods so callers can use extension-method syntax. Centralize page-size options, page-size control logic, and page-size validation in `DevCode.cs`, and require callers to reuse them rather than define duplicate behavior.

## Server-to-server encryption

For the IBanking → API Gateway → Backend flow, encrypt request and response business payloads on both hops with AES-256-GCM. Use independent key rings for IBanking ↔ Gateway and Gateway ↔ Backend. The Gateway decrypts and re-encrypts at each boundary. Follow the wire format in `docs/ebmb-encryption.md`.

- Keep AES-GCM operations, key-ring validation, and associated-data construction in `WalletIbmb.Shared/DevCode.cs`. Use the platform `AesGcm` implementation. Base64 is only a wire encoding; do not treat it as encryption or fall back to plaintext.
- Use a randomly generated 32-byte key, a fresh 12-byte nonce for every encryption under that key, and a 16-byte authentication tag. Never reuse a key/nonce pair. Key size is a cryptographic requirement, not a domain field-length validation rule.
- Use a versioned, named encrypted request model and `Result<NamedEncryptedResponseModel>` with `keyId`, nonce, ciphertext, and tag. Bind the HTTP method, route, direction, key ID, and response status where applicable as authenticated additional data. Trust the decrypted inner result for business decisions, not unauthenticated outer metadata.
- Load keys from protected deployment configuration. Never hardcode or commit new encryption keys. Configure `ActiveKeyId` and a key dictionary; encrypt with the active key and decrypt by the payload's `keyId`. Validate configured key material at startup and fail closed if it is missing or invalid.
- Rotate by distributing a new key to both peers before changing `ActiveKeyId`. Keep the previous key available for decryption until in-flight messages have cleared, then remove it. Do not silently retry decryption with a different key.
- Reject unsupported versions, unknown key IDs, malformed envelopes, and authentication failures with generic errors. Never log keys, plaintext, ciphertext, tags, or sensitive payloads.
- Keep HTTPS and authentication in place. AES-GCM does not prevent replay; require a shared replay/idempotency mechanism before enabling financial operations. Test round trips, tampering, wrong associated data, key rotation, and both Gateway/Backend hops.

## First-party API contracts

Every new or modified first-party API endpoint must accept exactly one named request model and return `Result<TResponse>`, where `TResponse` is a named, endpoint-specific response model. Response models may contain lists or pagination metadata, but primitives, raw collections, anonymous payloads, and bare `PagedResult<T>` values must not cross the HTTP boundary. Vendor callbacks and SSR navigation routes are protocol boundaries and are exempt.

## Outbound HTTP clients

Use `IHttpClientFactory` for outbound HTTP calls. Register clients with `AddHttpClient`, inject `IHttpClientFactory`, and create clients with `CreateClient`. Do not instantiate `HttpClient` directly or inject `HttpClient` into application services.

## Health checks

Every web project must register ASP.NET Core health checks and expose `/health` with `app.MapHealthChecks("/health")`. The endpoint must be reachable by anonymous health probes, including in applications with a fallback authorization policy. Keep the basic endpoint as a process health check; add dependency checks to a separate readiness endpoint when needed. Health-check routes are operational endpoints and are exempt from the first-party business API contract rule.

## Entity Framework scaffolding

Every `dotnet ef dbcontext scaffold` command must include the `--no-onconfiguring` option.

## Entity Framework queries

Use Entity Framework Core `FirstOrDefaultAsync` for queries that return the first matching entity or no entity. Do not use `SingleOrDefaultAsync`.

Use EF Core only for straightforward CRUD operations. Use stored procedures for complex queries and all reports, including queries that join multiple tables. Do not write joins in EF Core LINQ query syntax (`join`) or method syntax (`Join` or `GroupJoin`).

## Logging convention

All logs must follow a consistent structure to make searching, tracing, and debugging easier.

### Required log format

Every log entry must include:

1. Project name.
2. Method name.
3. Log message.

Every log message must use the following prefix, with the exact name of the method that emits it followed by ` => `:

```text
[ProjectName] MethodName => Log message
```

Examples:

```text
[ABankBackendApi] Login => User login request received.
[ABankBackendApi] Login => User authenticated successfully. UserId=1001
[ABankBackendApi] CreateTransaction => Transaction started. Ref=TXN001
[ABankBackendApi] CreateTransaction => Transaction completed successfully.
```

### C# examples

Information:

```csharp
logger.LogInformation(
    "[ABankBackendApi] Login => User login request received. Username={Username}",
    request.Username);
```

Warning:

```csharp
logger.LogWarning(
    "[ABankBackendApi] Login => Invalid password. Username={Username}",
    request.Username);
```

Error:

```csharp
logger.LogError(
    exception,
    "[ABankBackendApi] Login => Login failed. Username={Username}",
    request.Username);
```

Debug:

```csharp
logger.LogDebug(
    "[ABankBackendApi] CreateTransaction => Validation completed.");
```

### Do

- Always include the project name.
- Start every log message with `[ProjectName] MethodName =>`.
- Use the exact method name that emits the log.
- Use structured logging placeholders such as `{UserId}` and `{ReferenceNo}`.
- Keep messages concise and action-oriented.
- Include relevant identifiers, but avoid sensitive data.

### Don't

- Do not omit the project or method name.
- Do not change the `[ProjectName] MethodName =>` prefix format.
- Do not use string interpolation for structured log values.
- Do not log passwords, PINs, OTPs, JWTs, or full account numbers.
- Do not write vague messages such as `Error` or `Success` without context.

### Standard patterns

| Event | Example |
| --- | --- |
| Start | `[Project] Login => Login process started.` |
| Validation | `[Project] Login => Request validation completed.` |
| Success | `[Project] Login => Login completed successfully.` |
| Warning | `[Project] Login => Invalid credentials.` |
| Error | `[Project] Login => Login failed.` |

### Example flow

```csharp
public async Task<LoginResponseModel> Login(LoginRequestModel request)
{
    logger.LogInformation(
        "[ABankBackendApi] Login => Login process started. Username={Username}",
        request.Username);

    // ...

    logger.LogInformation(
        "[ABankBackendApi] Login => Login completed successfully. UserId={UserId}",
        user.Id);

    return response;
}
```

This standard ensures every log is searchable by project, method, and action while remaining compatible with `ILogger` structured logging practices.

### Serilog setup in web applications

- Every web application's `Program.cs` must configure Serilog text logging at Information level to both the console and an hourly rolling `logs/{Assembly.GetEntryAssembly()?.GetName().Name}.log` file.
- Register Serilog with `builder.Services.AddSerilog()`.
- Log application startup and fatal termination with the `[ProjectName] Main => ...` message prefix.
- Always call `Log.CloseAndFlush()` in a `finally` block.
- If showing setup examples, include only Serilog-related code, not unrelated `WebApplication` setup.

## Release workflow

- Before preparing a release, inspect the repository's latest release and follow its existing artifact naming, release naming, and release-note conventions.
- Keep all project and package versions consistent.
- Create one focused Conventional Commit for each completed task, followed by a dedicated `chore(release): prepare vX.Y.Z` commit. Resolve all validation failures before tagging or publishing.
- Run the relevant tests and release build before publishing. Build, sign, and checksum every expected release artifact.
- When the project is locally installable, install the release locally and verify its installed version and status.
- Push the branch and an annotated version tag. Publish a GitHub release that follows the existing naming and notes conventions, upload every required asset, verify the remote asset digests, and confirm that the local Git state is clean and synchronized with the remote.

## General coding rules

This project prioritizes readable, maintainable, and predictable code. Optimize for human readability first.

### Naming

#### Do

- Use descriptive names.
- Name variables after what they represent.
- Name methods with clear verbs.
- Use PascalCase for SQL table names.
- Keep naming consistent across the project.

```csharp
GetCustomerById();
CreatePayment();
var activeUsers = GetActiveUsers();
```

#### Don't

- Do not use single-letter names except for simple loop variables.
- Do not abbreviate unnecessarily.

```csharp
// Bad
var d = GetData();
var tmp = CreateTemporaryValue();
GetCust();
```

### Methods

#### Do

- Give each method one responsibility.
- Keep methods short.
- Extract reusable logic into private methods.
- Prefer early returns to deep nesting.

```csharp
public async Task LoginAsync()
{
    ValidateRequest();

    if (!IsValidUser())
    {
        return;
    }

    await CreateSessionAsync();
}
```

#### Don't

- Do not write excessively long methods.
- Do not mix validation, business logic, database access, and logging in one method.

### Classes

#### Do

- Keep classes focused on a single responsibility.
- Group related functionality together.

#### Don't

- Do not create god classes that handle unrelated responsibilities.

### Comments

#### Do

- Explain why code exists when the reason is not apparent.

```csharp
// Finacle requires account validation before transfer.
```

#### Don't

- Do not explain obvious code.
- Do not leave commented-out code.

```csharp
// Bad: increment count.
count++;
```

### Formatting

#### Do

- Use consistent indentation.
- Put braces on separate lines.
- Leave blank lines between logical sections.

#### Don't

- Do not compress code into one line.
- Do not mix formatting styles.

### Error handling

#### Do

- Handle expected failures.
- Return meaningful error messages.
- Log useful context while following the repository logging convention.

#### Don't

- Do not swallow exceptions.
- Do not expose sensitive information.

### Duplication

#### Do

- Extract repeated logic.
- Reuse existing helpers.

#### Don't

- Do not copy and paste large blocks.

### Constants

#### Do

- Use named constants or enums.
- Every enum must declare `None = 0` as its first member so the default enum value represents no selection or an unset value. Start valid business options at `1`.

```csharp
public enum UserStatus
{
    None = 0,
    Pending = 1,
    Active = 2,
    Locked = 3
}
```

#### Don't

- Do not use unexplained magic numbers.
- Do not assign `0` to a valid business option; it can hide a missing selection and cause an incorrect flow.

```csharp
// Bad
if (status == 3)
{
    // ...
}
```

### Clean logic

#### Do

- Write code that reads like English.
- Prefer guard clauses.

```csharp
ValidateRequest();
CheckDuplicateUser();
SaveUser();
SendNotification();
```

#### Don't

- Do not create unnecessary nesting.

```csharp
// Bad
if (a)
{
    if (b)
    {
        if (c)
        {
            // ...
        }
    }
}
```

### Project structure

#### Do

- Keep feature-related files together.

```text
Features/
└── Customer/
    ├── CustomerController.cs
    ├── CustomerService.cs
    ├── CreateCustomerRequestModel.cs
    └── CreateCustomerResponseModel.cs
```

#### Don't

- Do not scatter related files across unrelated folders.

### Git

#### Do

- Make small, focused commits.
- Use clear, scoped commit messages.

```text
feat(customer): add customer search
fix(login): handle expired token
refactor(payment): simplify validation
```

#### Don't

- Do not mix unrelated changes in one commit.
- Do not commit debugging code.

### Testing

#### Do

- Test new behavior.
- Verify edge cases.
- Keep tests readable.

#### Don't

- Do not leave failing tests.
- Do not skip obvious validation cases.

### Security

#### Do

- Validate all inputs in the domain layer, as required by the domain-authoritative validation rule.
- Use parameterized queries.
- Protect secrets.

#### Don't

- Do not hardcode passwords.
- Do not log tokens or sensitive data.

### Performance

#### Do

- Measure before optimizing.
- Write clear code first.

#### Don't

- Do not sacrifice readability for tiny performance gains.
- Do not optimize prematurely.

### Agent behavior

When generating or modifying code:

#### Do

- Preserve the project's existing coding style.
- Make the smallest safe change.
- Reuse existing patterns.
- Keep public APIs consistent.
- Explain significant architectural changes.
- Remove obsolete code when replacing it.
- Keep changes focused on the requested task.

#### Don't

- Do not rename files or methods without necessity.
- Do not introduce new dependencies unless required.
- Do not change unrelated code.
- Do not invent frameworks or patterns not already used.
- Do not over-engineer simple solutions.
- Do not leave TODOs unless explicitly requested.

### The 30-second readability test

Before considering a task complete, ask:

- Can another developer understand this file in under 30 seconds?
- Are method names self-explanatory?
- Does each method have one responsibility?
- Is duplicate code avoided?
- Is the code simpler than before?

If any answer is no, refactor before finishing.

### Philosophy

> Write code like a professional letter: clear, concise, organized, and easy for the next person to understand without additional explanation.
