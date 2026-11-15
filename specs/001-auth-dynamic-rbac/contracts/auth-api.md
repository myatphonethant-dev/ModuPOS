# Contract: Authentication, Portal and Branch Context API

Base path `/api/auth`. JSON. Errors use RFC 9457 problem details with a stable `code`.

## POST /api/auth/sign-in
Request: `{ "login": "user or email", "password": "..." }`

| Status | Body / code | When |
|--------|-------------|------|
| 200 | `SignInResponse` | success |
| 401 | `invalid_credentials` | wrong login or password (same message for both, FR-002) |
| 403 | `account_inactive` | Suspended/Deactivated |
| 423 | `account_locked` + `lockedUntil` | lockout (FR-004) |
| 429 | `too_many_requests` | IP rate limit |

`SignInResponse`:
```json
{
  "accessToken": "jwt", "accessTokenExpiresAt": "...",
  "refreshToken": "opaque", "refreshTokenExpiresAt": "...",
  "profiles": [ { "profileId": "...", "type": "Staff", "businessId": "...", "businessName": "..." } ],
  "next": "portal_selected | choose_portal | no_access"
}
```
When exactly one active profile exists it is pre-selected (`next = portal_selected`).

## POST /api/auth/refresh
Request `{ "refreshToken": "..." }` → 200 new pair (old token marked used) · 401 `refresh_invalid` · 401 `refresh_reused` (session revoked, audited).

## POST /api/auth/sign-out  *(signed in)*
Revokes current session and its refresh family, clears active branch → 204.

## POST /api/auth/sign-out-all  *(signed in)*
Revokes every session of the user → 200 `{ "sessionsEnded": n }`.

## POST /api/auth/portal  *(signed in)*
Request `{ "profileId": "..." }` → 200 new access token with `pid`/`bid` · 403 `profile_inactive` · 404 `profile_not_found`.

## GET /api/auth/branches  *(staff portal)*
200 `{ "branches": [ { "branchId", "name" } ], "next": "branch_selected | choose_branch | no_branch_access" }` — only Active branches assigned to the staff member; one branch → auto-selected.

## POST /api/auth/branch  *(staff portal)*
Request `{ "branchId": "..." }` → 200 new access token with `brid` · 403 `branch_denied` (`reason`: `not_assigned | inactive | other_business`).

## GET /api/auth/me  *(signed in)*
200 `{ user, activeProfile, activeBranch, permissions: ["code", ...] }` — effective permissions for UI gating.
