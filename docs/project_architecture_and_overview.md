# 🏥 ModuPOS Development & Architecture Guide

Welcome to the ModuPOS development team! This guide details our system architecture, project layout, and standard implementation patterns to help you build cohesive and clean features using our **Feature-based Organization**.

---

## **🏗️ Architecture Overview**

Our projects follow a strict **Feature-based Organization**. Instead of grouping code by technical layers (e.g., all controllers in one folder, all services in another), we group all logical components of a business concern into a **Business Feature** directory.

The application follows a clean, direct request-to-persistence flow:

```
Request (Client) ──> Controller (Api) ──> Service (Domain Feature) ──> Database (EF Core / AppDbContext)
```

---

### **High-Level Project Structure**

The solution is divided into the following projects and clients:

| Project / Directory | Type / Framework | Purpose |
| :--- | :--- | :--- |
| **`ModuPOS.Api`** | ASP.NET Core (.NET 10.0) Web API | Exposes RESTful endpoints, handles auth middleware, and serves OpenAPI documentation. |
| **`ModuPOS.Inventory`** | C# Class Library (.NET 10.0) | Contains Inventory business logic (Category, Product), DTOs, and services. |
| **`ModuPOS.Organization`** | C# Class Library (.NET 10.0) | Contains Organization business logic (Branch, Staff, StaffRole, Shift, StaffShift). |
| **`ModuPOS.Auth`** | C# Class Library (.NET 10.0) | Contains Identity and Security logic (User, Role, Permission, JWT Auth). |
| **`ModuPOS.Database`** | C# Class Library (.NET 10.0) | EF Core context (`AppDbContext`) and scaffolded MSSQL database-first entity models. |
| **`ModuPOS.Shared`** | C# Class Library (.NET 10.0) | Contains shared helpers, pagination models (`PagedResult<T>`, `PaginationRequest`), and standard `Result`/`Result<T>`. |
| **`ModuPOS.FeatureManagement`** | C# Class Library (.NET 10.0) | Tenant feature flags and license gatekeeper. |
| **`ModuPOS.LicenseManagement`** | C# Class Library (.NET 10.0) | Cryptographic license signature and payload verification. |
| **`tests/ModuPOS.Auth.UnitTests`** | xUnit Test Project (.NET 10.0) | Unit tests verifying domain services using In-Memory EF Core context. |
| **`tests/ModuPOS.Api.IntegrationTests`** | xUnit Test Project (.NET 10.0) | Integration tests using `WebApplicationFactory<Program>`. |

---

## **📂 Deep Dive: Feature-based Organization**

### **Current Features Registry**

Here is the list of active feature modules currently in the domain:

1. **Inventory (`ModuPOS.Inventory`)**:
   - `Category`: Categories management with soft-delete (`DeleteFlag`).
   - `Product`: Products catalog with pricing, stock quantity, category association, and soft-delete (`DeleteFlag`).
2. **Organization (`ModuPOS.Organization`)**:
   - `Branch`: Store branches (prohibits deletion via API per schema rules).
   - `Staff`: Employee roster, contact details, branch affiliation, and soft-delete (`DeleteFlag`).
   - `StaffRole`: Standalone organizational role profile and salary compensation catalog with soft-delete.
   - `Shift`: Work period schedules (prohibits deletion via API per schema rules).
   - `StaffShift`: Operational staff-to-shift assignments.
3. **Authentication & Authorization (`ModuPOS.Auth`)**:
   - `Auth`: JWT token generation, expiration tracking, refresh token rotation, and revocation.
   - `User`: User accounts with hashed credentials, role assignment, active flag, and soft-delete (`is_deleted`).
   - `Role`: System security roles and permission mappings.
   - `Permission`: Granular permission catalog.

---

## **🔄 Service Registration & Dependency Injection**

Each module provides its own modular dependency injection registration extension method in `FeatureManager.cs`:

- `builder.Services.AddInventoryModule();` in `ModuPOS.Inventory`
- `builder.Services.AddOrganizationModule();` in `ModuPOS.Organization`
- `builder.Services.AddAuthModule(builder.Configuration);` in `ModuPOS.Auth`
- `builder.Services.AddModuPOSDatabase(builder.Configuration);` in `ModuPOS.Database`

This keeps `Program.cs` lean and cohesive.

---

## **🛡️ The Result Pattern & Pagination Protocol**

We do not throw business exceptions or return raw database entities. Instead, services wrap all operational outputs in a `Result`, `Result<T>`, or `PagedResult<T>` structure (from `ModuPOS.Shared`).

### **Standard Service Signatures**

```csharp
// Single entity operation
public async Task<Result<ProductResponseDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(request.ProductCode) || string.IsNullOrWhiteSpace(request.Name))
        return Result<ProductResponseDto>.Failure("Product code and name are required.");

    var entity = new Product { /* map fields */ };
    context.Products.Add(entity);
    await context.SaveChangesAsync(cancellationToken);

    return Result<ProductResponseDto>.Success(new ProductResponseDto(...));
}

// Paginated collection query
public async Task<PagedResult<ProductResponseDto>> GetProductsAsync(int? categoryId = null, string? search = null, PaginationRequest? request = null, CancellationToken cancellationToken = default)
{
    request ??= new PaginationRequest();
    var query = context.Products.AsNoTracking().Where(p => !p.DeleteFlag);

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .OrderBy(p => p.ProductId)
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(...)
        .ToListAsync(cancellationToken);

    var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
    return PagedResult<ProductResponseDto>.Success(items, pagination);
}
```

### **Controller Translation Pattern**

```csharp
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
{
    var result = await productService.GetProductsAsync(request: request, cancellationToken: ct);
    return Ok(result);
}

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
{
    var result = await productService.CreateProductAsync(request, ct);
    return result.IsSuccess
        ? CreatedAtAction(nameof(GetById), new { id = result.Data!.ProductId }, result)
        : BadRequest(result);
}
```

---

## **💾 Database & Models**

- **Database-First Schema**: Defined in `ModuPOS.Database/db.sql` and `docs/db_tables.md`.
- **Entities**: Mapped under `ModuPOS.Database.AppDbContextModels/` within `AppDbContext`.
- **Soft-Delete Rules**:
  - `DeleteFlag` for `Product`, `Category`, `Staff`, `StaffRole`.
  - `is_deleted` for `User`, `Role`, `Permission`, `RolePermission`.
  - `Branch` and `Shift` do not have delete flags and their deletion is prohibited via the API.
- **Audit Tracking**: Creation and update UTC timestamps and user identifiers are persisted for entities providing audit columns.

---

## **🧪 Automated Testing Guidance**

We test business features using xUnit unit tests located in `tests/ModuPOS.Auth.UnitTests/` and integration tests in `tests/ModuPOS.Api.IntegrationTests/`.

- Unit tests utilize an in-memory database helper (`UseInMemoryDatabase(Guid.NewGuid().ToString())`) ensuring a fresh environment for each test.
- Every test follows the explicit `// Arrange`, `// Act`, and `// Assert` structure.

```sh
# Run all automated tests
dotnet test ModuPOS.slnx -c Release
```
