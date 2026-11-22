# N-Layered Architecture

Use this reference when a Collin project adopts the three-layer architecture below. Preserve a different established architecture unless the user asks to change it.

## Dependency Direction

```text
Client -> Presentation -> Business -> Database

MyApp.Api -> MyApp.Business -> MyApp.Database
```

- **Presentation** receives HTTP requests, validates transport input, invokes business services, and maps service outcomes to HTTP responses.
- **Business** owns application and business logic. Services coordinate EF Core and Dapper operations.
- **Database** contains the EF Core database-first `DbContext` and generated models plus the Dapper connection factory and database-specific DTOs where appropriate.

Controllers must not contain database access or business rules. The Database project must not depend on the Business or Presentation projects.

## Data Access Boundary

In this pattern, a business service may inject `AppDbContext` and `DapperContext` directly. Do not add a repository whose only behavior is passing calls through to those APIs. Add or preserve a repository only when it provides a meaningful boundary, such as aggregate persistence, reusable query behavior, provider isolation, or an existing project convention.

Use EF Core for ordinary entity queries and writes. Dapper is suitable for complex read queries and reports. Follow the existing provider and data-access style when the repository already makes a different choice.

For database-first EF Core, keep generated context and model files in the Database project. Regenerate them from the schema rather than hand-editing generated files when possible. Confirm the project's existing scaffold command, provider, context name, and output directories before running scaffolding; never invent a connection string.

## Registration

Register the context, Dapper connection factory, and business services at the composition root. Web-request services and `DbContext` normally use scoped lifetime:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DapperContext>();
builder.Services.AddScoped<IProductService, ProductService>();
```

Use the project's existing interface convention. An interface is not required when there is only one implementation and no useful abstraction.

## Growth Rules

- Split an oversized service by feature or business responsibility.
- Pass `CancellationToken` through controller, service, EF Core, and Dapper operations when their APIs support it.
- Return transport-neutral DTOs or results from Business rather than `IActionResult` or HTTP status codes.
- Check EF and SQL code for over-fetching, N+1 queries, missing pagination, and repeated calls.
