using Microsoft.AspNetCore.Http;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Http;

/// <summary>Stable error codes from contracts/rbac-api.md.</summary>
public static class RbacErrors
{
    public static readonly Error RoleNotFound = new("role_not_found", "That role was not found.", StatusCodes.Status404NotFound);
    public static readonly Error StaffNotFound = new("staff_not_found", "That staff member was not found.", StatusCodes.Status404NotFound);
    public static readonly Error AssignmentNotFound = new("assignment_not_found", "That role assignment was not found.", StatusCodes.Status404NotFound);
    public static readonly Error RoleNameTaken = new("role_name_taken", "A role with this name already exists in your business.", StatusCodes.Status409Conflict);
    public static readonly Error RoleNameInvalid = new("role_name_invalid", "Role names must be 2 to 60 characters.", StatusCodes.Status422UnprocessableEntity);
    public static readonly Error RoleDescriptionInvalid = new("role_description_invalid", "Role descriptions can be at most 500 characters.", StatusCodes.Status422UnprocessableEntity);
    public static readonly Error PermissionsRequired = new("permissions_required", "Choose at least one permission.", StatusCodes.Status422UnprocessableEntity);
    public static readonly Error VersionConflict = new("role_version_conflict", "This role was changed by someone else. Reload it and try again.", StatusCodes.Status409Conflict);
    public static readonly Error OwnerProtected = new("owner_protected", "The Owner role cannot be renamed, changed or deleted.", StatusCodes.Status403Forbidden);
    public static readonly Error AssignmentExists = new("assignment_exists", "This staff member already has this role at this scope.", StatusCodes.Status409Conflict);
    public static readonly Error BranchNotAssigned = new("branch_not_assigned", "The staff member is not assigned to that branch.", StatusCodes.Status422UnprocessableEntity);
    public static readonly Error OwnerBusinessScopeOnly = new("owner_business_scope_only", "The Owner role can only be given for the whole business.", StatusCodes.Status422UnprocessableEntity);
    public static readonly Error LastOwner = new("last_owner", "A business must keep at least one active Owner.", StatusCodes.Status409Conflict);
    public static readonly Error AuditUnavailable = new("audit_unavailable", "The change was not saved because its audit record could not be written. Try again.", StatusCodes.Status503ServiceUnavailable);

    public static Error RoleInUse(IEnumerable<object> holders) => new(
        "role_in_use", "Staff still hold this role. Reassign them first.", StatusCodes.Status409Conflict,
        new Dictionary<string, object?> { ["holders"] = holders.ToArray() });

    public static Error PermissionUnknown(IEnumerable<string> codes) => new(
        "permission_unknown", "One or more permissions do not exist.", StatusCodes.Status422UnprocessableEntity,
        new Dictionary<string, object?> { ["permissions"] = codes.Order(StringComparer.Ordinal).ToArray() });

    public static Error PermissionUnavailable(IEnumerable<string> codes) => new(
        "permission_unavailable", "One or more permissions belong to a feature your business does not have.", StatusCodes.Status422UnprocessableEntity,
        new Dictionary<string, object?> { ["permissions"] = codes.Order(StringComparer.Ordinal).ToArray() });

    public static Error EscalationBlocked(IEnumerable<string> codes) => new(
        "escalation_blocked", "You cannot grant permissions you do not hold yourself.", StatusCodes.Status403Forbidden,
        new Dictionary<string, object?> { ["permissions"] = codes.Order(StringComparer.Ordinal).ToArray() });
}
