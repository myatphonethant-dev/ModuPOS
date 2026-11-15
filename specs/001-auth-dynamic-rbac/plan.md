# Implementation Plan: Authentication and Dynamic Roles & Permissions

**Branch**: `001-auth-dynamic-rbac` | **Date**: 2026-11-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-auth-dynamic-rbac/spec.md`

## Summary

Build sign-in, rotating sessions, portal routing, business-defined roles over a system permission catalog, business/branch-scoped role assignment, active-branch selection, a single ordered authorization pipeline (session → tenant → account → license → feature → permission → branch), and a fixed security audit catalog.

Approach: ASP.NET Core on .NET 10 with JWT bearer access tokens (15 min) that carry identity, session and active branch only — **not permissions**. Permissions are resolved per request from a cache keyed by a per-user/per-tenant *authorization version*, so role changes apply on the next request (SC-003). Renewal tokens are opaque, stored hashed, and rotated in families for reuse detection. Audit writes for role/assignment/Owner changes share the database transaction (fail-closed); other audit events go through a background queue with retry (FR-040).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (matches existing projects, `net10.0`)

**Primary Dependencies**: ASP.NET Core 10 (controllers, already in `ModuPOS.Api`), `Microsoft.AspNetCore.Authentication.JwtBearer`, EF Core 10, `Microsoft.Extensions.Identity.Core` (password hasher only), `Microsoft.Extensions.Caching.Hybrid`; existing `ModuPOS.LicenseManagement` and `ModuPOS.FeatureManagement`

**Storage**: SQL Server 2022 via EF Core (`Microsoft.EntityFrameworkCore.SqlServer`); see research R1

**Testing**: xUnit v3, `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory), Testcontainers for SQL Server

**Target Platform**: Linux or Windows server, behind HTTPS

**Project Type**: Web service (REST API) in a modular monolith

**Performance Goals**: Authorization pipeline ≤ 10 ms p95 on cache hit; permission changes visible ≤ 5 s (SC-003); protected actions < 1 s for 95% (SC-006)

**Constraints**: Cross-tenant access impossible by default (global tenant filter); no secrets/credentials in audit; at least one active Owner per business always

**Scale/Scope**: Initial target ~1,000 businesses, ~10 branches and ~50 staff each, ~150 catalog permissions, ~5,000 concurrent sessions

All Technical Context items are resolved (see [research.md](research.md)); no NEEDS CLARIFICATION remain.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled template (no ratified principles), so there are no project gates to evaluate. **Result: PASS (no gates defined).** Recommend running `/speckit-constitution` before later features so plans have real gates.

Self-imposed checks used instead:

| Check | Status |
|-------|--------|
| Modules stay independent (Auth depends on License/Feature via interfaces only) | Pass |
| Every FR maps to a testable contract or scenario | Pass |
| No new project without a reason (see Complexity Tracking) | Pass |

**Post-design re-check**: Pass — design added one module project (`ModuPOS.Organization`) and test projects, both justified below.

## Project Structure

### Documentation (this feature)

```text
specs/001-auth-dynamic-rbac/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── auth-api.md
│   ├── rbac-api.md
│   ├── audit-api.md
│   ├── authorization-pipeline.md
│   └── permission-catalog.md
└── tasks.md             # created by /speckit-tasks
```

### Source Code (repository root)

```text
ModuPOS.Shared/                      # cross-module primitives
├── Tenancy/ITenantContext.cs        # current business, user, branch, session
├── Results/                         # Result / error codes
└── Auditing/IAuditWriter.cs         # audit abstraction other modules may call

ModuPOS.Database/                    # EF Core DbContext, migrations, tenant query filter
├── ModuPOSDbContext.cs
├── Configurations/
└── Migrations/

ModuPOS.Organization/                # NEW: minimal Business, Branch, StaffBranchAssignment (read-mostly here)
├── Entities/
└── Services/IBranchAccessService.cs

ModuPOS.Auth/
├── Accounts/                        # UserAccount, Profile, password hashing, lockout
├── Sessions/                        # Session, RefreshToken rotation, revoke
├── Tokens/                          # JWT issue/validate
├── Portals/                         # profile discovery & routing
├── Permissions/                     # PermissionCatalog (code-defined), dependencies
├── Roles/                           # Role, RolePermission, RoleAssignment, guards (escalation, Owner)
├── Authorization/                   # pipeline handlers, [RequirePermission], effective-permission cache
├── Auditing/                        # AuditEntry, catalog codes, queue, dedup, retention job
└── DependencyInjection.cs

ModuPOS.Api/
├── Controllers/Auth/                # AuthController, PortalController, BranchContextController
├── Controllers/Rbac/                # RolesController, RoleAssignmentsController, PermissionsController
├── Controllers/Audit/               # AuditController
└── Program.cs                       # AddAuth(), UseAuthentication(), pipeline

tests/
├── ModuPOS.Auth.UnitTests/          # rotation, effective permissions, guards, dedup
└── ModuPOS.Api.IntegrationTests/    # end-to-end against SQL Server container
```

**Structure Decision**: Keep the existing modular-monolith layout (one class-library project per module, `ModuPOS.Api` as host). Auth logic lives in `ModuPOS.Auth`; persistence mappings in `ModuPOS.Database`; minimal organization data in a new `ModuPOS.Organization` that the later Business & Branch feature will extend. The placeholder `Class1.cs` files and the WeatherForecast sample are removed.

## Complexity Tracking

| Addition | Why Needed | Simpler Alternative Rejected Because |
|----------|------------|-------------------------------------|
| New `ModuPOS.Organization` project | Roles are tenant- and branch-scoped; tenants, branches and staff-branch links must exist. PRD module diagram already names this module. | Putting Business/Branch inside Auth would have to be moved out when the Business & Branch feature lands. |
| Two test projects | Spec success criteria (SC-004, SC-005, SC-007) need real DB and HTTP pipeline tests. | Unit tests alone cannot prove the ordered pipeline and tenant isolation. |
| Background audit queue | FR-040 requires non-critical actions to continue when audit storage fails. | Writing every audit row synchronously would block sign-in when storage hiccups. |
