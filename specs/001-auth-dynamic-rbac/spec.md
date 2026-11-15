# Feature Specification: Authentication and Dynamic Roles & Permissions

**Feature Branch**: `001-auth-dynamic-rbac`

**Created**: 2026-11-14

**Status**: Draft

**Input**: User description: "Auth/ Dynamic Role and Permission" (scoped from PRD.md sections 4, 5, 6, 13 and 14)

## Clarifications

### Session 2026-11-14

- Q: Should the system audit all records, or only specific events? → A: Only a defined catalog of security events (FR-033); routine activity and successful actions are excluded (FR-037).
- Q: How long should audit entries be kept before they can be removed? → A: 1 year, then removed automatically by the system (FR-036).
- Q: Who should be able to view a business's audit entries? → A: Anyone in the business with the "view audit log" permission (Owner by default); platform admins can view them with a stated reason, and each such view is audited (FR-039).
- Q: If an audit entry cannot be saved, should the triggering action still go ahead? → A: Refuse role, assignment and Owner changes; let everything else continue with queued retry and an alert (FR-040).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign in, stay signed in, and sign out (Priority: P1)

A person with a Modu POS account (platform admin, business staff, or member) enters their username or email and password. If the credentials are correct and the account is active, they are signed in and taken to the portal that fits them. Their session stays valid while they work and renews quietly without asking for the password again. When they sign out, the session ends everywhere it was being used on that device and cannot be reused.

**Why this priority**: Nothing else in Modu POS can be used without a trusted identity. This is the foundation every other module depends on.

**Independent Test**: Create an active account, sign in, call a protected action, let the short-lived access expire and confirm it renews, sign out, and confirm the old session is refused.

**Acceptance Scenarios**:

1. **Given** an active account, **When** the user submits the correct username/email and password, **Then** they are signed in and receive a session.
2. **Given** any account, **When** the user submits a wrong password or unknown username, **Then** sign-in fails with a single generic message that does not reveal which part was wrong.
3. **Given** a suspended or deactivated account, **When** the user submits correct credentials, **Then** sign-in is rejected with a message that the account is not active.
4. **Given** a signed-in user whose short-lived access has expired, **When** they continue working within the allowed session lifetime, **Then** their access is renewed without re-entering the password.
5. **Given** a signed-in user, **When** they sign out, **Then** their session and renewal credential are revoked, their active branch is cleared, and any later attempt to use them is refused.
6. **Given** a renewal credential that has already been used once, **When** it is presented again, **Then** it is refused and the whole session is revoked as a likely theft.

---

### User Story 2 - Portal routing for people with more than one profile (Priority: P1)

After sign-in, the system looks at which profiles the person holds: platform admin, staff of a business, and/or member of a business's loyalty program. With one profile they go straight to that portal. With several (for example, a staff member who is also a loyalty member) they choose which portal to enter. Each portal re-checks that the chosen profile is active.

**Why this priority**: Routing decides which rules apply to the rest of the session; getting it wrong mixes up admin, staff and customer access.

**Independent Test**: Create one account with both a staff profile and a member profile, sign in, confirm the portal choice appears, and confirm each choice lands in the right portal with only that portal's access.

**Acceptance Scenarios**:

1. **Given** a user with exactly one active profile, **When** they sign in, **Then** they are taken directly to that portal.
2. **Given** a user with staff and member profiles, **When** they sign in, **Then** they are shown a portal choice.
3. **Given** a user who chooses the member portal, **When** their member profile is inactive, **Then** access to that portal is refused.
4. **Given** a user with no active profile, **When** they sign in, **Then** they are told they have no access and no portal is opened.

---

### User Story 3 - Business admin creates and edits custom roles (Priority: P1)

A business owner or anyone holding the role-management permission opens Role Management, creates a role (for example "Senior Cashier"), names and describes it, and ticks the permissions it should have from the list of permissions the system offers. They can later rename the role or change its permissions. Roles belong to one business and are never visible to other businesses.

**Why this priority**: Every business arranges its staff differently. Fixed roles cannot fit them all, so dynamic roles are the core of this feature.

**Independent Test**: As a business owner, create a role with two permissions, assign it to a staff member, and confirm the staff member can do exactly those two things and nothing else.

**Acceptance Scenarios**:

1. **Given** a business admin, **When** they create a role with a unique name and at least one permission, **Then** the role is saved and available for assignment.
2. **Given** a business admin, **When** they try to create a role with a name that already exists in their business, **Then** the save is rejected.
3. **Given** a role that depends on another permission (for example "refund sale" requires "view sales"), **When** the admin selects only the dependent permission, **Then** the system either adds the required permission automatically or blocks the save with an explanation.
4. **Given** a role assigned to staff, **When** the admin removes a permission from it, **Then** every staff member holding that role loses that permission on their next action.
5. **Given** a business, **When** an admin from another business lists roles, **Then** none of this business's roles appear.
6. **Given** a permission that belongs to a feature the business is not licensed for, **When** the admin views the permission list, **Then** that permission is hidden or marked unavailable and cannot grant access.

---

### User Story 4 - Assign roles to staff at business or branch scope (Priority: P1)

A business admin picks a staff member, picks a role, and chooses where it applies: the whole business, or one specific branch. A staff member can hold different roles in different branches (for example, Manager at Branch A, Cashier at Branch B). Changes take effect right away.

**Why this priority**: Multi-branch businesses need staff to have different powers in different locations; this is what makes permissions safe across branches.

**Independent Test**: Give one staff member Manager at Branch A and Cashier at Branch B, then confirm manager-only actions succeed in Branch A and are refused in Branch B.

**Acceptance Scenarios**:

1. **Given** a staff member and a role, **When** the admin assigns the role at business scope, **Then** the staff member holds it in every branch they are assigned to.
2. **Given** a staff member and a role, **When** the admin assigns the role at a specific branch, **Then** it applies only while that branch is the active branch.
3. **Given** a staff member with roles at both scopes, **When** they work in a branch, **Then** their effective permissions are the combination of their business-scope roles and that branch's roles.
4. **Given** a role assignment is removed, **When** the staff member performs their next action, **Then** the removed permissions no longer apply.
5. **Given** an admin, **When** they try to assign a role at a branch the staff member is not assigned to, **Then** the assignment is rejected.

---

### User Story 5 - Staff branch selection and active branch context (Priority: P2)

When a staff member enters the staff portal, the system loads the branches they are assigned to. If they have none, access is refused. If they have one, it is chosen automatically. If they have several, they pick one. The chosen branch becomes their active branch, and their effective permissions are worked out for it. They can switch branches later without signing in again.

**Why this priority**: Needed for branch-scoped roles to mean anything, but a single-branch business can run with the automatic choice, so it ranks after the core role work.

**Independent Test**: Assign a staff member to two active branches and one inactive branch; confirm they see only the two active ones, can pick one, and can switch to the other.

**Acceptance Scenarios**:

1. **Given** a staff member with no active branch assignments, **When** they enter the staff portal, **Then** access is refused with a "no branch access" message.
2. **Given** a staff member with exactly one active branch, **When** they enter the staff portal, **Then** that branch is selected automatically.
3. **Given** a staff member with multiple active branches, **When** they enter the staff portal, **Then** they choose from a list of only those branches.
4. **Given** a staff member, **When** they request a branch they are not assigned to, or one that is inactive, **Then** the request is refused.
5. **Given** a staff member working in Branch A, **When** they switch to Branch B, **Then** their permissions are recalculated for Branch B.

---

### User Story 6 - Every protected action is checked the same way (Priority: P2)

Whenever anyone performs a protected action, the system checks, in order: they are signed in, their business (tenant) is valid, their account is active, the business license is valid, the feature behind the action is enabled, they hold the required permission, and (for branch actions) the active branch belongs to their business, is assigned to them, and is active. The first failed check stops the action with a clear reason. Refusals for missing permission, another business's data, or an invalid branch are recorded for audit (see FR-033).

**Why this priority**: This is what turns roles into real protection. It builds on the existing license and feature modules.

**Independent Test**: Use one staff account to call one protected action while changing one condition at a time (expired license, disabled feature, missing permission, wrong branch) and confirm each is refused with its own reason.

**Acceptance Scenarios**:

1. **Given** no valid session, **When** a protected action is called, **Then** it is refused as "not signed in".
2. **Given** a valid session but an expired or suspended license, **When** a protected action is called, **Then** it is refused as "license not valid".
3. **Given** a valid license but the feature is not enabled for the business, **When** an action of that feature is called, **Then** it is refused as "feature unavailable".
4. **Given** everything valid except the permission, **When** the action is called, **Then** it is refused as "permission denied".
5. **Given** a branch action, **When** the active branch belongs to another business or is not assigned to the user, **Then** it is refused.
6. **Given** a user from Business A, **When** they try to reach any data of Business B, **Then** it is refused and recorded.

---

### User Story 7 - Protect roles that are in use and system roles (Priority: P3)

Admins can deactivate or delete roles they no longer need, but the system stops them from breaking access by accident. A role still assigned to staff cannot be deleted until those staff are reassigned. Default system roles created at business setup, especially Owner, cannot be deleted or stripped of their permissions, so a business can never lock itself out.

**Why this priority**: Important safety rail, but it only matters once roles are in daily use.

**Independent Test**: Try to delete a role that has assigned staff and confirm it is blocked; reassign the staff and confirm deletion succeeds; try to delete Owner and confirm it is blocked.

**Acceptance Scenarios**:

1. **Given** a role assigned to one or more staff, **When** the admin tries to delete it, **Then** deletion is blocked and the admin is shown who holds it.
2. **Given** a role with no assignments, **When** the admin deletes it, **Then** it is removed.
3. **Given** an active role, **When** the admin deactivates it, **Then** it stops granting permissions to everyone holding it but keeps its assignments for later reactivation.
4. **Given** the Owner role, **When** anyone tries to delete it, rename it, or remove its permissions, **Then** the change is refused.
5. **Given** a business with exactly one active Owner, **When** someone tries to remove that person's Owner role, **Then** the change is refused.

---

### Edge Cases

- Repeated wrong passwords: after 5 failed attempts in 15 minutes, the account is temporarily locked for 15 minutes and the attempt is recorded.
- A user's role or branch assignment changes while they are signed in: the change applies on their next action; they are not signed out.
- A user's account or business is suspended while they are signed in: their next action is refused and their sessions are revoked.
- The active branch is deactivated while staff are working in it: their next branch action is refused and they are sent back to branch selection.
- A staff member is removed from their only branch while signed in: next action refused with "no branch access".
- An admin tries to grant a permission they do not hold themselves: refused, so nobody can raise their own or others' access above their own.
- An admin edits their own role and removes role-management permission: allowed only if another active user can still manage roles (Owner always can).
- Two admins edit the same role at the same time: the second save is rejected and asked to reload, so no change is silently lost.
- A permission is retired from the system catalog: it is removed from all roles and no longer grants access.
- Audit storage is unavailable: role and assignment changes are refused until it recovers; sign-in, POS work and branch switching carry on and their entries are saved once storage returns (FR-040).
- Sign-in from many devices: each device has its own session; signing out on one does not end the others; an option to sign out of all devices exists.

## Requirements *(mandatory)*

### Functional Requirements

**Authentication and sessions**

- **FR-001**: System MUST let users sign in with username or email plus password.
- **FR-002**: System MUST reject sign-in with a generic message when credentials are wrong, without revealing whether the username exists.
- **FR-003**: System MUST reject sign-in for suspended, deactivated, or locked accounts.
- **FR-004**: System MUST temporarily lock an account for 15 minutes after 5 failed sign-in attempts within 15 minutes.
- **FR-005**: System MUST issue a short-lived access credential (15 minutes) and a longer-lived renewal credential (7 days) per sign-in.
- **FR-006**: System MUST allow renewal of access with the renewal credential, issuing a new renewal credential each time and invalidating the old one.
- **FR-007**: System MUST revoke the whole session when an already-used renewal credential is presented again.
- **FR-008**: System MUST let users sign out, revoking the session and renewal credential and clearing the active branch.
- **FR-009**: System MUST let users sign out of all their sessions at once.
- **FR-010**: System MUST store passwords only in a non-reversible, salted form.

**Portal routing**

- **FR-011**: System MUST determine each signed-in user's active profiles (platform admin, staff, member) and route them directly when there is one, or offer a choice when there are several.
- **FR-012**: System MUST re-check the chosen profile is active before opening its portal.

**Permission catalog and dynamic roles**

- **FR-013**: System MUST provide a system-defined catalog of permissions, each with a code, a name, a group (module), the feature it belongs to, and any permissions it depends on.
- **FR-014**: Business admins MUST be able to create, rename, describe, change permissions of, deactivate, reactivate and delete roles within their own business.
- **FR-015**: System MUST require role names to be unique within a business.
- **FR-016**: System MUST enforce permission dependencies when a role is saved.
- **FR-017**: System MUST NOT allow a user to grant, through a role or assignment, any permission they do not hold themselves.
- **FR-018**: System MUST hide or disable permissions for features the business is not licensed for, and such permissions MUST never grant access.
- **FR-019**: System MUST create default roles (Owner, Manager, Cashier) when a business is set up; Owner MUST hold all permissions and cannot be deleted, renamed or reduced.
- **FR-020**: System MUST block deletion of a role while it has assignments and show which staff hold it.
- **FR-021**: System MUST keep every business's roles and assignments invisible and inaccessible to other businesses.
- **FR-022**: System MUST reject a role save if the role changed since the editor loaded it.

**Role assignment and effective permissions**

- **FR-023**: Business admins MUST be able to assign and remove roles for a staff member at business scope or at a specific branch.
- **FR-024**: System MUST reject branch-scope assignments for branches the staff member is not assigned to.
- **FR-025**: System MUST compute effective permissions as the union of the user's active business-scope roles and active roles for the current active branch.
- **FR-026**: Changes to roles, permissions, assignments, account status or branch status MUST take effect on the user's next action without requiring sign-out.
- **FR-027**: System MUST always keep at least one active Owner per business.

**Branch context**

- **FR-028**: System MUST load a staff member's active branch assignments on entering the staff portal: refuse when none, auto-select when one, offer a choice when several.
- **FR-029**: System MUST let staff switch active branch without signing in again, recalculating permissions.
- **FR-030**: System MUST refuse an active branch that is inactive, belongs to another business, or is not assigned to the user.

**Authorization checks**

- **FR-031**: System MUST check every protected action in this order: signed in → tenant valid → account active → license valid → feature enabled → permission held → (branch actions) branch valid for the user. It MUST stop at the first failure and return a distinct reason for each.
- **FR-032**: Platform admin permissions MUST be separate from business permissions; a platform admin MUST NOT gain business staff permissions without a staff profile in that business.

**Audit**

- **FR-033**: System MUST record exactly the audit events in the catalog below, and only those. Routine activity is not audited by this feature.

  | Code | When it is recorded | Extra details kept |
  |------|---------------------|--------------------|
  | `AUTH.SIGNIN_SUCCEEDED` | A user signs in successfully | Portal entered |
  | `AUTH.SIGNIN_FAILED` | Sign-in is rejected | Username/email typed, reason (wrong credentials, account inactive, account locked) |
  | `AUTH.ACCOUNT_LOCKED` | An account is locked after repeated failures | Failed-attempt count, lock end time |
  | `AUTH.SIGNOUT` | A user signs out of one session | — |
  | `AUTH.SIGNOUT_ALL` | A user signs out of all sessions | Number of sessions ended |
  | `AUTH.RENEWAL_REUSE_DETECTED` | A used renewal credential is presented again and the session is revoked | Session revoked |
  | `AUTH.SESSIONS_REVOKED` | Sessions are ended because the account or business was suspended or deactivated | Cause, number of sessions ended |
  | `BRANCH.SELECTED` | A staff member sets or switches their active branch | Previous branch, new branch |
  | `ROLE.CREATED` | A role is created | Name, permissions |
  | `ROLE.UPDATED` | A role's name or description changes | Before and after values |
  | `ROLE.PERMISSIONS_CHANGED` | Permissions are added to or removed from a role | Permissions added, permissions removed, number of staff affected |
  | `ROLE.DEACTIVATED` / `ROLE.REACTIVATED` | A role is turned off or back on | Number of staff affected |
  | `ROLE.DELETED` | A role is deleted | Name and permissions at time of deletion |
  | `ROLE_ASSIGNMENT.GRANTED` | A role is given to a staff member | Staff member, role, scope (business or branch) |
  | `ROLE_ASSIGNMENT.REVOKED` | A role is taken from a staff member | Staff member, role, scope |
  | `ACCESS.PERMISSION_DENIED` | A signed-in user is refused for lacking the required permission | Permission required, action attempted |
  | `ACCESS.CROSS_TENANT_DENIED` | A user tries to reach another business's data (marked high severity) | Business targeted, action attempted |
  | `ACCESS.BRANCH_DENIED` | A branch action is refused because the branch is inactive, not assigned, or not in the user's business | Branch, reason |
  | `SECURITY.ESCALATION_BLOCKED` | Someone tries to grant a permission they do not hold | Permissions attempted |
  | `SECURITY.OWNER_PROTECTION_BLOCKED` | Someone tries to delete, rename or reduce the Owner role, or remove the last active Owner | Change attempted |
  | `AUDIT.VIEWED_BY_PLATFORM` | A platform admin views a business's audit entries | Business viewed, filters used, reason given |

- **FR-034**: Every audit entry MUST contain: event code, time (UTC), outcome, acting user and profile, business, active branch (if any), target (type and identifier), session, source address and device description, plus the event's extra details above.
- **FR-035**: Audit entries MUST NOT contain passwords, renewal or access credentials, or any part of them.
- **FR-036**: Audit entries MUST be append-only: no user, including Owner and platform admins, can edit or delete them. Entries MUST be kept for 1 year; after that the system removes them automatically.
- **FR-037**: These are explicitly NOT audited by this feature: successful access renewals, portal selection, successful protected actions, read-only views, refusals for "not signed in", and refusals for "license not valid" or "feature unavailable" (those are business state, not security events). Business modules (sales, inventory, etc.) define their own audit events.
- **FR-038**: Repeated identical refusals (same user, same code, same action) within 1 minute MUST be recorded as one entry with a count, so a looping client cannot flood the audit record.
- **FR-039**: Viewing a business's audit entries MUST require the "view audit log" permission, which Owner holds by default and admins can add to other roles. Platform admins MAY view a business's audit entries only after entering a reason, and each viewing MUST be recorded as `AUDIT.VIEWED_BY_PLATFORM`. Views inside the business itself are not audited.
- **FR-040**: If an audit entry cannot be saved: role, permission, role-assignment and Owner changes MUST be refused (nothing changes) with a "try again" message; all other actions (sign-in, sign-out, branch selection, access refusals) MUST continue, with the entry queued and retried until saved, and an operations alert raised if it is still unsaved after 5 minutes. Queued entries keep their original time.

### Key Entities

- **User Account**: A person's sign-in identity: username, email, password (protected), status (active, suspended, deactivated, locked), failed-attempt count. One account can hold several profiles.
- **Profile**: The person's role in the system: platform admin, staff of a business, or member of a business. Each has its own status.
- **Session**: One signed-in device: owner, start time, last activity, renewal credential (rotating), status (active, revoked, expired), active portal and active branch.
- **Business (Tenant)**: The customer organization that owns roles, staff and branches. Provided by the organization module.
- **Branch**: A location of a business with a status. Provided by the organization module.
- **Staff Branch Assignment**: Links a staff profile to a branch they may work in.
- **Permission**: A system-defined right to perform one kind of action; has a code, group, owning feature and dependencies.
- **Role**: A named, business-owned bundle of permissions; flags for system/default and active.
- **Role Assignment**: Gives a staff profile a role at business scope or at one branch.
- **Effective Permissions**: The calculated set of permissions a staff member holds in their current active branch.
- **Audit Entry**: One append-only security event from the FR-033 catalog: event code, severity, time, outcome, actor (user and profile), business, branch, target, session, source address, device description, event details, and a repeat count.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user with one profile goes from opening the app to their dashboard in under 15 seconds; a staff member with several branches in under 25 seconds.
- **SC-002**: A business admin can create a new role and assign it to a staff member in under 2 minutes without help.
- **SC-003**: Changes to roles or assignments take effect for affected users within 5 seconds, with no sign-out required.
- **SC-004**: In security testing, 100% of attempts to reach another business's data, use a revoked session, reuse a renewal credential, or act without the required permission are refused.
- **SC-005**: Every event in the FR-033 catalog is recorded each time it happens (100%), and none of the activities excluded by FR-037 creates an audit entry.
- **SC-006**: Access checks add no noticeable delay: 95% of protected actions complete for the user in under 1 second.
- **SC-007**: No business can reach a state with zero active Owners, verified across all role and assignment change paths.

## Assumptions

- Accounts are created by platform admins (business owners), by business admins (staff), or through member sign-up handled by the loyalty feature; self-registration and invitation flows are covered by the onboarding feature, not this one.
- Password reset, two-factor sign-in and single sign-on with outside providers are out of scope for this version.
- Businesses, branches and staff-to-branch assignment records are managed by the business/branch management feature; this feature reads them and only enforces them.
- The existing license validation and feature management modules decide license validity and enabled features; this feature calls them as part of the authorization checks.
- Permissions are defined by the system (developers add them as features are built); businesses build roles from them but cannot invent new permissions.
- Member portal access is routed and checked here, but member-only screens and their permissions belong to the loyalty feature.
- Session lifetimes (15 minutes access, 7 days renewal) and lockout values (5 attempts / 15 minutes) are standard defaults and may be made configurable later.
- Platform admin roles use a separate, fixed set of platform permissions; dynamic roles in this feature apply to business staff.
