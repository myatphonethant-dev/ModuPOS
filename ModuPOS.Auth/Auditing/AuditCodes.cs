namespace ModuPOS.Auth.Auditing;

/// <summary>The complete FR-033 audit catalog. Nothing else is audited by this module.</summary>
public static class AuditCodes
{
    public const string SignInSucceeded = "AUTH.SIGNIN_SUCCEEDED";
    public const string SignInFailed = "AUTH.SIGNIN_FAILED";
    public const string AccountLocked = "AUTH.ACCOUNT_LOCKED";
    public const string SignOut = "AUTH.SIGNOUT";
    public const string SignOutAll = "AUTH.SIGNOUT_ALL";
    public const string RenewalReuseDetected = "AUTH.RENEWAL_REUSE_DETECTED";
    public const string SessionsRevoked = "AUTH.SESSIONS_REVOKED";
    public const string BranchSelected = "BRANCH.SELECTED";
    public const string RoleCreated = "ROLE.CREATED";
    public const string RoleUpdated = "ROLE.UPDATED";
    public const string RolePermissionsChanged = "ROLE.PERMISSIONS_CHANGED";
    public const string RoleDeactivated = "ROLE.DEACTIVATED";
    public const string RoleReactivated = "ROLE.REACTIVATED";
    public const string RoleDeleted = "ROLE.DELETED";
    public const string RoleAssignmentGranted = "ROLE_ASSIGNMENT.GRANTED";
    public const string RoleAssignmentRevoked = "ROLE_ASSIGNMENT.REVOKED";
    public const string PermissionDenied = "ACCESS.PERMISSION_DENIED";
    public const string CrossTenantDenied = "ACCESS.CROSS_TENANT_DENIED";
    public const string BranchDenied = "ACCESS.BRANCH_DENIED";
    public const string EscalationBlocked = "SECURITY.ESCALATION_BLOCKED";
    public const string OwnerProtectionBlocked = "SECURITY.OWNER_PROTECTION_BLOCKED";
    public const string AuditViewedByPlatform = "AUDIT.VIEWED_BY_PLATFORM";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SignInSucceeded, SignInFailed, AccountLocked, SignOut, SignOutAll, RenewalReuseDetected,
        SessionsRevoked, BranchSelected, RoleCreated, RoleUpdated, RolePermissionsChanged,
        RoleDeactivated, RoleReactivated, RoleDeleted, RoleAssignmentGranted, RoleAssignmentRevoked,
        PermissionDenied, CrossTenantDenied, BranchDenied, EscalationBlocked, OwnerProtectionBlocked,
        AuditViewedByPlatform
    };

    /// <summary>Events that must be saved with their change, or the change fails (FR-040).</summary>
    public static readonly IReadOnlySet<string> Critical = new HashSet<string>(StringComparer.Ordinal)
    {
        RoleCreated, RoleUpdated, RolePermissionsChanged, RoleDeactivated, RoleReactivated, RoleDeleted,
        RoleAssignmentGranted, RoleAssignmentRevoked
    };
}
