using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Authorization;
using ModuPOS.Organization.Entities;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Sessions;

public sealed record CurrentUser(
    UserSummary User,
    ActiveProfileSummary? ActiveProfile,
    ActiveBranchSummary? ActiveBranch,
    IReadOnlyCollection<string> Permissions);

public sealed record UserSummary(Guid Id, string Username, string Email);

public sealed record ActiveProfileSummary(Guid ProfileId, string Type, Guid? BusinessId);

public sealed record ActiveBranchSummary(Guid BranchId, string Name);

/// <summary>Backs GET /api/auth/me: who am I, where am I working, and what may I do (for UI gating).</summary>
public sealed class CurrentUserService(
    DbContext db,
    ITenantContext caller,
    EffectivePermissionService permissions)
{
    public async Task<CurrentUser?> GetAsync(CancellationToken ct)
    {
        var user = await db.Set<UserAccount>()
            .AsNoTracking()
            .Where(u => u.Id == caller.UserId)
            .Select(u => new UserSummary(u.Id, u.Username, u.Email))
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return null;

        var profile = caller.ProfileId is { } pid
            ? new ActiveProfileSummary(pid, caller.ProfileType ?? string.Empty, caller.BusinessId)
            : null;

        var branch = caller.BranchId is { } brid && caller.BusinessId is { } bid
            ? await db.Set<Branch>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => b.Id == brid && b.BusinessId == bid)
                .Select(b => new ActiveBranchSummary(b.Id, b.Name))
                .FirstOrDefaultAsync(ct)
            : null;

        IReadOnlySet<string> granted = caller.ProfileType switch
        {
            nameof(ProfileType.Staff) when caller.ProfileId is { } staff && caller.BusinessId is { } business =>
                await permissions.GetForStaffAsync(staff, business, caller.BranchId, ct),
            nameof(ProfileType.PlatformAdmin) => await permissions.GetForPlatformAdminAsync(ct),
            _ => new HashSet<string>()
        };

        return new CurrentUser(user, profile, branch, granted.Order(StringComparer.Ordinal).ToList());
    }
}
