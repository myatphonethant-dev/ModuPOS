using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ModuPOS.Auth.Accounts;
using ModuPOS.Organization.Entities;
using ModuPOS.Organization.Services;

namespace ModuPOS.Auth.Authorization;

public sealed record AccessStatus(AccountStatus? Account, ProfileStatus? Profile, BusinessStatus? Business);

/// <summary>
/// Cached account / profile / business status and branch access for the pipeline. Business
/// entries are keyed by the authorization version, so any change that bumps it is seen on the
/// next request. Without a business (platform admins) entries live only 2 seconds.
/// </summary>
public sealed class AccessStatusService(DbContext db, HybridCache cache, IBranchAccessService branches)
{
    private static readonly HybridCacheEntryOptions Versioned = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    private static readonly HybridCacheEntryOptions Short = new()
    {
        Expiration = TimeSpan.FromSeconds(2),
        LocalCacheExpiration = TimeSpan.FromSeconds(2)
    };

    public ValueTask<AccessStatus> GetAsync(Guid userId, Guid profileId, Guid? businessId, long version, CancellationToken ct) =>
        cache.GetOrCreateAsync(
            $"status:{userId:N}:{profileId:N}:{businessId?.ToString("N") ?? "none"}:{version}",
            (db, userId, profileId, businessId),
            static async (s, token) =>
            {
                var account = await s.db.Set<UserAccount>().AsNoTracking()
                    .Where(a => a.Id == s.userId).Select(a => (AccountStatus?)a.Status).FirstOrDefaultAsync(token);
                var profile = await s.db.Set<Profile>().AsNoTracking()
                    .Where(p => p.Id == s.profileId && p.UserAccountId == s.userId && p.BusinessId == s.businessId)
                    .Select(p => (ProfileStatus?)p.Status).FirstOrDefaultAsync(token);
                var business = s.businessId is { } bid
                    ? await s.db.Set<Business>().AsNoTracking().Where(b => b.Id == bid).Select(b => (BusinessStatus?)b.Status).FirstOrDefaultAsync(token)
                    : null;
                return new AccessStatus(account, profile, business);
            },
            businessId is null ? Short : Versioned,
            cancellationToken: ct);

    public ValueTask<BranchAccess> BranchAsync(Guid staffProfileId, Guid businessId, Guid branchId, long version, CancellationToken ct) =>
        cache.GetOrCreateAsync(
            $"branch:{staffProfileId:N}:{branchId:N}:{version}",
            (branches, staffProfileId, businessId, branchId),
            static async (s, token) => await s.branches.CheckAsync(s.staffProfileId, s.businessId, s.branchId, token),
            Versioned,
            cancellationToken: ct);
}
