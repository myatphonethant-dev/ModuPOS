using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ModuPOS.Auth.Roles;
using ModuPOS.FeatureManagement.Interfaces;

namespace ModuPOS.Auth.Authorization;

/// <summary>Whether a feature is enabled for a business. Permissions of disabled features never grant access (FR-018).</summary>
public interface IFeatureAvailability
{
    Task<bool> IsEnabledAsync(Guid businessId, string featureCode, CancellationToken ct = default);
}

internal sealed class FeatureManagerAvailability(IFeatureManager features) : IFeatureAvailability
{
    public Task<bool> IsEnabledAsync(Guid businessId, string featureCode, CancellationToken ct = default) =>
        features.IsEnabledAsync(businessId, featureCode, ct);
}

/// <summary>
/// Effective permissions = union of the staff member's active business-scope roles and active
/// roles for the active branch, minus permissions whose feature is disabled. An Owner role grants
/// every non-retired business permission. Cached per (profile, branch, authorization version).
/// </summary>
public sealed class EffectivePermissionService(
    DbContext db,
    HybridCache cache,
    AuthorizationVersionService versions,
    IFeatureAvailability features)
{
    private static readonly HybridCacheEntryOptions CacheFor5Minutes = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    public async Task<IReadOnlySet<string>> GetForStaffAsync(
        Guid staffProfileId,
        Guid businessId,
        Guid? branchId,
        CancellationToken ct = default)
    {
        var version = await versions.GetAsync(businessId, ct);
        var key = $"perm:{staffProfileId:N}:{branchId?.ToString("N") ?? "none"}:{version}";

        var codes = await cache.GetOrCreateAsync(
            key,
            (self: this, staffProfileId, businessId, branchId),
            static (s, token) => new ValueTask<string[]>(s.self.ComputeAsync(s.staffProfileId, s.businessId, s.branchId, token)),
            CacheFor5Minutes,
            cancellationToken: ct);

        return codes.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Platform admins hold the fixed set of platform permissions (FR-032).</summary>
    public async Task<IReadOnlySet<string>> GetForPlatformAdminAsync(CancellationToken ct = default) =>
        (await db.Set<Permission>()
            .AsNoTracking()
            .Where(p => p.Scope == PermissionScope.Platform && !p.IsRetired)
            .Select(p => p.Code)
            .ToListAsync(ct))
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>Business permissions (non-retired) and whether each one's feature is enabled for the business.</summary>
    public async Task<IReadOnlyList<(CatalogPermission Permission, bool Available)>> CatalogForBusinessAsync(
        Guid businessId,
        CancellationToken ct = default)
    {
        var catalog = await LoadCatalogAsync(ct);
        var enabled = await FeatureStatesAsync(businessId, catalog, ct);
        return catalog.Select(p => (p, p.FeatureCode is null || enabled[p.FeatureCode])).ToList();
    }

    private async Task<string[]> ComputeAsync(Guid staffProfileId, Guid businessId, Guid? branchId, CancellationToken ct)
    {
        // Explicit business filter instead of the tenant filter, so this also works while the
        // pipeline is still building the tenant context.
        var assignments = await db.Set<RoleAssignment>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.StaffProfileId == staffProfileId && a.BusinessId == businessId && a.Role!.BusinessId == businessId)
            .Select(a => new AssignedRole(a.RoleId, a.Role!.IsOwner, a.Role.IsActive, a.BranchId))
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return [];

        var roleIds = assignments.Select(a => a.RoleId).Distinct().ToList();
        var rolePermissions = (await db.Set<RolePermission>()
                .AsNoTracking()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .ToListAsync(ct))
            .GroupBy(rp => rp.RoleId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(rp => rp.PermissionCode).ToList());

        var catalog = await LoadCatalogAsync(ct);
        var enabled = await FeatureStatesAsync(businessId, catalog, ct);

        return EffectivePermissionCalculator
            .Compute(assignments, branchId, rolePermissions, catalog, f => enabled[f])
            .ToArray();
    }

    private Task<List<CatalogPermission>> LoadCatalogAsync(CancellationToken ct) =>
        db.Set<Permission>()
            .AsNoTracking()
            .Where(p => p.Scope == PermissionScope.Business && !p.IsRetired)
            .OrderBy(p => p.Code)
            .Select(p => new CatalogPermission(p.Code, p.FeatureCode))
            .ToListAsync(ct);

    private async Task<Dictionary<string, bool>> FeatureStatesAsync(Guid businessId, IEnumerable<CatalogPermission> catalog, CancellationToken ct)
    {
        var enabled = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var feature in catalog.Select(p => p.FeatureCode).OfType<string>().Distinct())
            enabled[feature] = await features.IsEnabledAsync(businessId, feature, ct);
        return enabled;
    }
}
