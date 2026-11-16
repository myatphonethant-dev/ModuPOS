# Tasks: Database Feature Alignment and Shared Contract Standardization

**Input**: Design documents from `specs/002-database-feature-alignment/`
**Prerequisites**: [plan.md](file:///d:/ModuPOS/specs/002-database-feature-alignment/plan.md), [spec.md](file:///d:/ModuPOS/specs/002-database-feature-alignment/spec.md), [research.md](file:///d:/ModuPOS/specs/002-database-feature-alignment/research.md), [data-model.md](file:///d:/ModuPOS/specs/002-database-feature-alignment/data-model.md), [contracts/](file:///d:/ModuPOS/specs/002-database-feature-alignment/contracts/)

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (`[US1]`, `[US2]`, `[US3]`, `[US4]`)
- Every task includes explicit file paths

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify and ensure shared envelopes and pagination primitives in `ModuPOS.Shared` are fully ready for consumption across all modules.

- [X] T001 [P] Verify and enhance pagination metadata calculation and `PagedResult<T>` factory methods in `ModuPOS.Shared/Pagination/Pagination.cs`
- [X] T002 [P] Verify strongly-typed `Result`, `Result<T>`, and JSON constructor attributes in `ModuPOS.Shared/Results/Result.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Update service contracts and resolve the 79 compilation errors across all domain libraries so the solution compiles cleanly.

**⚠️ CRITICAL**: No user story implementation or testing can run until foundational compilation is achieved.

- [X] T003 [P] Update service interfaces in `ModuPOS.Inventory/Services/Category/CategoryService.cs` and `ModuPOS.Inventory/Services/Product/ProductService.cs` to return `Task<PagedResult<CategoryResponseDto>>` and `Task<PagedResult<ProductResponseDto>>`
- [X] T004 [P] Update service interfaces in `ModuPOS.Organization/Services/Branch/BranchService.cs`, `ModuPOS.Organization/Services/Staff/StaffService.cs`, `ModuPOS.Organization/Services/Staff/StaffRoleService.cs`, and `ModuPOS.Organization/Services/Shift/ShiftService.cs` to return `Task<PagedResult<T>>`
- [X] T005 [P] Update service interfaces in `ModuPOS.Auth/Services/Auth/AuthService.cs`, `ModuPOS.Auth/Services/User/UserService.cs`, `ModuPOS.Auth/Services/Role/RoleService.cs`, and `ModuPOS.Auth/Services/Permission/PermissionService.cs` to return `Task<PagedResult<T>>` and `Task<Result<T>>`
- [X] T006 Resolve all `Result.Failure<T>` and `Result.Success` compiler errors across `ModuPOS.Inventory`, `ModuPOS.Organization`, `ModuPOS.Auth`, and `ModuPOS.Api` to achieve clean compilation via `dotnet build Lineup.slnx -c Release --no-restore`

**Checkpoint**: Solution compiles with 0 errors. Foundation ready for user story implementations.

---

## Phase 3: User Story 1 - Standardized Operational Querying with Pagination (Priority: P1) 🎯 MVP

**Goal**: Enable bounded, paginated retrieval with total item counts, page indices, and navigation flags across 100% of collection endpoints.

**Independent Test**: Execute queries against Products, Staff, and Branches with `pageNumber=1&pageSize=10`; verify exactly 10 records are returned along with total count, total pages, and navigation flags.

### Tests for User Story 1

- [X] T007 [P] [US1] Add unit tests for `PaginationRequest` clamping (`pageNumber >= 1`, `1 <= pageSize <= 100`) and `PagedResult<T>` envelope validation in `tests/ModuPOS.Auth.UnitTests/ProductServiceTests.cs`

### Implementation for User Story 1

- [X] T008 [P] [US1] Implement paginated `GetCategoriesAsync(PaginationRequest request, CancellationToken ct)` using EF Core `CountAsync` and `Skip`/`Take` in `ModuPOS.Inventory/Services/Category/CategoryService.cs`
- [X] T009 [P] [US1] Implement paginated `GetProductsAsync(int? categoryId, string? search, PaginationRequest request, CancellationToken ct)` in `ModuPOS.Inventory/Services/Product/ProductService.cs`
- [X] T010 [P] [US1] Implement paginated `GetBranchesAsync(PaginationRequest request, CancellationToken ct)` in `ModuPOS.Organization/Services/Branch/BranchService.cs`
- [X] T011 [P] [US1] Implement paginated `GetStaffListAsync(string? branchCode, PaginationRequest request, CancellationToken ct)` in `ModuPOS.Organization/Services/Staff/StaffService.cs`
- [X] T012 [P] [US1] Implement paginated `GetStaffRolesAsync(PaginationRequest request, CancellationToken ct)` in `ModuPOS.Organization/Services/Staff/StaffRoleService.cs`
- [X] T013 [P] [US1] Implement paginated `GetShiftsAsync(PaginationRequest request, CancellationToken ct)` and `GetStaffShiftsAsync(string? shiftCode, PaginationRequest request, CancellationToken ct)` in `ModuPOS.Organization/Services/Shift/ShiftService.cs`
- [X] T014 [P] [US1] Implement paginated `GetUsersAsync(PaginationRequest request, CancellationToken ct)` in `ModuPOS.Auth/Services/User/UserService.cs`
- [X] T015 [P] [US1] Implement paginated `GetRolesAsync(PaginationRequest request, CancellationToken ct)` and `GetPermissionsAsync(PaginationRequest request, CancellationToken ct)` in `ModuPOS.Auth/Services/Role/RoleService.cs` and `ModuPOS.Auth/Services/Permission/PermissionService.cs`
- [X] T016 [P] [US1] Update `CategoriesController.cs` and `ProductsController.cs` in `ModuPOS.Api/Controllers/` to accept `[FromQuery] PaginationRequest request` and return `PagedResult<T>`
- [X] T017 [P] [US1] Update `BranchesController.cs`, `StaffController.cs`, and `ShiftsController.cs` in `ModuPOS.Api/Controllers/` to accept `[FromQuery] PaginationRequest request` and return `PagedResult<T>`
- [X] T018 [P] [US1] Update `UsersController.cs`, `RolesController.cs`, and `PermissionsController.cs` in `ModuPOS.Api/Controllers/` to accept `[FromQuery] PaginationRequest request` and return `PagedResult<T>`

**Checkpoint**: User Story 1 complete. All 10 collection endpoints return paginated responses.

---

## Phase 4: User Story 2 - Uniform Soft-Delete and Record Lifecycle Management (Priority: P1)

**Goal**: Enforce soft-deletion filtering on all queries for entities with delete flags, apply audit tracking timestamps upon deletion, and prohibit deletion of Branch and Shift entities.

**Independent Test**: Delete an active product, category, staff member, and user; verify they no longer appear in standard listings, lookups return 404, and audit columns (`UpdatedAt`, `UpdatedBy`) are updated. Verify `DELETE /api/branches/{id}` is rejected/absent.

### Tests for User Story 2

- [X] T019 [P] [US2] Add unit tests verifying `DeleteFlag == false` query filtering and soft-delete mutation in `tests/ModuPOS.Auth.UnitTests/ProductServiceTests.cs`

### Implementation for User Story 2

- [X] T020 [P] [US2] Enforce `DeleteFlag == false` filtering and soft-delete mutation with UTC timestamps in `ModuPOS.Inventory/Services/Category/CategoryService.cs` and `ModuPOS.Inventory/Services/Product/ProductService.cs`
- [X] T021 [P] [US2] Enforce `DeleteFlag == false` filtering and soft-delete mutation (`DeleteStaffAsync`, `DeleteStaffRoleAsync`) in `ModuPOS.Organization/Services/Staff/StaffService.cs` and `ModuPOS.Organization/Services/Staff/StaffRoleService.cs`
- [X] T022 [P] [US2] Enforce `is_deleted == false` filtering and soft-delete mutation (`DeleteUserAsync`, `DeleteRoleAsync`) in `ModuPOS.Auth/Services/User/UserService.cs` and `ModuPOS.Auth/Services/Role/RoleService.cs`
- [X] T023 [P] [US2] Ensure `ModuPOS.Organization/Services/Branch/BranchService.cs` and `ModuPOS.Api/Controllers/BranchesController.cs` have no delete endpoint and prohibit branch deletion per Decision 3 (Q1: Option A)
- [X] T024 [P] [US2] Ensure `ModuPOS.Organization/Services/Shift/ShiftService.cs` and `ModuPOS.Api/Controllers/ShiftsController.cs` have no delete endpoint and prohibit shift deletion per Decision 3 (Q1: Option A)

**Checkpoint**: User Story 2 complete. Soft-delete rules and deletion prohibitions are enforced consistently.

---

## Phase 5: User Story 3 - Comprehensive Organization & Shift Scheduling (Priority: P2)

**Goal**: Provide operational shift scheduling, staff-shift assignments, and standalone staff role catalog management.

**Independent Test**: Create a shift, assign a staff member using `AssignStaffShiftAsync`, verify assignment in `GetStaffShiftsAsync`, and create/update staff roles and compensation.

### Tests for User Story 3

- [X] T025 [P] [US3] Add unit tests for shift scheduling, assignment validation, and staff role CRUD in `tests/ModuPOS.Auth.UnitTests/ShiftServiceTests.cs`

### Implementation for User Story 3

- [X] T026 [P] [US3] Implement shift creation and update with shift code uniqueness and UTC audit tracking in `ModuPOS.Organization/Services/Shift/ShiftService.cs`
- [X] T027 [P] [US3] Implement `AssignStaffShiftAsync` with validation checking active `StaffCode` and `ShiftCode` in `ModuPOS.Organization/Services/Shift/ShiftService.cs`
- [X] T028 [P] [US3] Implement `StaffRoleService` CRUD operations for role definitions and salary bands (`decimal(18,2)`) in `ModuPOS.Organization/Services/Staff/StaffRoleService.cs`
- [X] T029 [P] [US3] Expose shift assignment (`POST /api/shifts/assign`) and staff role routes (`/api/staff/roles`) in `ModuPOS.Api/Controllers/ShiftsController.cs` and `ModuPOS.Api/Controllers/StaffController.cs`

**Checkpoint**: User Story 3 complete. Shifts, shift assignments, and staff roles operate smoothly.

---

## Phase 6: User Story 4 - Consistent Operational Responses and Error Communication (Priority: P1)

**Goal**: Ensure every endpoint across all controllers strictly adheres to the unified Result pattern, returning standard JSON envelopes and appropriate HTTP status codes.

**Independent Test**: Execute valid requests, missing ID lookups, and invalid inputs across Auth, Inventory, and Organization endpoints; verify consistent `isSuccess`, `message`, and `data` fields with HTTP 200, 201, 400, and 404 codes.

### Tests for User Story 4

- [X] T030 [P] [US4] Update integration tests in `tests/ModuPOS.Api.IntegrationTests/ApiIntegrationTests.cs` to assert unified `Result` and `PagedResult` response schemas

### Implementation for User Story 4

- [X] T031 [P] [US4] Update `ModuPOS.Auth/Services/Auth/AuthService.cs` to return `Result<LoginResponse>.Success` and `Result<LoginResponse>.Failure` with proper JWT and refresh token generation
- [X] T032 [P] [US4] Standardize controller action returns (`Ok(result)`, `CreatedAtAction(...)`, `NotFound(result)`, `BadRequest(result)`) across all 9 controllers in `ModuPOS.Api/Controllers/`

**Checkpoint**: User Story 4 complete. Universal Result contract and error messaging active.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete documentation, verify coding standards, and execute full end-to-end validation.

- [X] T033 [P] Update `docs/project_architecture_and_overview.md` replacing template placeholders (`{{FEATURES_LIST}}`) with the active features registry and database mapping notes
- [X] T034 [P] Verify coding standards compliance (file-scoped namespaces, XML documentation on public entities, lines <= 225 chars) across all modified files
- [X] T035 Run complete test suite and build verification:
  ```powershell
  dotnet build -c Release
  dotnet test tests/ModuPOS.Auth.UnitTests/ModuPOS.Auth.UnitTests.csproj -c Release
  dotnet test tests/ModuPOS.Api.IntegrationTests/ModuPOS.Api.IntegrationTests.csproj -c Release
  ```
- [X] T036 Execute validation scenarios documented in `specs/002-database-feature-alignment/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1 (Setup)**: Independent — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS all user stories**.
- **Phase 3 (User Story 1 - Pagination)**: Depends on Phase 2. Forms the core MVP increment.
- **Phase 4 (User Story 2 - Soft-Delete)**: Depends on Phase 2. Can execute in parallel with or sequentially after US1.
- **Phase 5 (User Story 3 - Shifts & Roles)**: Depends on Phase 2. Can execute in parallel with or after US1/US2.
- **Phase 6 (User Story 4 - Result Envelopes)**: Depends on Phase 2. Integrates with US1, US2, and US3.
- **Phase 7 (Polish)**: Depends on all user stories being implemented.

### Parallel Execution Opportunities
- Tasks marked `[P]` operate on distinct files without shared state dependencies.
- Once Phase 2 (Foundational) is complete:
  - Developer A can implement Inventory pagination and soft-delete (`T008`, `T009`, `T016`, `T020`).
  - Developer B can implement Organization features and shifts (`T010`, `T011`, `T012`, `T013`, `T017`, `T021`, `T026-T029`).
  - Developer C can implement Auth features and user pagination (`T014`, `T015`, `T018`, `T022`, `T031`).

---

## Implementation Strategy

### MVP Scope (Phases 1, 2, and 3)
1. Complete Setup (`T001-T002`) and Foundational compilation fixes (`T003-T006`).
2. Implement User Story 1 (`T007-T018`) to deliver fully paginated collection querying across all endpoints.
3. Validate User Story 1 independently with test queries before proceeding.
