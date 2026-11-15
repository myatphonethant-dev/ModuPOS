using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ModuPOS.Auth.Roles;

namespace ModuPOS.Auth.Authorization;

/// <summary>
/// Per-business counter that invalidates cached permissions. Any change to roles, role
/// permissions, assignments, account or branch status increments it (FR-026).
/// </summary>
public sealed class AuthorizationVersionService(DbContext db, HybridCache cache)
{
    private static readonly HybridCacheEntryOptions CacheFor2Seconds = new()
    {
        Expiration = TimeSpan.FromSeconds(2),
        LocalCacheExpiration = TimeSpan.FromSeconds(2)
    };

    public ValueTask<long> GetAsync(Guid businessId, CancellationToken ct = default) =>
        cache.GetOrCreateAsync(
            Key(businessId),
            (db, businessId),
            static async (s, token) => await s.db.Set<AuthorizationVersion>()
                .IgnoreQueryFilters()
                .Where(v => v.BusinessId == s.businessId)
                .Select(v => v.Value)
                .FirstOrDefaultAsync(token),
            CacheFor2Seconds,
            cancellationToken: ct);

    public async Task IncrementAsync(Guid businessId, CancellationToken ct = default)
    {
        await BumpAsync(businessId, ct);
        await db.SaveChangesAsync(ct);
        await InvalidateAsync(businessId, ct);
    }

    /// <summary>
    /// Increments inside the caller's transaction. If no row exists yet one is added to the
    /// unit of work; the caller saves, commits, then calls <see cref="InvalidateAsync"/>.
    /// </summary>
    public async Task BumpAsync(Guid businessId, CancellationToken ct = default)
    {
        var updated = await db.Set<AuthorizationVersion>()
            .IgnoreQueryFilters()
            .Where(v => v.BusinessId == businessId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Value, v => v.Value + 1), ct);

        if (updated == 0)
            db.Set<AuthorizationVersion>().Add(new AuthorizationVersion { BusinessId = businessId, Value = 1 });
    }

    public ValueTask InvalidateAsync(Guid businessId, CancellationToken ct = default) =>
        cache.RemoveAsync(Key(businessId), ct);

    private static string Key(Guid businessId) => $"authv:{businessId:N}";
}
