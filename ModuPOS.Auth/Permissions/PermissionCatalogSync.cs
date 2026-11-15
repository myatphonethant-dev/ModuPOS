using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModuPOS.Auth.Roles;

namespace ModuPOS.Auth.Permissions;

/// <summary>
/// Brings the Permission table in line with <see cref="PermissionCatalog"/>: inserts new codes,
/// updates metadata, retires removed codes, strips retired codes from roles, and bumps every
/// business's authorization version when anything changed.
/// </summary>
public sealed class PermissionCatalogSync(
    IServiceScopeFactory scopes,
    ILogger<PermissionCatalogSync> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => SyncAsync(cancellationToken);

    /// <summary>Runs the sync now. Also called before seeding, so default roles can reference permissions.</summary>
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();

        var existing = await db.Set<Permission>().ToDictionaryAsync(p => p.Code, StringComparer.Ordinal, cancellationToken);
        var defined = PermissionCatalog.All.ToDictionary(p => p.Code, StringComparer.Ordinal);
        var changed = false;

        foreach (var def in defined.Values)
        {
            if (!existing.TryGetValue(def.Code, out var row))
            {
                db.Set<Permission>().Add(new Permission
                {
                    Code = def.Code,
                    Name = def.Name,
                    Group = def.Group,
                    FeatureCode = def.FeatureCode,
                    Scope = def.Scope,
                    DependsOn = [.. def.DependsOn]
                });
                changed = true;
                continue;
            }

            if (row.Name != def.Name || row.Group != def.Group || row.FeatureCode != def.FeatureCode ||
                row.Scope != def.Scope || row.IsRetired || !row.DependsOn.SequenceEqual(def.DependsOn))
            {
                row.Name = def.Name;
                row.Group = def.Group;
                row.FeatureCode = def.FeatureCode;
                row.Scope = def.Scope;
                row.DependsOn = [.. def.DependsOn];
                row.IsRetired = false;
                changed = true;
            }
        }

        var retired = existing.Values.Where(p => !p.IsRetired && !defined.ContainsKey(p.Code)).ToList();
        foreach (var p in retired)
        {
            p.IsRetired = true;
            changed = true;
        }

        if (!changed)
            return;

        await db.SaveChangesAsync(cancellationToken);

        var retiredCodes = await db.Set<Permission>().Where(p => p.IsRetired).Select(p => p.Code).ToListAsync(cancellationToken);
        if (retiredCodes.Count > 0)
        {
            await db.Set<RolePermission>()
                .Where(rp => retiredCodes.Contains(rp.PermissionCode))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await db.Set<AuthorizationVersion>()
            .IgnoreQueryFilters()
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Value, v => v.Value + 1), cancellationToken);

        logger.LogInformation("Permission catalog synced: {Count} defined, {Retired} newly retired.", defined.Count, retired.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
