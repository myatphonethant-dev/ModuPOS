# Contract: Roles, Permissions and Assignments API

All endpoints are tenant-scoped to the caller's active business. Required permission shown per endpoint.

## GET /api/permissions — `role.view`
200 list of `{ code, name, group, featureCode, dependsOn[], available }`. `available=false` when the feature is not enabled for the business (FR-018). Platform-scope and retired permissions are never returned.

## GET /api/roles — `role.view`
200 `[{ id, name, description, isSystem, isOwner, isActive, permissionCount, assignedStaffCount, version }]`

## GET /api/roles/{id} — `role.view`
200 role with `permissions[]` and `version`; 404 if not in caller's business (never 403, to avoid revealing existence).

## POST /api/roles — `role.manage`
Request `{ name, description?, permissions: [codes] }`
- 201 role; response includes `addedDependencies[]` if dependencies were auto-added.
- 409 `role_name_taken` · 422 `permission_unknown | permission_unavailable` · 403 `escalation_blocked` (audited `SECURITY.ESCALATION_BLOCKED`).

## PUT /api/roles/{id} — `role.manage`
Request `{ name, description?, permissions: [codes], version }`
- 200 · 409 `role_version_conflict` (FR-022) · 403 `owner_protected` (audited) · same validation errors as create.

## POST /api/roles/{id}/deactivate · /reactivate — `role.manage`
200 `{ affectedStaff: n }` · 403 `owner_protected`.

## DELETE /api/roles/{id} — `role.manage`
204 · 409 `role_in_use` with `holders: [{ staffProfileId, name, branchId? }]` (FR-020) · 403 `owner_protected`.

## GET /api/staff/{staffProfileId}/role-assignments — `role.assign`
200 `[{ id, roleId, roleName, branchId?, branchName? }]`

## POST /api/staff/{staffProfileId}/role-assignments — `role.assign`
Request `{ roleId, branchId? }` (null = business scope)
- 201 · 409 `assignment_exists` · 422 `branch_not_assigned` (FR-024) · 403 `escalation_blocked`.

## DELETE /api/staff/{staffProfileId}/role-assignments/{assignmentId} — `role.assign`
204 · 409 `last_owner` (FR-027, audited `SECURITY.OWNER_PROTECTION_BLOCKED`).

Every successful write increments the business `AuthorizationVersion` and writes its audit entry in the same transaction; if the audit write fails → 503 `audit_unavailable` and nothing changes (FR-040).
