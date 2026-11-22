# Dependency Injection

Use ASP.NET Core's built-in container to supply dependencies instead of constructing infrastructure and services inside consumers. Constructor injection is the default because required dependencies are explicit and the object is usable after construction.

## Injection Choice

- **Constructor injection:** required or repeatedly used class dependencies.
- **Method/action injection:** a dependency needed by only one action or operation.
- **Property injection:** avoid with the built-in container unless an existing framework integration intentionally provides it; it permits incomplete objects.

Do not hide ordinary dependencies behind `IServiceProvider.GetRequiredService()` in business code. If constructor dependencies grow large, split the class by responsibility rather than replacing explicit dependencies with service location.

## Registration and Lifetimes

Register services at the composition root and select lifetime from actual ownership and state:

| Lifetime | New instance | Typical use |
|---|---|---|
| Transient | Each resolution | Lightweight stateless service |
| Scoped | Once per request/scope | Business service, EF Core `DbContext` |
| Singleton | Once per application/container | Thread-safe shared service or cache |

```csharp
builder.Services.AddTransient<EmailFormatter>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddSingleton<ExchangeRateCache>();
```

Prefer the established lifetime in the codebase. Web business services and `DbContext` are normally scoped. Do not inject a scoped service or `DbContext` into a singleton; this captures request state beyond its valid scope. If a singleton must perform scoped work, use a deliberate scope-per-operation design and review concurrency, disposal, and cancellation.

The container owns injected disposable services. Consumers must not dispose an injected `DbContext`, factory-created dependency, or other container-owned instance.

## Interfaces

DI does not require an interface. Register a concrete type when one implementation is sufficient:

```csharp
builder.Services.AddScoped<ProductService>();
```

Use an interface when it creates a real boundary, supports multiple implementations, or matches an existing project convention:

```csharp
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
```

Do not add one-to-one interfaces mechanically to every class.

## Verification

- Build or resolve the application host so missing registrations and invalid lifetime graphs fail early.
- Test behavior through public consumers rather than asserting container registration text.
- Ensure singleton implementations and their state are thread-safe.
- Confirm configuration objects are validated and secrets are not embedded in registration code.
