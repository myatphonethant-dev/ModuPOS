using ModuPOS.Auth.Http;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Roles;

/// <summary>What the validator needs to know about one catalog permission for this business.</summary>
public sealed record PermissionInfo(
    string Code,
    IReadOnlyCollection<string> DependsOn,
    PermissionScope Scope,
    bool IsRetired,
    bool Available);

public sealed record RoleDraft(string Name, string? Description, IReadOnlyCollection<string> Permissions);

public sealed record ValidatedRole(
    string Name,
    string? Description,
    IReadOnlySet<string> Permissions,
    IReadOnlyList<string> AddedDependencies);

/// <summary>
/// Pure role rules: name 2–60 chars, description ≤ 500, at least one permission, only known
/// business permissions, dependencies added (transitively), only available features, and never
/// more than the actor holds (FR-015–FR-018). Name uniqueness is checked against the database.
/// </summary>
public static class RoleValidator
{
    public const int NameMin = 2;
    public const int NameMax = 60;
    public const int DescriptionMax = 500;

    public static Result<ValidatedRole> Validate(
        RoleDraft draft,
        IReadOnlyDictionary<string, PermissionInfo> catalog,
        IReadOnlySet<string> actorPermissions)
    {
        var name = (draft.Name ?? string.Empty).Trim();
        if (name.Length is < NameMin or > NameMax)
            return RbacErrors.RoleNameInvalid;

        var description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim();
        if (description?.Length > DescriptionMax)
            return RbacErrors.RoleDescriptionInvalid;

        var requested = (draft.Permissions ?? []).Select(p => p.Trim()).ToHashSet(StringComparer.Ordinal);
        if (requested.Count == 0)
            return RbacErrors.PermissionsRequired;

        bool Usable(string code) =>
            catalog.TryGetValue(code, out var p) && !p.IsRetired && p.Scope == PermissionScope.Business;

        var unknown = requested.Where(c => !Usable(c)).ToList();
        if (unknown.Count > 0)
            return RbacErrors.PermissionUnknown(unknown);

        // Add dependencies until nothing new appears.
        var final = new HashSet<string>(requested, StringComparer.Ordinal);
        var pending = new Queue<string>(requested);
        while (pending.TryDequeue(out var code))
        {
            foreach (var dependency in catalog[code].DependsOn)
            {
                if (!Usable(dependency))
                    return RbacErrors.PermissionUnknown([dependency]);

                if (final.Add(dependency))
                    pending.Enqueue(dependency);
            }
        }

        var unavailable = final.Where(c => !catalog[c].Available).ToList();
        if (unavailable.Count > 0)
            return RbacErrors.PermissionUnavailable(unavailable);

        var beyondActor = final.Where(c => !actorPermissions.Contains(c)).ToList();
        if (beyondActor.Count > 0)
            return RbacErrors.EscalationBlocked(beyondActor);

        var added = final.Except(requested).Order(StringComparer.Ordinal).ToList();
        return new ValidatedRole(name, description, final, added);
    }
}
