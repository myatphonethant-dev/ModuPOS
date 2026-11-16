# Feature Specification: Database Feature Alignment and Shared Contract Standardization

**Feature Branch**: `002-database-feature-alignment`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "According to the database changes, we have to make changes to the features. Scan the whole repo first and make a list to apply changes."

## Clarifications

### Session 2026-11-16

- **Q1: Branch and Shift Deletion Semantics**: How should the system handle deletion requests for Branches and Shifts given that the database schema lacks soft-delete columns?
  - **Resolution**: Option A — Prohibit deletion via API (Read / Create / Update only). Preserves existing database schema unchanged; branches and shifts cannot be removed through the API once created.
- **Q2: Pagination Scope Across Collections**: Should small system lookup catalogs (`Permissions`, `Roles`, `StaffRoles`) be paginated or return full unpaginated lists?
  - **Resolution**: Option B — Enforce pagination across 100% of collection endpoints. Every collection endpoint returns `PagedResult<T>` with `PaginationRequest` for uniform client contracts.
- **Q3: Staff and Staff Role Association**: How should staff members be associated with staff roles given the current database schema structure?
  - **Resolution**: Option A — Keep `StaffRole` as a standalone organizational reference catalog for now. No database schema changes; staff records and staff roles remain independently managed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Standardized Operational Querying with Pagination (Priority: P1)

A store manager or inventory operator needs to browse large catalogs of products, staff rosters, branches, and customer accounts. Instead of experiencing performance degradation or unmanageable lists when data grows, the system returns well-organized, paginated records with total item counts, current page indicators, and page navigation metadata. The user can request specific page numbers and page sizes to inspect manageable batches of data smoothly.

**Why this priority**: Large operational tables (products, staff, audit trails, user accounts) degrade system responsiveness and client usability if loaded without bounded pagination. Aligning query operations to standardized pagination contracts is essential for production viability.

**Independent Test**: Request page 1 with 10 records for products and staff; verify exactly 10 records are returned along with total count, total pages, and navigation flags. Request page 2 and verify next sequence of records.

**Acceptance Scenarios**:

1. **Given** a catalog containing 50 products, **When** an operator requests the first page with a page size of 10, **Then** the system returns the first 10 products, indicates page 1 of 5, reports 50 total records, and confirms a next page exists.
2. **Given** an operator viewing the last page of a staff roster, **When** the page is loaded, **Then** the system indicates no next page is available and previous page is available.
3. **Given** an invalid or negative page number, **When** a query is issued, **Then** the system falls back safely to page 1 with standard page size rather than failing.

---

### User Story 2 - Uniform Soft-Delete and Record Lifecycle Management (Priority: P1)

An administrator manages active records across organization, inventory, and identity domains. When a product, category, staff member, or user account is removed, the system safely marks the record as inactive or deleted without losing historical associations. Deleted items are immediately excluded from standard listings and searches, while preventing duplicate code conflicts according to domain rules.

**Why this priority**: Preserves transactional integrity, historical audits, and consistent behavior across modules according to the underlying database lifecycle rules.

**Independent Test**: Deactivate/delete an existing product and staff member; verify they no longer appear in standard listings, cannot be retrieved via normal lookups, and historical linkages remain intact.

**Acceptance Scenarios**:

1. **Given** an active product in the catalog, **When** an administrator deletes the product, **Then** the product is marked as deleted and immediately hidden from subsequent catalog searches.
2. **Given** a deleted staff record, **When** an operator queries the active branch roster, **Then** the deleted staff member is not included.
3. **Given** a deleted category, **When** a manager creates a new category with the same code, **Then** the system handles code validation according to domain uniqueness rules without data corruption.

---

### User Story 3 - Comprehensive Organization & Shift Scheduling (Priority: P2)

A store supervisor manages branch operations by configuring work shifts (e.g., Morning, Evening, Night) and assigning staff members to specific shifts using standardized identifiers. The supervisor can review shift rosters, inspect staff assigned to each shift, and manage staff roles and salary bands independently.

**Why this priority**: Accurate scheduling and staff allocation are critical to daily retail store operations and workforce management.

**Independent Test**: Create a new operational shift, assign two staff members to the shift, and verify the shift roster accurately reflects both staff members with their names and codes.

**Acceptance Scenarios**:

1. **Given** an active shift and an active staff member, **When** the supervisor assigns the staff member to the shift, **Then** the assignment is recorded and visible in shift roster inquiries.
2. **Given** an assignment attempt with a non-existent staff code or shift code, **When** submitted, **Then** the system rejects the assignment with a descriptive domain error.
3. **Given** an existing staff role with defined compensation, **When** an administrator updates the role title or salary band, **Then** the updated details are accurately persisted and reflected in future inquiries.

---

### User Story 4 - Consistent Operational Responses and Error Communication (Priority: P1)

A client application (web dashboard or POS terminal) executes operations such as user authentication, staff creation, or product updates. The system uniformly responds with standardized success or failure outcomes, clear messages, and strongly-typed payloads. When business rule violations occur, meaningful explanatory messages are delivered consistently across all endpoints.

**Why this priority**: Reliable client-server contract prevents silent failures, crashes, and inconsistent error handling across front-end and POS terminals.

**Independent Test**: Submit valid and invalid requests across Auth, Organization, and Inventory modules; verify every response adheres strictly to the unified operational result contract with consistent success flags, message fields, and data packaging.

**Acceptance Scenarios**:

1. **Given** a valid entity creation request, **When** submitted, **Then** the system returns a success outcome with the created entity details and a confirmation message.
2. **Given** a business rule validation failure (such as a duplicate code or invalid foreign reference), **When** submitted, **Then** the system returns a failure outcome with an informative error message and no payload data.
3. **Given** an operation that yields no data (such as revoking a token or deleting an item), **When** completed, **Then** the system returns a successful result status with a completion message.

---

### Edge Cases

- What happens when a user requests a page number beyond the total number of pages? The system returns an empty data collection with accurate pagination metadata indicating total pages and count.
- How does the system handle concurrent updates to the same operational record? Changes are applied atomically, and if a record has been marked deleted concurrently, subsequent update operations return a clear record-not-found error.
- What happens if a staff member is assigned to a shift that has no defined start or end times? The system accepts valid shift codes regardless of whether optional scheduling timestamps are populated.
- How does the system handle product category assignment when a category is deleted? Products assigned to a deleted category either retain their historical category identifier or report category as unassigned in read models.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide paginated query capabilities for Products, Categories, Staff, Branches, Shifts, Staff Shifts, and Users, accepting page number and page size parameters.
- **FR-002**: System MUST return total item counts, total pages, current page number, and page size along with paginated query results.
- **FR-003**: System MUST standardize all operational service outputs to conform to the unified Result pattern (`Result` for operations without payload, `Result<T>` for operations returning a single entity, and `PagedResult<T>` for paginated collections).
- **FR-004**: System MUST align domain entity field mappings with the database schema, including audit tracking (creation timestamp and creator identity, update timestamp and updater identity) where supported by the entity.
- **FR-005**: System MUST enforce soft-deletion rules for entities possessing delete flags (`DeleteFlag` or `is_deleted`), filtering them out of active business queries by default.
- **FR-006**: System MUST prohibit deletion of `Branch` and `Shift` entities via the API, restricting their life-cycle operations to Read, Create, and Update only in accordance with the underlying schema without soft-delete columns.
- **FR-007**: System MUST provide comprehensive management of staff roles and compensations via dedicated domain operations and endpoints.
- **FR-008**: System MUST support associating staff members with work shifts via staff shift assignments using unique business codes.
- **FR-009**: System MUST support query filtering by branch, category, and search terms while maintaining pagination consistency.
- **FR-010**: System MUST enforce pagination across 100% of collection query endpoints (including `Products`, `Categories`, `Branches`, `Staff`, `StaffRoles`, `Shifts`, `StaffShifts`, `Users`, `Roles`, and `Permissions`), accepting standard `PaginationRequest` parameters and returning uniform `PagedResult<T>` envelopes.
- **FR-011**: System MUST maintain `StaffRole` records as an independent reference catalog of job roles and compensation definitions without direct foreign key binding to individual staff members in the current schema.

### Key Entities

- **Product**: Represents a sellable inventory item with unique code, name, category reference, price, quantity on hand, image URL, soft-delete state, and audit metadata.
- **Category**: Represents a classification for inventory items with unique code, name, and soft-delete state.
- **Branch**: Represents an operational store or location with unique code, name, physical address, and audit tracking.
- **Staff**: Represents an employee with personal details, contact info, branch affiliation code, active status, soft-delete flag, and audit metadata.
- **StaffRole**: Represents an organizational role profile with code, title, salary compensation, soft-delete flag, and audit metadata.
- **Shift**: Represents a defined work period with unique code, shift type, start/end timestamps, and audit metadata.
- **StaffShift**: Represents an operational assignment linking a staff member to a scheduled shift.
- **User**: Represents a system authentication identity with username, hashed credentials, email, active flag, soft-delete flag, role assignment, and audit timestamps.
- **Role & Permission**: Represents system security roles and granular functional permissions mapped through role-permission associations.
- **RefreshToken**: Represents an authentication renewal token with expiration timestamp and revocation tracking.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of collection query operations across Auth, Organization, and Inventory return paginated responses within standard performance thresholds.
- **SC-002**: 100% of domain services and API endpoints adhere strictly to the unified operational Result contract without runtime serialization or compilation defects.
- **SC-003**: System builds cleanly with zero compiler warnings or errors across all solution projects and automated test suites.
- **SC-004**: Automated unit and integration test suites cover all CRUD, pagination, filtering, and error handling paths for all 12 database entity features.
- **SC-005**: All soft-deleted records are verified to be excluded from 100% of standard active operational listings.

## Assumptions

- Database schema definitions in `docs/db_tables.md` and `ModuPOS.Database/db.sql` represent the single source of truth for entity storage layout.
- Sale and SaleItem modules remain out of scope for this alignment phase as they were intentionally removed from the database schema.
- Modules without active database tables (`ModuPOS.Loyalty`, `ModuPOS.Reporting`) remain inactive stubs until future schema iterations define their storage models.
- Standard default page size is 10 records per page with a maximum allowable page size of 100 records to prevent resource exhaustion.
- Authentication mechanisms will continue to rely on JWT access tokens and database-backed refresh tokens.

---

## Detailed Repository Scan & Change Register

### 1. Database Schema Status & Table Inventory

The database defines 12 core tables in `ModuPOS.Database/db.sql` and `docs/db_tables.md`:

| # | Table Name | C# Model | Primary Key | Soft Delete Column | Audit Columns Present |
|---|---|---|---|---|---|
| 1 | `Role` | `Role.cs` | `id` (int) | `is_deleted` | No |
| 2 | `Permission` | `Permission.cs` | `id` (int) | `is_deleted` | No |
| 3 | `Role_Permission` | `RolePermission.cs` | `id` (int) | `is_deleted` | No |
| 4 | `User` | `User.cs` | `id` (int) | `is_deleted` | `created_at`, `created_by`, `updated_at`, `updated_by` |
| 5 | `Refresh_Token` | `RefreshToken.cs` | `id` (int) | Revocation timestamp (`revoked_at`) | `created_at` |
| 6 | `Branch` | `Branch.cs` | `BranchId` (int) | None (Missing) | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |
| 7 | `Staff` | `Staff.cs` | `StaffId` (int) | `DeleteFlag` | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |
| 8 | `StaffRole` | `StaffRole.cs` | `StaffRoleId` (int) | `DeleteFlag` | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |
| 9 | `Shift` | `Shift.cs` | `ShiftId` (int) | None (Missing) | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |
| 10 | `StaffShift` | `StaffShift.cs` | `StaffShiftId` (int) | None | None |
| 11 | `Category` | `Category.cs` | `CategoryId` (int) | `DeleteFlag` | None |
| 12 | `Product` | `Product.cs` | `ProductId` (int) | `DeleteFlag` | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |

### 2. Discrepancies and Required Code Changes by Module

#### A. Shared Infrastructure (`ModuPOS.Shared`)
- **Issue**: Commit `58368de` introduced breaking changes in `Result.cs` and added `Pagination.cs` / `PaginationRequest.cs`.
- **Changes Required**:
  1. Fix Result invocation patterns across all features: Replace `Result.Failure<T>(msg)` with `Result<T>.Failure(msg)`.
  2. Replace `Result.Success(data)` for payloads with `Result<T>.Success(data)`.
  3. Ensure `PagedResult<T>` is cleanly integrated with `PaginationRequest`.

#### B. Inventory Module (`ModuPOS.Inventory`)
- **Files**: `CategoryService.cs`, `ProductService.cs`, `DTOs/CategoryDTO.cs`, `DTOs/ProductDTO.cs`
- **Changes Required**:
  1. `ICategoryService`: Add pagination parameter `PaginationRequest` to `GetCategoriesAsync`, returning `PagedResult<CategoryResponseDto>`.
  2. `IProductService`: Add pagination parameter `PaginationRequest` to `GetProductsAsync`, returning `PagedResult<ProductResponseDto>`.
  3. Update service implementations to compute total counts via EF Core `CountAsync` and project pages using `Skip`/`Take`.
  4. Fix all Result pattern return types.

#### C. Organization Module (`ModuPOS.Organization`)
- **Files**: `BranchService.cs`, `StaffService.cs`, `StaffRoleService.cs`, `ShiftService.cs`, DTOs
- **Changes Required**:
  1. `IBranchService`: Add pagination to `GetBranchesAsync` returning `PagedResult<BranchResponseDto>`. Clarify deletion behavior (no soft-delete column in DB).
  2. `IStaffService`: Add pagination to `GetStaffListAsync` returning `PagedResult<StaffResponseDto>`.
  3. `IStaffRoleService`: Add pagination or standardized list returning `PagedResult<StaffRoleResponseDto>` or `Result<List<StaffRoleResponseDto>>`.
  4. `IShiftService`: Add pagination to `GetShiftsAsync` and `GetStaffShiftsAsync`. Clarify deletion behavior for Shift (no soft-delete column in DB).
  5. Fix all Result pattern return types across all 4 services.

#### D. Authentication & Identity Module (`ModuPOS.Auth`)
- **Files**: `AuthService.cs`, `UserService.cs`, `RoleService.cs`, `PermissionService.cs`, DTOs
- **Changes Required**:
  1. `IUserService`: Add pagination to `GetUsersAsync` returning `PagedResult<UserResponseDto>`.
  2. `IRoleService`: Align `GetRolesAsync` with pagination or standardized collection return.
  3. `IPermissionService`: Fix Result pattern return types.
  4. `AuthService`: Fix `Result<LoginResponse>.Success` and `Result<LoginResponse>.Failure` syntax.

#### E. API Layer (`ModuPOS.Api`)
- **Files**: All controllers in `Controllers/`
- **Changes Required**:
  1. `CategoriesController`: Accept `[FromQuery] PaginationRequest request` on `GetAll`, returning `PagedResult<CategoryResponseDto>`.
  2. `ProductsController`: Accept `[FromQuery] PaginationRequest request`, category filter, and search term.
  3. `BranchesController`: Accept `[FromQuery] PaginationRequest request`.
  4. `StaffController`: Accept `[FromQuery] PaginationRequest request` for staff and staff roles.
  5. `ShiftsController`: Accept `[FromQuery] PaginationRequest request` for shifts and staff shifts.
  6. `UsersController`: Accept `[FromQuery] PaginationRequest request`.
  7. Align response unpacking (`result.IsSuccess ? Ok(result) : BadRequest(result)`).

#### F. Inactive / Removed Modules
- `ModuPOS.Sale`: Retain as inactive stub; document that Sale tables were removed from database.
- `ModuPOS.Loyalty`: Retain as inactive stub.
- `ModuPOS.Reporting`: Retain as inactive stub.
- `ModuPOS.FeatureManagement`: Align with in-memory or document prerequisite of customer/tenant DB schema if feature toggling is to be re-activated.

#### G. Test Suites (`tests/`)
- **Files**: `tests/ModuPOS.Auth.UnitTests/`, `tests/ModuPOS.Api.IntegrationTests/`
- **Changes Required**:
  1. Update existing tests in `AuthServiceTests` and `ProductServiceTests` to match new Result syntax and pagination structures.
  2. Add unit tests for `CategoryService`, `BranchService`, `StaffService`, `StaffRoleService`, `ShiftService`, `UserService`, `RoleService`, `PermissionService`.
  3. Update integration tests in `ApiIntegrationTests` to assert paginated response schemas.
