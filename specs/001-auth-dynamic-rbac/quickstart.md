# Quickstart: Validate Authentication and Dynamic Roles & Permissions

## Prerequisites

- .NET 10 SDK
- Docker (for SQL Server and Testcontainers), or a local SQL Server / LocalDB instance for running the API
- Dev JWT signing key and a dev license token in `ModuPOS.Api/appsettings.Development.json` (or user secrets)

## Setup

```bash
docker run -d --name modupos-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=Dev_Passw0rd! -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

```bash
dotnet ef database update --project ModuPOS.Database --startup-project ModuPOS.Api
```

```bash
dotnet run --project ModuPOS.Api
```

Development seeding creates one business ("Demo") with branches A, B (active) and C (inactive), an Owner account, and a staff account assigned to A and B.

## Automated checks

```bash
dotnet test
```

Integration tests cover every acceptance scenario in [spec.md](spec.md). They must pass before the feature is done.

## Manual scenarios (via `ModuPOS.Api/ModuPOS.Api.http`)

| # | Steps | Expected |
|---|-------|----------|
| 1 | Sign in as Owner ([auth-api](contracts/auth-api.md)) | 200, `next = portal_selected` |
| 2 | Sign in with wrong password 5× | 5th → 423 `account_locked`; audit has `AUTH.SIGNIN_FAILED` ×5 (or deduped) and `AUTH.ACCOUNT_LOCKED` |
| 3 | Refresh twice with the same refresh token | 2nd → 401 `refresh_reused`; old access token now 401 |
| 4 | Owner creates role "Senior Cashier" with `role.manage` only | 201 with `addedDependencies: ["role.view"]` |
| 5 | Assign it to staff at branch A; staff selects A, then B, and calls `GET /api/roles` | A: 200, B: 403 `permission_denied` |
| 6 | Staff calls `POST /api/auth/branch` with branch C | 403 `branch_denied`, `reason = inactive` |
| 7 | Owner removes the permission; staff retries within 5 s | 403 `permission_denied` without signing out (SC-003) |
| 8 | Delete a role that has assignments | 409 `role_in_use` with holders |
| 9 | Try to remove the only Owner assignment | 409 `last_owner`; `SECURITY.OWNER_PROTECTION_BLOCKED` audited |
| 10 | Staff of business 1 requests a role id of business 2 | 404; `ACCESS.CROSS_TENANT_DENIED` audited (High) |
| 11 | Stop the DB briefly; sign in (with cached data) vs. edit a role | Role edit → 503 `audit_unavailable`, unchanged; queued entries appear after DB returns |
| 12 | `GET /api/audit` as Owner; platform call without `reason` | 200; 400 `reason_required` |

## Done when

All automated tests pass, the 12 scenarios behave as expected, and the audit table contains only FR-033 codes after the run (SC-005).
