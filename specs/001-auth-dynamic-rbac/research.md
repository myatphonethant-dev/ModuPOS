# Research: Authentication and Dynamic Roles & Permissions

All decisions below resolve the Technical Context. Each lists the decision, why, and what else was considered.

## R1. Database

- **Decision**: Microsoft SQL Server 2022 through EF Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`), chosen by the project owner. One shared database; every tenant-owned table has `BusinessId` with a global EF query filter driven by `ITenantContext`.
- **SQL Server specifics**: sequential GUID keys (avoid clustered-index fragmentation); case-insensitive collation `SQL_Latin1_General_CP1_CI_AS` for unique Username/Email/role names; `rowversion` for optimistic concurrency; audit `Details` and `Permission.DependsOn` stored as JSON in `nvarchar(max)` with an `ISJSON` check (EF Core maps primitive collections to JSON automatically); `UPDLOCK, HOLDLOCK` hints for the Owner-invariant lock; `datetime2` in UTC.
- **Rationale**: Fits the team's Windows/Visual Studio tooling and hosting preference; LocalDB or Developer edition is free for development and Testcontainers supports it.
- **Alternatives**: PostgreSQL (original draft; equally capable); database-per-tenant (too heavy for ~1,000 small businesses).

## R2. Password storage

- **Decision**: `PasswordHasher<T>` from `Microsoft.Extensions.Identity.Core` (PBKDF2-HMAC-SHA512, 100k+ iterations, per-hash salt), without adopting full ASP.NET Core Identity.
- **Rationale**: Meets FR-010 with a vetted implementation and built-in rehash-on-upgrade; avoids Identity's user/role tables, which don't fit multi-profile accounts or tenant-owned roles.
- **Alternatives**: Full ASP.NET Core Identity (its role model is global, not per-tenant/per-branch); Argon2 library (stronger, but adds a third-party native dependency for little gain here).

## R3. Access and renewal credentials

- **Decision**: Access token = signed JWT (15 min) with claims `sub` (user), `sid` (session), `pid`/`ptype` (active profile), `bid` (business), `brid` (active branch, optional), `av` (authorization version). Renewal token = 256-bit random opaque string, stored as SHA-256 hash, 7-day lifetime, rotated on every use within a **token family** per session. Presenting a token already marked used revokes the whole session (FR-007).
- **Rationale**: Stateless validation keeps every request fast; the `sid` claim is checked against a cached session status so sign-out/revocation take effect immediately rather than after 15 min.
- **Alternatives**: Opaque access tokens with server lookup on every request (slower, no benefit since session check is cached anyway); cookie sessions (POS clients may be native/tablet apps).
- **Signing**: Asymmetric key (ES256) loaded from configuration/secret store; separate from the license public key.

## R4. Where permissions live

- **Decision**: Permissions are **not** in the JWT. Each request computes effective permissions = union(active business-scope roles, active roles for `brid`) ∩ licensed features, cached in `HybridCache` keyed `perm:{userProfileId}:{branchId}:{authVersion}`. A row `AuthorizationVersion` per business is incremented on any role, role-permission, assignment, account-status or branch-status change; the current version is cached for 2 s.
- **Rationale**: Satisfies FR-026 / SC-003 (≤ 5 s) without re-issuing tokens; cache hit keeps the pipeline under 10 ms.
- **Alternatives**: Permissions in token (stale for up to 15 min — fails SC-003); no cache (DB hit per request).

## R5. Permission catalog

- **Decision**: Permissions are defined in code by each module as constants (e.g. `sale.refund`) with metadata: group, owning `FeatureCode` (reusing `ModuPOS.FeatureManagement.FeatureRegistry.FeatureCodes`), and dependencies. On startup the catalog is synced into a `Permission` table (new codes inserted, retired codes marked retired and removed from roles — spec edge case).
- **Rationale**: Spec says permissions are system-defined; code-first keeps them versioned with the endpoints that check them. See [contracts/permission-catalog.md](contracts/permission-catalog.md).
- **Alternatives**: Admin-editable permission table (contradicts spec).

## R6. Authorization pipeline

- **Decision**: ASP.NET Core authentication (JwtBearer) + a custom `IAuthorizationMiddlewareResultHandler`-backed policy pipeline. Endpoints declare `[RequirePermission("code")]`; branch endpoints add `[BranchScoped]`. A single `AccessPipeline` runs the checks in spec order and returns a typed failure (`not_signed_in`, `tenant_invalid`, `account_inactive`, `license_invalid`, `feature_unavailable`, `permission_denied`, `branch_denied`). See [contracts/authorization-pipeline.md](contracts/authorization-pipeline.md).
- **Rationale**: One ordered place for FR-031 with distinct reasons; easy to test each failure in isolation.
- **Alternatives**: Separate middlewares per check (ordering is fragile); dynamic `IAuthorizationPolicyProvider` only (can't express the ordered stop-on-first-failure semantics cleanly).

## R7. License and feature checks

- **Decision**: Auth depends on a new `ITenantLicenseStatus` abstraction (in `ModuPOS.LicenseManagement`) returning valid/invalid per business, implemented on top of the existing `ILicenseValidator` and cached 5 min. Feature checks call the existing `IFeatureManager.IsEnabledAsync(businessId, featureCode)` (business = customer). Permissions whose feature is disabled never enter the effective set (FR-018).
- **Rationale**: Reuses existing modules; the current validator validates one installation token, so the wrapper isolates that from Auth.
- **Alternatives**: Auth calling `ILicenseValidator` directly (would re-parse the token every request).

## R8. Branch context

- **Decision**: The active branch is stored on the `Session` row. Selecting/switching a branch validates assignment + status, updates the session, and returns a fresh access token with the new `brid`. The pipeline re-validates the branch on every branch-scoped request using cached branch status and assignment.
- **Rationale**: Keeps one source of truth per device; FR-029/FR-030 enforced both at selection and per request (branch deactivated mid-shift edge case).
- **Alternatives**: Client-sent `X-Branch-Id` header per request (easier to tamper with, needs the same checks anyway).

## R9. Escalation and Owner protection

- **Decision**: Role save and assignment services check `requested ⊆ actor's effective permissions` (FR-017) and run Owner invariants inside the same transaction with an `UPDLOCK, HOLDLOCK` lock on the business's Owner assignments (FR-027, SC-007). Roles carry a `rowversion` concurrency token for FR-022.
- **Rationale**: Invariants enforced in the transaction can't be raced by two admins.
- **Alternatives**: Checking in controllers only (race-prone).

## R10. Audit writing and failure behaviour

- **Decision**: Two paths behind one `IAuditWriter`:
  - **Critical** (`ROLE.*`, `ROLE_ASSIGNMENT.*`, `SECURITY.OWNER_PROTECTION_BLOCKED` when part of a change): the audit row is added to the same EF transaction as the change; if saving fails the change fails (FR-040 fail-closed).
  - **Non-critical** (all other catalog events): enqueued to a bounded in-process `Channel<AuditEntry>`; a hosted service batches inserts, retries with backoff, keeps original timestamps, and raises an operations alert (structured `Critical` log + health check degraded) if an entry is still unsaved after 5 min. Dedup (FR-038) collapses identical refusals within 1 minute into one entry with `RepeatCount`.
  - Retention (FR-036): daily hosted job deletes entries older than 1 year in batches. The application DB role has no UPDATE/DELETE on the audit table except the retention job's role.
- **Rationale**: Matches the clarified FR-040 exactly with no extra infrastructure.
- **Alternatives**: Message broker (overkill for a single deployable); synchronous writes for all (blocks sign-in on audit outage).
- **Known limit**: The in-process queue is lost if the process crashes while storage is down. Acceptable for non-critical events at this stage; revisit with an outbox table if multiple instances are deployed.

## R11. Lockout and rate limiting

- **Decision**: Per-account counter (5 failures in a rolling 15 min → locked 15 min) stored on `UserAccount`; additionally ASP.NET Core rate limiter on the sign-in endpoint (per IP, 20/min) to slow credential stuffing.
- **Rationale**: FR-004 plus basic abuse protection; generic error messages per FR-002.

## R12. Testing

- **Decision**: xUnit v3; unit tests for pure logic (rotation, effective-permission union, escalation, Owner invariants, dedup); integration tests with `WebApplicationFactory` + Testcontainers SQL Server for every acceptance scenario and SC-004/005/007.
- **Rationale**: The highest-risk requirements (tenant isolation, pipeline order) need the real HTTP pipeline and database.
