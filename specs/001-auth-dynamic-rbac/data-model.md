# Data Model: Authentication and Dynamic Roles & Permissions

Conventions (SQL Server): all ids are `uniqueidentifier`, generated with EF Core's sequential GUID generator to avoid index fragmentation; times are UTC `datetime2`; text is `nvarchar` with the database's case-insensitive collation (`SQL_Latin1_General_CP1_CI_AS`). **T** = tenant-owned (has `BusinessId`, filtered by global query filter).

## Organization (minimal, owned by `ModuPOS.Organization`)

### Business
| Field | Type | Rules |
|-------|------|-------|
| Id | uniqueidentifier | PK; equals FeatureManagement `customerId` |
| Name | text | required |
| Status | enum `Active, Suspended, Deactivated` | non-Active → tenant check fails, sessions revoked (`AUTH.SESSIONS_REVOKED`) |

### Branch (T)
| Field | Type | Rules |
|-------|------|-------|
| Id, BusinessId | uniqueidentifier | FK Business |
| Name | text | unique per business |
| Status | enum `Active, Inactive` | Inactive → `branch_denied` |

### StaffBranchAssignment (T)
| Field | Type | Rules |
|-------|------|-------|
| StaffProfileId, BranchId | uniqueidentifier | composite PK; both same business |
| AssignedAt | datetime2 | |

## Identity (`ModuPOS.Auth`)

### UserAccount
| Field | Type | Rules |
|-------|------|-------|
| Id | uniqueidentifier | PK |
| Username | nvarchar(50) CI | unique, 3–50 chars |
| Email | nvarchar(256) CI | unique, valid email |
| PasswordHash | text | PasswordHasher output; never logged/audited |
| Status | enum `Active, Suspended, Deactivated` | |
| FailedSignInCount | int | reset on success |
| FailureWindowStart | datetime2? | rolling 15 min |
| LockedUntil | datetime2? | set when count reaches 5 |
| CreatedAt | datetime2 | |

Status transitions: `Active ⇄ Suspended → Deactivated`. Lock is separate from status (time-based).

### Profile
| Field | Type | Rules |
|-------|------|-------|
| Id | uniqueidentifier | PK |
| UserAccountId | uniqueidentifier | FK |
| Type | enum `PlatformAdmin, Staff, Member` | |
| BusinessId | uniqueidentifier? | required for Staff/Member, null for PlatformAdmin |
| Status | enum `Active, Inactive` | |

Unique: (UserAccountId, Type, BusinessId).

### Session
| Field | Type | Rules |
|-------|------|-------|
| Id | uniqueidentifier | PK; JWT `sid` |
| UserAccountId | uniqueidentifier | FK |
| ActiveProfileId | uniqueidentifier? | set by portal selection |
| ActiveBranchId | uniqueidentifier? | Staff only; cleared on sign-out |
| Status | enum `Active, Revoked, Expired` | |
| RevokedReason | enum? `SignOut, SignOutAll, RenewalReuse, AccountSuspended, BusinessSuspended` | |
| CreatedAt, LastSeenAt, ExpiresAt | datetime2 | ExpiresAt = last renewal + 7 days |
| SourceAddress, DeviceDescription | text | for audit |

### RefreshToken
| Field | Type | Rules |
|-------|------|-------|
| Id | uniqueidentifier | PK |
| SessionId | uniqueidentifier | FK; one family per session |
| TokenHash | varbinary(32) | SHA-256, unique |
| IssuedAt, ExpiresAt | datetime2 | |
| UsedAt | datetime2? | set on rotation; reuse of a used token → revoke session |
| ReplacedById | uniqueidentifier? | next token in family |

## RBAC (`ModuPOS.Auth`)

### Permission (system catalog, not tenant-owned)
| Field | Type | Rules |
|-------|------|-------|
| Code | text | PK, `module.action` lowercase, e.g. `role.manage` |
| Name, Group | text | |
| FeatureCode | text | must be a known `FeatureCodes` value |
| DependsOn | nvarchar(max) (JSON array) | codes that must also be granted |
| Scope | enum `Business, Platform` | Platform permissions never appear in business roles (FR-032) |
| IsRetired | bool | retired → removed from all roles, never granted |

### Role (T)
| Field | Type | Rules |
|-------|------|-------|
| Id, BusinessId | uniqueidentifier | |
| Name | text | 2–60 chars, unique per business (case-insensitive) |
| Description | text? | ≤ 500 chars |
| IsSystem | bool | true for Owner/Manager/Cashier defaults |
| IsOwner | bool | exactly one per business; immutable name/permissions; not deletable |
| IsActive | bool | inactive → grants nothing, assignments kept |
| Version | rowversion | concurrency token (FR-022) |

### RolePermission (T)
| RoleId, PermissionCode | composite PK |

Validation on save: all `DependsOn` present (auto-added and reported), no retired or Platform-scope codes, every code ⊆ actor's effective permissions (FR-017).

### RoleAssignment (T)
| Field | Type | Rules |
|-------|------|-------|
| Id | uniqueidentifier | |
| StaffProfileId | uniqueidentifier | Staff profile of same business |
| RoleId | uniqueidentifier | same business |
| BranchId | uniqueidentifier? | null = business scope; else must be in StaffBranchAssignment (FR-024) |
| AssignedAt, AssignedByProfileId | | |

Unique: (StaffProfileId, RoleId, BranchId). Invariant: ≥ 1 active Owner assignment (business scope) to an Active staff profile per business (FR-027).

### AuthorizationVersion (T)
| BusinessId | PK | 
| Value | bigint | incremented on any change affecting effective permissions in the business |

### Effective permissions (computed, not stored)
`EP(profile, branch) = ⋃ perms(r) for active r in assignments where BranchId is null or = branch` minus permissions whose FeatureCode is disabled for the business. Owner = all Business-scope, non-retired, feature-enabled permissions.

## Audit (`ModuPOS.Auth`)

### AuditEntry (T, BusinessId nullable for platform/pre-tenant events)
| Field | Type | Rules |
|-------|------|-------|
| Id | uniqueidentifier | |
| Code | text | one of the FR-033 catalog codes |
| Severity | enum `Info, Warning, High` | High for `ACCESS.CROSS_TENANT_DENIED`, `AUTH.RENEWAL_REUSE_DETECTED` |
| OccurredAt | datetime2 | original event time even if written late |
| Outcome | enum `Succeeded, Denied, Blocked` | |
| ActorUserId, ActorProfileId | uniqueidentifier? | null for unknown-user sign-in failure |
| BusinessId, BranchId, SessionId | uniqueidentifier? | |
| TargetType, TargetId | text? | |
| SourceAddress, DeviceDescription | text? | |
| Details | nvarchar(max) JSON (`ISJSON` check) | event-specific fields from FR-033; validated to contain no credential fields |
| RepeatCount | int | ≥ 1 (FR-038) |
| DedupKey | text? | hash(user, code, action) for 1-min collapse |

Indexes: (BusinessId, OccurredAt desc), (Code, OccurredAt), (ActorUserId, OccurredAt). Append-only; retention deletes rows with OccurredAt < now − 1 year.

## Relationships

```text
UserAccount 1─* Profile *─1 Business 1─* Branch
UserAccount 1─* Session 1─* RefreshToken
Profile(Staff) *─* Branch        via StaffBranchAssignment
Profile(Staff) 1─* RoleAssignment *─1 Role *─* Permission (via RolePermission)
RoleAssignment *─0..1 Branch
```
