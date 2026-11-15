namespace ModuPOS.Auth.Authorization;

/// <summary>One of the staff member's role assignments with the facts the calculation needs.</summary>
public sealed record AssignedRole(Guid RoleId, bool IsOwner, bool IsActive, Guid? BranchId);

/// <summary>A business permission from the catalog (non-retired).</summary>
public sealed record CatalogPermission(string Code, string? FeatureCode);

/// <summary>
/// Pure effective-permission rule (data-model.md): union of active roles that apply at business
/// scope or at the active branch; an Owner role grants every business permission; permissions
/// whose feature is not enabled are dropped (FR-018, FR-025).
/// </summary>
public static class EffectivePermissionCalculator
{
    public static IReadOnlySet<string> Compute(
        IEnumerable<AssignedRole> assignments,
        Guid? activeBranchId,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> rolePermissions,
        IEnumerable<CatalogPermission> businessPermissions,
        Func<string, bool> isFeatureEnabled)
    {
        var catalog = businessPermissions.ToDictionary(p => p.Code, StringComparer.Ordinal);

        var applicable = assignments
            .Where(a => a.IsActive && (a.BranchId is null || a.BranchId == activeBranchId))
            .ToList();

        IEnumerable<string> granted = applicable.Any(a => a.IsOwner)
            ? catalog.Keys
            : applicable.SelectMany(a => rolePermissions.TryGetValue(a.RoleId, out var codes) ? codes : []);

        return granted
            .Where(code => catalog.TryGetValue(code, out var p) && (p.FeatureCode is null || isFeatureEnabled(p.FeatureCode)))
            .ToHashSet(StringComparer.Ordinal);
    }
}
