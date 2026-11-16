# Research & Technical Decisions: Database Feature Alignment

**Feature Branch**: `002-database-feature-alignment`
**Date**: 2026-11-16
**Status**: Completed

## 1. Executive Summary

This research resolves all technical decisions and design strategies required to align the ModuPOS feature modules (`ModuPOS.Inventory`, `ModuPOS.Organization`, `ModuPOS.Auth`, `ModuPOS.Api`) with:
1. The MSSQL Database-First schema defined in `ModuPOS.Database/db.sql` and `docs/db_tables.md`.
2. The standardized `Result` / `Result<T>` and `Pagination` / `PagedResult<T>` infrastructure in `ModuPOS.Shared`.
3. The user-confirmed architectural decisions (Q1: A - Prohibit Branch/Shift deletion; Q2: B - Enforce pagination on 100% of collection endpoints; Q3: A - Maintain StaffRole as a standalone catalog).

---

## 2. Technical Decisions & Analysis

### Decision 1: Result Pattern Standardization across Services & Controllers
- **Problem**: In commit `58368de`, `ModuPOS.Shared.Results.Result` was modernized to remove `Result.Failure<T>(msg)` and changed `Result.Success(msg)` to non-generic. All existing services call `Result.Failure<T>("...")` and `Result.Success(data)` resulting in 79 compile errors (CS0308, CS1503).
- **Decision**:
  - Non-payload operations (void equivalent): return `Task<Result>`. Use `Result.Success(message)` and `Result.Failure(errorMessage)`.
  - Single-entity operations: return `Task<Result<TDto>>`. Use `Result<TDto>.Success(data, message)` and `Result<TDto>.Failure(errorMessage)`.
  - Collection queries: return `Task<PagedResult<TDto>>`. Use `PagedResult<TDto>.Success(data, pagination, message)` and `PagedResult<TDto>.Failure(errorMessage)`.
- **Alternatives Considered**:
  - *Re-adding generic helpers back to non-generic Result*: Rejected because keeping `Result` and `Result<T>` distinct avoids ambiguity and matches clean C# API guidelines.
- **Impact**: Restores clean compilation, enforces strong typing, and ensures consistent error messaging across API controllers.

---

### Decision 2: 100% Paginated Collection Query Pattern
- **Problem**: Prior service methods returned unpaginated `List<T>` inside `Result<List<T>>`. For growing datasets (Products, Staff, Categories, Users), this causes memory pressure and unbounded queries.
- **Decision**:
  - Every collection query accepts `PaginationRequest request` (`PageNumber`, `PageSize`) with optional filters (e.g. `branchCode`, `categoryId`, `search`).
  - Validation: Sanitize `PageNumber < 1 => 1` and clamp `PageSize` between 1 and 100 (default 10).
  - Implementation:
    ```csharp
    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(...)
        .ToListAsync(cancellationToken);
    var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
    return PagedResult<TDto>.Success(items, pagination);
    ```
  - Scope: Applied across all 10 collection endpoints: `Categories`, `Products`, `Branches`, `Staff`, `StaffRoles`, `Shifts`, `StaffShifts`, `Users`, `Roles`, and `Permissions`.
- **Alternatives Considered**:
  - *Keep lookup catalogs (Permissions, Roles) unpaginated*: User explicitly selected Option B (100% pagination) for universal consistency.

---

### Decision 3: Entity Lifecycle & Deletion Semantics
- **Problem**: Discrepancy in table columns:
  - Entities with `DeleteFlag` (`Staff`, `StaffRole`, `Category`, `Product`): soft-delete via `DeleteFlag = true`.
  - Entities with `is_deleted` (`User`, `Role`, `Permission`, `Role_Permission`): soft-delete via `IsDeleted = true`.
  - Entities without delete flags (`Branch`, `Shift`, `StaffShift`): deletion is not supported in the database schema.
- **Decision**:
  - Soft-delete enabled entities: Provide `DELETE` endpoint and service method. When executed, set flag to `true`, update `UpdatedAt` / `UpdatedBy` (if available), and save. Standard queries filter by `!DeleteFlag` or `!IsDeleted`.
  - Entities lacking delete flags (`Branch`, `Shift`, `StaffShift`): Prohibit deletion via API. Omit `DELETE` endpoints from `BranchesController` and `ShiftsController`. If an internal delete attempt occurs, return `Result.Failure("Deletion is not supported for this entity.")`.
- **Alternatives Considered**:
  - *Altering DB schema to add DeleteFlag to Branch and Shift*: Rejected per user decision Q1: Option A.
  - *Hard deletion*: Rejected to prevent cascading record breaks in live systems.

---

### Decision 4: Audit Tracking & Metadata Mapping
- **Problem**: Certain tables include audit metadata (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`), while others (`Category`, `Role`, `Permission`, `StaffShift`) do not.
- **Decision**:
  - Entities with audit fields:
    - On Creation: Set `CreatedAt = DateTime.UtcNow` and `CreatedBy = request.CreatedBy ?? "System"`.
    - On Update: Set `UpdatedAt = DateTime.UtcNow` and `UpdatedBy = request.UpdatedBy ?? "System"`.
  - Entities without audit fields (`Category`, `Role`, `Permission`, `StaffShift`): Do not attempt to persist audit fields to avoid phantom properties or mapping errors.
  - In DTOs: Expose `CreatedAt` in response DTOs where available in the underlying entity.

---

### Decision 5: Staff and StaffRole Decoupling
- **Problem**: The `Staff` table schema links to `Branch` via `BranchCode`, but contains no `StaffRoleCode` or `StaffRoleId`.
- **Decision**:
  - Maintain `StaffRole` as an independent reference catalog of organizational roles and compensation bands.
  - `StaffService` and `StaffRoleService` operate independently.
  - API exposes `/api/staff` for staff members and `/api/staff/roles` for staff role catalog management.

---

### Decision 6: Inactive / Legacy Modules Strategy
- **ModuPOS.Sale**: Kept as an inactive stub library. The `Sale` and `SaleItem` tables were removed from `db.sql`. No services or controllers are implemented until future sales schema design.
- **ModuPOS.Loyalty** & **ModuPOS.Reporting**: Kept as inactive stubs (`Class1.cs`).
- **ModuPOS.FeatureManagement** & **ModuPOS.LicenseManagement**: Maintain existing cryptographic license validation logic and in-memory feature checking, decoupled from the current DB context until tenant/customer tables are reintroduced in a future schema version.
