# Contract: Authorization Pipeline

Applies to every endpoint with `[RequirePermission(code)]`; `[BranchScoped]` adds step 7. Stops at the first failure.

| # | Check | Failure status / code | Audited? |
|---|-------|-----------------------|----------|
| 1 | Valid access token and Active session (`sid`) | 401 `not_signed_in` | No (FR-037) |
| 2 | Business exists and Active; route/body tenant = token `bid` | 403 `tenant_invalid`; cross-tenant id → 404 to caller | `ACCESS.CROSS_TENANT_DENIED` (High) |
| 3 | Account and active profile Active | 403 `account_inactive` (sessions revoked if suspended) | `AUTH.SESSIONS_REVOKED` when revoking |
| 4 | Business license valid (`ITenantLicenseStatus`) | 403 `license_invalid` | No |
| 5 | Permission's feature enabled (`IFeatureManager`) | 403 `feature_unavailable` | No |
| 6 | Permission ∈ effective permissions | 403 `permission_denied` | `ACCESS.PERMISSION_DENIED` (deduped) |
| 7 | Active branch: same business, assigned to staff, Active | 403 `branch_denied` + `reason` | `ACCESS.BRANCH_DENIED` |

On success, `ITenantContext` exposes `UserId, ProfileId, BusinessId, BranchId?, SessionId, Permissions` to handlers and to EF query filters.

Endpoint declaration (illustrative):
```text
[RequirePermission(Permissions.Sale.Refund)] [BranchScoped]
POST /api/sales/{id}/refund
```
Platform endpoints use `[RequirePlatformPermission(code)]`: steps 1, 3, then platform permission check; steps 2, 4, 5, 7 do not apply.
