# Implementation Plan: Database Feature Alignment and Shared Contract Standardization

**Branch**: `002-database-feature-alignment` | **Date**: 2026-11-16 | **Spec**: [spec.md](file:///d:/ModuPOS/specs/002-database-feature-alignment/spec.md)

**Input**: Feature specification from `/specs/002-database-feature-alignment/spec.md`

## Summary

Systematically align all feature modules (`ModuPOS.Inventory`, `ModuPOS.Organization`, `ModuPOS.Auth`, and `ModuPOS.Api`) with the MSSQL Database-First schema (`ModuPOS.Database`) and the standardized Result/Pagination pattern (`ModuPOS.Shared`). The technical approach resolves compiler defects across all services, introduces 100% paginated collection queries via `PagedResult<T>`, respects entity lifecycle rules (prohibiting branch/shift deletion and enforcing soft-delete flags), decouples staff roles as an independent reference catalog, and updates unit and integration test suites.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0 (net10.0)

**Primary Dependencies**: ASP.NET Core, Microsoft.EntityFrameworkCore.SqlServer (10.0), Microsoft.AspNetCore.Authentication.JwtBearer, System.IdentityModel.Tokens.Jwt, Microsoft.AspNetCore.Identity

**Storage**: Microsoft SQL Server (MSSQL) Database-First via `AppDbContext` and entity models in `ModuPOS.Database.AppDbContextModels`

**Testing**: xUnit v3, Microsoft.Testing.Platform, Microsoft.EntityFrameworkCore.InMemory, Microsoft.AspNetCore.Mvc.Testing (`WebApplicationFactory<Program>`)

**Target Platform**: Windows / Linux cross-platform server (.NET 10 runtime)

**Project Type**: Feature-Driven Modular Web API + Class Libraries

**Performance Goals**: Bounded database querying via `Skip`/`Take`, sub-50ms query processing for standard pages (10 records), zero unpaginated full-table loads on collection endpoints

**Constraints**: Singular table names, no database-level foreign key enforcement, 100% Result pattern compliance, lines <= 225 characters, file-scoped namespaces, nullable reference type safety

**Scale/Scope**: 12 database tables across 3 active business domains (Inventory, Organization, Auth), 9 REST API controllers, 8 domain service implementations, and automated test suites

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Constitutional Rule | Compliance Status | Details / Justification |
|---|---|---|
| File-scoped namespaces & Nullable Safety | PASS | All created/modified C# files use file-scoped namespaces and `#nullable enable` |
| XML Documentation on non-private entities | PASS | All public interfaces, DTOs, and service methods documented with XML comments |
| Max Line Length (<= 225 chars) | PASS | No line exceeds 225 characters |
| Test Structure (Arrange/Act/Assert) | PASS | All unit and integration tests feature explicit `// Arrange`, `// Act`, `// Assert` sections |
| Clean Feature-Based Architecture | PASS | Request flows directly: `Controller -> Service -> AppDbContext` without unnecessary mediator/repository bloat (Ponytail / YAGNI) |
| Conventional Commits | PASS | Commit messages follow `type(scope): description` |

## Project Structure

### Documentation (this feature)

```text
specs/002-database-feature-alignment/
├── plan.md              # This implementation plan
├── research.md          # Technical decisions and trade-off analysis
├── data-model.md        # Comprehensive 12-table data model and entity definitions
├── quickstart.md        # Validation guide and verification scenarios
├── contracts/           # API interface specifications
│   ├── pagination-and-results.md
│   ├── inventory-api.md
│   ├── organization-api.md
│   └── auth-api.md
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Implementation tasks (/speckit-tasks command)
```

### Source Code (repository layout)

```text
d:\ModuPOS\
├── ModuPOS.Shared/
│   ├── Results/
│   │   └── Result.cs                         # Result, Result<T> pattern
│   └── Pagination/
│       ├── Pagination.cs                     # Pagination metadata & PagedResult<T>
│       └── PaginationRequest.cs              # PageNumber & PageSize request model
├── ModuPOS.Database/
│   ├── AppDbContextModels/                   # 12 Scaffolded MSSQL entities & AppDbContext
│   │   ├── Branch.cs, Category.cs, Permission.cs, Product.cs, RefreshToken.cs,
│   │   ├── Role.cs, RolePermission.cs, Shift.cs, Staff.cs, StaffRole.cs, StaffShift.cs, User.cs
│   │   └── AppDbContext.cs
│   └── db.sql                                # MSSQL database-first schema definition
├── ModuPOS.Inventory/
│   ├── DTOs/
│   │   ├── CategoryDTO.cs                    # Category requests and response DTOs
│   │   └── ProductDTO.cs                     # Product requests and response DTOs
│   ├── Services/
│   │   ├── Category/CategoryService.cs       # ICategoryService & CategoryService
│   │   └── Product/ProductService.cs         # IProductService & ProductService
│   └── FeatureManager.cs                     # DI module registration
├── ModuPOS.Organization/
│   ├── DTOs/
│   │   ├── BranchDTO.cs                      # Branch requests & responses
│   │   ├── ShiftDTO.cs                       # Shift & StaffShift requests & responses
│   │   └── StaffDTO.cs                       # Staff & StaffRole requests & responses
│   ├── Services/
│   │   ├── Branch/BranchService.cs           # IBranchService & BranchService
│   │   ├── Shift/ShiftService.cs             # IShiftService & ShiftService
│   │   └── Staff/
│   │       ├── StaffService.cs               # IStaffService & StaffService
│   │       └── StaffRoleService.cs           # IStaffRoleService & StaffRoleService
│   └── FeatureManager.cs                     # DI module registration
├── ModuPOS.Auth/
│   ├── DTOs/
│   │   └── AuthDTO.cs                        # Auth, User, Role, Permission DTOs
│   ├── Services/
│   │   ├── Auth/AuthService.cs               # JWT token generation & authentication
│   │   ├── User/UserService.cs               # User lifecycle & identity
│   │   ├── Role/RoleService.cs               # Roles & permissions mapping
│   │   └── Permission/PermissionService.cs   # Permissions catalog
│   └── FeatureManager.cs                     # DI module registration
├── ModuPOS.Api/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── BranchesController.cs
│   │   ├── CategoriesController.cs
│   │   ├── PermissionsController.cs
│   │   ├── ProductsController.cs
│   │   ├── RolesController.cs
│   │   ├── ShiftsController.cs
│   │   ├── StaffController.cs
│   │   └── UsersController.cs
│   └── Program.cs
├── ModuPOS.Sale/                             # Inactive stub library
├── ModuPOS.Loyalty/                          # Inactive stub library
├── ModuPOS.Reporting/                        # Inactive stub library
├── ModuPOS.FeatureManagement/                # Feature registry & license gatekeeper
├── ModuPOS.LicenseManagement/                # Standalone cryptographic license validator
└── tests/
    ├── ModuPOS.Auth.UnitTests/               # Domain service unit tests
    └── ModuPOS.Api.IntegrationTests/         # API integration tests
```

**Structure Decision**: The solution adheres to ModuPOS's Feature-based Organization. Business logic and DTOs reside in domain-specific class libraries (`ModuPOS.Inventory`, `ModuPOS.Organization`, `ModuPOS.Auth`), controllers reside in `ModuPOS.Api/Controllers/`, data access directly leverages `AppDbContext`, and shared envelopes reside in `ModuPOS.Shared`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| None | N/A | Design follows standard direct Controller -> Service -> DbContext architecture without unrequested abstractions |
