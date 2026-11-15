# Tasks: Authentication and Dynamic Roles & Permissions

**Input**: Design documents from `specs/001-auth-dynamic-rbac/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Included. The plan (R12) and quickstart require integration tests for every acceptance scenario and for SC-004, SC-005 and SC-007. Within each story, write tests first and confirm they fail.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on unfinished tasks)
- **[Story]**: user story from spec.md (US1–US7)

## Path Conventions

Modular monolith at repo root: `ModuPOS.Shared/`, `ModuPOS.Database/`, `ModuPOS.Organization/`, `ModuPOS.Auth/`, `ModuPOS.Api/`, tests in `tests/`.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Create class library `ModuPOS.Organization/ModuPOS.Organization.csproj` (net10.0, Nullable/ImplicitUsings enabled) and add it to `ModuPOS.slnx`
- [X] T002 Create `tests/ModuPOS.Auth.UnitTests/ModuPOS.Auth.UnitTests.csproj` (xUnit v3) and `tests/ModuPOS.Api.IntegrationTests/ModuPOS.Api.IntegrationTests.csproj` (xUnit v3, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.MsSql`); add both to `ModuPOS.slnx`
- [X] T003 Add packages: `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Design` to `ModuPOS.Database/ModuPOS.Database.csproj`; `Microsoft.Extensions.Identity.Core`, `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.EntityFrameworkCore` to `ModuPOS.Auth/ModuPOS.Auth.csproj`; `Microsoft.AspNetCore.Authentication.JwtBearer` to `ModuPOS.Api/ModuPOS.Api.csproj`
- [X] T004 Wire project references: Database → Shared; Organization → Shared, Database; Auth → Shared, Database, Organization, FeatureManagement, LicenseManagement; Api → Auth, Organization; IntegrationTests → Api; UnitTests → Auth
- [X] T005 [P] Delete placeholders `ModuPOS.Auth/Class1.cs`, `ModuPOS.Database/Class1.cs`, `ModuPOS.Shared/Class1.cs`, `ModuPOS.Api/WeatherForecast.cs`, `ModuPOS.Api/Controllers/WeatherForecastController.cs`
- [X] T006 [P] Add `ConnectionStrings:Default`, `Jwt` (Issuer, Audience, ES256 private/public key PEM, AccessTokenMinutes=15, RefreshTokenDays=7) and `Auth:Lockout` (MaxFailures=5, WindowMinutes=15, LockMinutes=15) sections to `ModuPOS.Api/appsettings.json` and dev values to `ModuPOS.Api/appsettings.Development.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ No user story work can begin until this phase is complete.**

- [X] T007 [P] Create `ITenantContext` (UserId, ProfileId, ProfileType, BusinessId?, BranchId?, SessionId, Permissions set, IsAuthenticated) in `ModuPOS.Shared/Tenancy/ITenantContext.cs` and a scoped mutable implementation `ModuPOS.Shared/Tenancy/TenantContext.cs`
- [X] T008 [P] Create `Result`/`Error(code, message, status)` types in `ModuPOS.Shared/Results/Result.cs` and a problem-details mapper (RFC 9457 with `code` extension) in `ModuPOS.Api/Infrastructure/ProblemDetailsMapper.cs`
- [X] T009 [P] Create `IAuditWriter` with `WriteCritical(AuditEntry)` and `Enqueue(AuditEntry)` plus `AuditEntry` record (Code, Severity `Info|Warning|High`, OccurredAt, Outcome `Succeeded|Denied|Blocked`, ActorUserId?, ActorProfileId?, BusinessId?, BranchId?, SessionId?, TargetType?, TargetId?, SourceAddress?, DeviceDescription?, Details JSON, RepeatCount ≥ 1) in `ModuPOS.Shared/Auditing/`
- [X] T010 [P] Create audit code constants for the full FR-033 catalog (`AUTH.SIGNIN_SUCCEEDED`, `AUTH.SIGNIN_FAILED`, `AUTH.ACCOUNT_LOCKED`, `AUTH.SIGNOUT`, `AUTH.SIGNOUT_ALL`, `AUTH.RENEWAL_REUSE_DETECTED`, `AUTH.SESSIONS_REVOKED`, `BRANCH.SELECTED`, `ROLE.CREATED`, `ROLE.UPDATED`, `ROLE.PERMISSIONS_CHANGED`, `ROLE.DEACTIVATED`, `ROLE.REACTIVATED`, `ROLE.DELETED`, `ROLE_ASSIGNMENT.GRANTED`, `ROLE_ASSIGNMENT.REVOKED`, `ACCESS.PERMISSION_DENIED`, `ACCESS.CROSS_TENANT_DENIED`, `ACCESS.BRANCH_DENIED`, `SECURITY.ESCALATION_BLOCKED`, `SECURITY.OWNER_PROTECTION_BLOCKED`, `AUDIT.VIEWED_BY_PLATFORM`) with an `IsCritical` set (all `ROLE.*`, `ROLE_ASSIGNMENT.*`) in `ModuPOS.Auth/Auditing/AuditCodes.cs`
- [X] T011 [P] Create Organization entities in `ModuPOS.Organization/Entities/`: `Business` (Id, Name required, Status `Active|Suspended|Deactivated`), `Branch` (Id, BusinessId, Name "unique per business", Status `Active|Inactive`), `StaffBranchAssignment` (composite PK StaffProfileId+BranchId, AssignedAt)
- [X] T012 [P] Create Identity entities in `ModuPOS.Auth/Accounts/`: `UserAccount` (Username nvarchar(50) unique "3–50 chars", Email nvarchar(256) unique, PasswordHash, Status `Active|Suspended|Deactivated`, FailedSignInCount, FailureWindowStart?, LockedUntil?, CreatedAt) and `Profile` (UserAccountId, Type `PlatformAdmin|Staff|Member`, BusinessId? "required for Staff/Member, null for PlatformAdmin", Status `Active|Inactive`; unique (UserAccountId, Type, BusinessId))
- [X] T013 [P] Create Session entities in `ModuPOS.Auth/Sessions/`: `Session` (Id=JWT sid, UserAccountId, ActiveProfileId?, ActiveBranchId?, Status `Active|Revoked|Expired`, RevokedReason? `SignOut|SignOutAll|RenewalReuse|AccountSuspended|BusinessSuspended`, CreatedAt, LastSeenAt, ExpiresAt, SourceAddress, DeviceDescription) and `RefreshToken` (SessionId, TokenHash varbinary(32) unique SHA-256, IssuedAt, ExpiresAt, UsedAt?, ReplacedById?)
- [X] T014 [P] Create RBAC entities in `ModuPOS.Auth/Roles/`: `Permission` (Code PK lowercase `module.action`, Name, Group, FeatureCode, DependsOn JSON array, Scope `Business|Platform`, IsRetired), `Role` (BusinessId, Name "2–60 chars, unique per business (case-insensitive)", Description? "≤ 500 chars", IsSystem, IsOwner, IsActive, Version=rowversion), `RolePermission` (RoleId+PermissionCode PK), `RoleAssignment` (StaffProfileId, RoleId, BranchId? "null = business scope", AssignedAt, AssignedByProfileId; unique (StaffProfileId, RoleId, BranchId)), `AuthorizationVersion` (BusinessId PK, Value bigint)
- [X] T015 [P] Create persisted `AuditEntryRecord` entity in `ModuPOS.Auth/Auditing/AuditEntryRecord.cs` with `DedupKey?` and indexes (BusinessId, OccurredAt desc), (Code, OccurredAt), (ActorUserId, OccurredAt)
- [X] T016 Create `ModuPOSDbContext` in `ModuPOS.Database/ModuPOSDbContext.cs` applying configurations from Auth and Organization assemblies, with a global query filter `BusinessId == tenant.BusinessId` on every tenant-owned entity (Branch, StaffBranchAssignment, Role, RolePermission, RoleAssignment, AuthorizationVersion, AuditEntryRecord) driven by `ITenantContext`
- [X] T017 Create EF configurations (case-insensitive collation, enums as strings, `rowversion` concurrency on Role, Details as nvarchar(max) with `ISJSON` check constraint, DependsOn as JSON primitive collection, sequential GUID keys, indexes/uniques above) in `ModuPOS.Database/Configurations/` and the initial migration in `ModuPOS.Database/Migrations/`
- [X] T018 Implement `PermissionCatalog` (code-declared permissions per contracts/permission-catalog.md: `role.view`, `role.manage`→`role.view`, `role.assign`→`role.view`, `audit.view`, `platform.audit.view` Platform scope) in `ModuPOS.Auth/Permissions/PermissionCatalog.cs` and a startup `PermissionCatalogSync` hosted service that inserts new codes, marks removed ones retired, strips retired codes from roles and bumps every `AuthorizationVersion` in `ModuPOS.Auth/Permissions/PermissionCatalogSync.cs`
- [X] T019 Implement `JwtTokenService` (ES256; claims `sub`, `sid`, `pid`, `ptype`, `bid`, `brid`, `av`; 15-min lifetime) in `ModuPOS.Auth/Tokens/JwtTokenService.cs`
- [X] T020 Implement `AuditWriter` in `ModuPOS.Auth/Auditing/AuditWriter.cs`: `WriteCritical` adds the record to the current DbContext (same transaction); `Enqueue` writes to a bounded `Channel<AuditEntry>`; reject Details containing keys `password`, `token`, `refreshToken`, `accessToken` (FR-035)
- [X] T021 Implement `AuditQueueWorker` hosted service in `ModuPOS.Auth/Auditing/AuditQueueWorker.cs`: batch insert, exponential backoff retry, keep original OccurredAt, 1-minute dedup on (user, code, action) incrementing RepeatCount (FR-038), log `Critical` and mark health check degraded if any entry unsaved > 5 min (FR-040)
- [X] T022 Implement `AuthorizationVersionService` (increment per business; read cached 2 s via HybridCache) in `ModuPOS.Auth/Authorization/AuthorizationVersionService.cs`
- [X] T023 Implement `EffectivePermissionService` per data-model formula (union of active business-scope roles and active roles for branch; Owner = all Business-scope non-retired permissions; exclude permissions whose feature is disabled via `IFeatureManager`) cached under `perm:{profileId}:{branchId}:{authVersion}` in `ModuPOS.Auth/Authorization/EffectivePermissionService.cs`
- [X] T024 Implement `RequirePermissionAttribute(code)`, `BranchScopedAttribute`, `RequirePlatformPermissionAttribute(code)` in `ModuPOS.Auth/Authorization/Attributes.cs` and an `AccessPipeline` running steps 1 (token+active session) and 6 (permission) from contracts/authorization-pipeline.md, populating `ITenantContext`, wired as an authorization middleware result handler in `ModuPOS.Auth/Authorization/AccessPipeline.cs` (remaining steps added in US6)
- [X] T025 Add `AddModuPOSAuth(IConfiguration)` in `ModuPOS.Auth/DependencyInjection.cs` and `AddOrganization()` in `ModuPOS.Organization/DependencyInjection.cs`; update `ModuPOS.Api/Program.cs` to register DbContext (`UseSqlServer`), JwtBearer, both modules, problem details, `UseAuthentication()` before `UseAuthorization()`
- [X] T026 Create `ApiFactory` (WebApplicationFactory + SQL Server Testcontainer (`mcr.microsoft.com/mssql/server:2022-latest`), applies migrations) and a `TestData` builder (business, branches, accounts, profiles, roles, assignments) in `tests/ModuPOS.Api.IntegrationTests/Infrastructure/`
- [X] T027 Create development seeder (business "Demo"; branches A, B active and C inactive; Owner account; staff account assigned to A and B; default roles) in `ModuPOS.Api/Infrastructure/DevSeeder.cs`, run only in Development

**Checkpoint**: Database, tokens, audit plumbing, permission resolution and the pipeline skeleton exist.

---

## Phase 3: User Story 1 - Sign in, stay signed in, and sign out (Priority: P1) 🎯 MVP

**Goal**: Sign-in with lockout, rotating renewal, sign-out and sign-out-all.

**Independent Test**: Sign in, call `GET /api/auth/me`, renew, sign out, confirm the old session is refused.

### Tests for User Story 1

- [X] T028 [P] [US1] Unit tests for refresh-token rotation and reuse detection in `tests/ModuPOS.Auth.UnitTests/Sessions/RefreshTokenRotationTests.cs`
- [X] T029 [P] [US1] Unit tests for lockout (5 failures in 15 min → locked 15 min; reset on success) in `tests/ModuPOS.Auth.UnitTests/Accounts/LockoutTests.cs`
- [X] T030 [P] [US1] Integration tests for US1 acceptance scenarios 1–6 and audit codes `AUTH.SIGNIN_SUCCEEDED`, `AUTH.SIGNIN_FAILED`, `AUTH.ACCOUNT_LOCKED`, `AUTH.SIGNOUT`, `AUTH.SIGNOUT_ALL`, `AUTH.RENEWAL_REUSE_DETECTED` in `tests/ModuPOS.Api.IntegrationTests/Auth/SignInTests.cs`

### Implementation for User Story 1

- [X] T031 [P] [US1] Implement `PasswordService` wrapping `PasswordHasher<UserAccount>` with rehash-on-success in `ModuPOS.Auth/Accounts/PasswordService.cs`
- [X] T032 [US1] Implement `SignInService` (lookup by username or email, generic `invalid_credentials`, `account_inactive`, `account_locked` with lockedUntil, lockout counters, create Session + RefreshToken, audit via `Enqueue`) in `ModuPOS.Auth/Sessions/SignInService.cs`
- [X] T033 [US1] Implement `RefreshService` (hash lookup, reject expired/revoked → `refresh_invalid`, used token → revoke session with `RenewalReuse` and audit High → `refresh_reused`, else rotate) in `ModuPOS.Auth/Sessions/RefreshService.cs`
- [X] T034 [US1] Implement `SignOutService` (revoke current session and refresh family, clear ActiveBranchId; sign-out-all returns count) and a cached `SessionStatusCache` used by pipeline step 1 so revocation applies immediately in `ModuPOS.Auth/Sessions/`
- [X] T035 [US1] Implement `AuthController` endpoints `POST /api/auth/sign-in`, `/refresh`, `/sign-out`, `/sign-out-all`, `GET /api/auth/me` per contracts/auth-api.md in `ModuPOS.Api/Controllers/Auth/AuthController.cs`
- [X] T036 [US1] Add fixed-window rate limiter (20/min per IP) on sign-in returning 429 `too_many_requests` in `ModuPOS.Api/Program.cs`

**Checkpoint**: US1 independently testable.

---

## Phase 4: User Story 2 - Portal routing (Priority: P1)

**Goal**: Profile discovery after sign-in and portal selection.

**Independent Test**: Account with Staff + Member profiles gets `choose_portal`; each choice yields a token for that profile only.

### Tests for User Story 2

- [X] T037 [P] [US2] Integration tests for US2 scenarios 1–4 (`portal_selected`, `choose_portal`, `profile_inactive`, `no_access`) in `tests/ModuPOS.Api.IntegrationTests/Auth/PortalTests.cs`

### Implementation for User Story 2

- [X] T038 [US2] Implement `PortalService` (list active profiles with business names, auto-select single profile, validate chosen profile is Active and belongs to the user, update Session.ActiveProfileId, issue new access token) in `ModuPOS.Auth/Portals/PortalService.cs`
- [X] T039 [US2] Return `profiles` and `next` from sign-in in `ModuPOS.Auth/Sessions/SignInService.cs` and add `POST /api/auth/portal` in `ModuPOS.Api/Controllers/Auth/PortalController.cs`

**Checkpoint**: US1 + US2 work together.

---

## Phase 5: User Story 3 - Create and edit custom roles (Priority: P1)

**Goal**: Business admins manage roles over the permission catalog.

**Independent Test**: Owner creates a role with two permissions; a staff member holding it can do exactly those.

### Tests for User Story 3

- [X] T040 [P] [US3] Unit tests for role validation (dependency auto-add, retired/Platform/unavailable rejected, escalation `requested ⊆ actor effective`) in `tests/ModuPOS.Auth.UnitTests/Roles/RoleValidationTests.cs`
- [X] T041 [P] [US3] Integration tests for US3 scenarios 1–6, `role_version_conflict`, and audit `ROLE.CREATED`, `ROLE.UPDATED`, `ROLE.PERMISSIONS_CHANGED`, `SECURITY.ESCALATION_BLOCKED` in `tests/ModuPOS.Api.IntegrationTests/Rbac/RoleTests.cs`

### Implementation for User Story 3

- [X] T042 [US3] Implement `RoleValidator` (name "2–60 chars", description "≤ 500 chars", unique per business case-insensitive → `role_name_taken`, add missing DependsOn and report `addedDependencies`, reject unknown → `permission_unknown`, feature-disabled → `permission_unavailable`, escalation → `escalation_blocked`) in `ModuPOS.Auth/Roles/RoleValidator.cs`
- [X] T043 [US3] Implement `RoleService` create/update (rowversion concurrency → `role_version_conflict`, Owner rename/permission change → `owner_protected`, increment AuthorizationVersion, `WriteCritical` audit in the same transaction, audit failure → 503 `audit_unavailable`) in `ModuPOS.Auth/Roles/RoleService.cs`
- [X] T044 [US3] Implement `DefaultRolesProvisioner` creating Owner (IsOwner, IsSystem), Manager (`role.view`, `role.assign`, `audit.view`) and Cashier (empty) for a new business in `ModuPOS.Auth/Roles/DefaultRolesProvisioner.cs`
- [X] T045 [US3] Implement `PermissionsController` `GET /api/permissions` (with `available`, hiding Platform and retired) and `RolesController` `GET /api/roles`, `GET /api/roles/{id}` (404 for other business), `POST /api/roles`, `PUT /api/roles/{id}` per contracts/rbac-api.md in `ModuPOS.Api/Controllers/Rbac/`

**Checkpoint**: Roles can be created and edited.

---

## Phase 6: User Story 4 - Assign roles at business or branch scope (Priority: P1)

**Goal**: Business- and branch-scoped assignments driving effective permissions.

**Independent Test**: Manager at A, Cashier at B — manager action allowed in A, refused in B.

### Tests for User Story 4

- [X] T046 [P] [US4] Unit tests for effective-permission union across scopes and inactive roles in `tests/ModuPOS.Auth.UnitTests/Authorization/EffectivePermissionTests.cs`
- [X] T047 [P] [US4] Integration tests for US4 scenarios 1–5, change visible ≤ 5 s without sign-out (SC-003), `last_owner`, audit `ROLE_ASSIGNMENT.GRANTED/REVOKED` in `tests/ModuPOS.Api.IntegrationTests/Rbac/RoleAssignmentTests.cs`

### Implementation for User Story 4

- [X] T048 [US4] Implement `RoleAssignmentService` (role and staff in same business, branch must be in StaffBranchAssignment → `branch_not_assigned`, duplicate → `assignment_exists`, escalation check, last active Owner protected with `UPDLOCK, HOLDLOCK` on the business's Owner assignments → `last_owner` + `SECURITY.OWNER_PROTECTION_BLOCKED`, increment AuthorizationVersion, `WriteCritical` audit) in `ModuPOS.Auth/Roles/RoleAssignmentService.cs`
- [X] T049 [US4] Implement `RoleAssignmentsController` `GET/POST /api/staff/{staffProfileId}/role-assignments`, `DELETE .../{assignmentId}` in `ModuPOS.Api/Controllers/Rbac/RoleAssignmentsController.cs`

**Checkpoint**: All P1 stories complete — MVP of dynamic RBAC.

---

## Phase 7: User Story 5 - Branch selection and active branch context (Priority: P2)

**Goal**: Staff choose or auto-get an active branch and can switch.

**Independent Test**: Staff assigned to A, B (active) and C (inactive) sees A and B only, selects, switches.

### Tests for User Story 5

- [X] T050 [P] [US5] Integration tests for US5 scenarios 1–5 and audit `BRANCH.SELECTED` (previous and new branch) in `tests/ModuPOS.Api.IntegrationTests/Auth/BranchContextTests.cs`

### Implementation for User Story 5

- [X] T051 [P] [US5] Implement `IBranchAccessService` (active assigned branches for staff; validate branch same business, assigned, Active; cached, invalidated by AuthorizationVersion) in `ModuPOS.Organization/Services/BranchAccessService.cs`
- [X] T052 [US5] Implement `BranchContextService` (list with `next` = `branch_selected|choose_branch|no_branch_access`, auto-select single branch, select/switch updates Session.ActiveBranchId and issues token with `brid`, `branch_denied` reasons `not_assigned|inactive|other_business`) in `ModuPOS.Auth/Sessions/BranchContextService.cs`
- [X] T053 [US5] Implement `BranchContextController` `GET /api/auth/branches`, `POST /api/auth/branch` in `ModuPOS.Api/Controllers/Auth/BranchContextController.cs`

---

## Phase 8: User Story 6 - Every protected action checked the same way (Priority: P2)

**Goal**: Complete the ordered pipeline with tenant, account, license, feature and branch checks.

**Independent Test**: One action, change one condition at a time; each fails with its own code.

### Tests for User Story 6

- [X] T054 [P] [US6] Integration tests for each pipeline failure code in order (`not_signed_in`, `tenant_invalid`, `account_inactive`, `license_invalid`, `feature_unavailable`, `permission_denied`, `branch_denied`) and cross-tenant attempts returning 404 + `ACCESS.CROSS_TENANT_DENIED` High (SC-004) in `tests/ModuPOS.Api.IntegrationTests/Authorization/PipelineTests.cs`

### Implementation for User Story 6

- [X] T055 [P] [US6] Add `ITenantLicenseStatus` (valid/invalid per business, wraps `ILicenseValidator`, cached 5 min) in `ModuPOS.LicenseManagement/Interfaces/ITenantLicenseStatus.cs` and `ModuPOS.LicenseManagement/Services/TenantLicenseStatus.cs`; register in `ModuPOS.LicenseManagement/DependencyInjection.cs`
- [X] T056 [US6] Add steps 2 (business Active, route tenant = `bid`, cross-tenant audit), 3 (account and profile Active; revoke sessions + `AUTH.SESSIONS_REVOKED` when suspended), 4 (license), 5 (feature via `IFeatureManager`) and 7 (`[BranchScoped]` via `IBranchAccessService`, `ACCESS.BRANCH_DENIED`) to `ModuPOS.Auth/Authorization/AccessPipeline.cs`; add `ACCESS.PERMISSION_DENIED` audit on step 6
- [X] T057 [US6] Implement platform path for `[RequirePlatformPermission]` (steps 1, 3, platform permission; platform profiles never receive business permissions, FR-032) in `ModuPOS.Auth/Authorization/AccessPipeline.cs`
- [X] T058 [US6] Revoke sessions when an account or business is suspended/deactivated (`AUTH.SESSIONS_REVOKED` with cause and count) in `ModuPOS.Auth/Sessions/SessionRevocationService.cs`

---

## Phase 9: User Story 7 - Protect in-use and system roles (Priority: P3)

**Goal**: Safe deactivate/delete; Owner can never be lost.

**Independent Test**: Delete an assigned role → blocked with holders; unassign → deleted; delete Owner → blocked.

### Tests for User Story 7

- [X] T059 [P] [US7] Integration tests for US7 scenarios 1–5 and audit `ROLE.DEACTIVATED`, `ROLE.REACTIVATED`, `ROLE.DELETED`, `SECURITY.OWNER_PROTECTION_BLOCKED`; property-style test that no sequence of role/assignment calls leaves zero active Owners (SC-007) in `tests/ModuPOS.Api.IntegrationTests/Rbac/RoleProtectionTests.cs`

### Implementation for User Story 7

- [X] T060 [US7] Add deactivate/reactivate (returns `affectedStaff`) and delete (`role_in_use` with holders, `owner_protected`) to `ModuPOS.Auth/Roles/RoleService.cs`
- [X] T061 [US7] Add `POST /api/roles/{id}/deactivate`, `/reactivate`, `DELETE /api/roles/{id}` to `ModuPOS.Api/Controllers/Rbac/RolesController.cs`

---

## Phase 10: Polish & Cross-Cutting Concerns (Audit viewing, retention)

- [X] T062 [P] Implement `AuditController` `GET /api/audit` (`audit.view`, filters `from`,`to` ≤ 1 year, `code[]`, `actorUserId`, `branchId`, `severity`, `pageSize` ≤ 200) and `GET /api/platform/businesses/{businessId}/audit` (`platform.audit.view`, `reason` "5–500 chars" else 400 `reason_required`, writes `AUDIT.VIEWED_BY_PLATFORM`) in `ModuPOS.Api/Controllers/Audit/AuditController.cs`
- [X] T063 [P] Implement `AuditRetentionJob` (daily, deletes entries with OccurredAt < now − 1 year in batches of 5,000) in `ModuPOS.Auth/Auditing/AuditRetentionJob.cs`
- [X] T064 Add migration SQL revoking UPDATE/DELETE on the audit table from the app role, granting DELETE only to the retention role, in `ModuPOS.Database/Migrations/`
- [X] T065 [P] Integration tests: audit view/permission (FR-039), FR-040 fail-closed (role edit → 503 when audit insert fails) vs. sign-in continues with queued entry, and SC-005 (after a full run only FR-033 codes exist and FR-037 activities create none) in `tests/ModuPOS.Api.IntegrationTests/Audit/AuditTests.cs`
- [X] T066 [P] Unit tests for audit dedup and credential-field rejection in `tests/ModuPOS.Auth.UnitTests/Auditing/AuditWriterTests.cs`
- [X] T067 [P] Replace `ModuPOS.Api/ModuPOS.Api.http` with the 12 quickstart scenarios
- [X] T068 Run quickstart.md end-to-end and fix discrepancies; add a performance smoke test asserting pipeline p95 ≤ 10 ms on cache hit in `tests/ModuPOS.Api.IntegrationTests/Authorization/PipelinePerformanceTests.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (1) → Foundational (2) → user stories (3–9) → Polish (10)

### User Story Dependencies

- **US1**: after Foundational. No story dependency.
- **US2**: depends on US1 (extends sign-in response).
- **US3**: after Foundational; tests need a signed-in Owner, so uses US1 endpoints (or test token helper).
- **US4**: depends on US3 (needs roles).
- **US5**: after Foundational; branch-scoped role checks in its tests need US4.
- **US6**: after US1; branch step uses US5's `IBranchAccessService`.
- **US7**: depends on US3 and US4.

### Within Each User Story

Tests first (fail) → entities/services → controllers → story checkpoint.

### Parallel Opportunities

- T005–T006; T007–T015 (separate files); all `[P]` test tasks in a story.
- After Foundational: US1 and US3 can start together (different modules/files); US5's T051 can run alongside US3/US4.

## Parallel Example: User Story 1

```text
T028 RefreshTokenRotationTests.cs   T029 LockoutTests.cs   T030 SignInTests.cs   T031 PasswordService.cs
```

## Implementation Strategy

### MVP First

Phases 1–3 (US1): working sign-in/renew/sign-out with audit. Then US2–US4 complete the dynamic RBAC MVP (all P1).

### Incremental Delivery

1. US1 → secure sessions
2. US2 → portals
3. US3 + US4 → dynamic roles (P1 complete)
4. US5 + US6 → branches and full pipeline (P2)
5. US7 → safety rails (P3)
6. Polish → audit viewing, retention, quickstart

### Parallel Team Strategy

After Foundational: dev A on US1→US2→US6, dev B on US3→US4→US7, dev C on US5 then Polish.

## Notes

- Total: 68 tasks. Commit after each task or checkpoint.
- MVP implementation notes (T001–T036):
  - Integration tests use a throwaway LocalDB database per run (`MODUPOS_TEST_SQL` overrides the server) instead of Testcontainers, because Docker is not available on the dev machine.
  - `ModuPOS.Database` references the modules (not the other way round); modules use the EF `DbContext` base type, avoiding a project cycle.
  - JwtBearer is configured inside `ModuPOS.Auth`; the problem-details mapper lives in `ModuPOS.Auth/Http/AuthErrors.cs` so the access pipeline can use it.
  - Sign-in already returns `profiles`/`next` and pre-selects a single profile; `POST /api/auth/portal` (T038–T039) is still open.
  - `ModuPOS.Database/CustomerFeatureRepository.cs` was added so `IFeatureManager` can resolve.
- US2–US4 implementation notes (T037–T049):
  - Effective-permission rules live in a pure `EffectivePermissionCalculator` (unit-tested); the service only loads data.
  - Branch-scope tests set the session's active branch directly and renew, standing in for branch selection (US5).
  - The Owner role can only be assigned at business scope (`owner_business_scope_only`), which keeps the last-Owner rule simple.
  - Custom roles need at least one permission (`permissions_required`); default Cashier starts empty.
  - Feature on/off changes do not bump the authorization version yet; cached permissions then refresh within 5 minutes. Feature management should call `AuthorizationVersionService.IncrementAsync` when it changes a business's features.
- US5–US6 implementation notes (T050–T058):
  - Pipeline steps 2–7 apply to `[RequirePermission]` endpoints; `[RequireSignedIn]`-only endpoints (me, sign-out, portal, branches) check step 1 only, so a user can always sign out.
  - Account/profile/business status and branch access are cached against the authorization version; `SessionRevocationService.SetAccountStatusAsync` / `SetBusinessStatusAsync` change status, end sessions, bump the version and audit `AUTH.SESSIONS_REVOKED`. Admin endpoints that call them come with platform/business management.
  - When the pipeline itself notices a suspended account or business it also ends the caller's sessions.
  - Cross-tenant attempts are detected where an id is "not found" in the caller's business but exists in another (roles, role assignments, staff profiles) and when a `businessId` route value differs from the caller's; the caller always sees 404.
  - Test-only endpoints in `tests/ModuPOS.Api.IntegrationTests/Infrastructure/TestEndpoints.cs` cover branch-scoped, business-in-route and platform actions until real ones exist.
  - Fixed in the audit worker: dedup keys are stored as a SHA-256 hash (fixed length), and a failed batch is retried row by row so one bad row is dropped (logged Critical) instead of blocking the queue.
  - Without a license token every business endpoint now returns `license_invalid`; configure `License` settings (or a dev license) to use the dev seed data.
- US7 + polish notes (T059–T068):
  - Append-only is enforced in the database for every login: an `INSTEAD OF UPDATE` trigger rejects updates, and an `AFTER DELETE` trigger allows deletes only from a connection that set the `modupos.audit_retention` session flag and only for rows older than 365 days. Roles `modupos_app` (UPDATE/DELETE denied) and `modupos_retention` (DELETE granted) are created for deployments with separate logins.
  - FR-040 fail-closed is tested by making critical audit rows violate the JSON check constraint; role changes then return 503 `audit_unavailable` and nothing is saved, while sign-in keeps working.
  - Fixed: seeding a fresh database failed because default roles referenced permissions before the catalog sync ran; the dev seeder now syncs the catalog first.
  - The performance smoke test measures end to end through the in-memory test server (an upper bound on pipeline cost) and passes at p95 ≤ 10 ms on this machine.
- Every tenant query must go through the DbContext filter; never call `IgnoreQueryFilters()` outside platform endpoints and the retention job.
