using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Sessions;
using ModuPOS.Organization.Entities;
using ModuPOS.Shared.Results;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Portals;

public sealed record PortalSelection(string AccessToken, DateTime AccessTokenExpiresAt, ProfileSummary Profile);

/// <summary>Switches the session to one of the caller's profiles after re-checking it (US2, FR-011, FR-012).</summary>
public sealed class PortalService(
    DbContext db,
    SessionTokens tokens,
    ITenantContext caller)
{
    public static readonly Error ProfileNotFound = new("profile_not_found", "That profile was not found.", StatusCodes.Status404NotFound);
    public static readonly Error ProfileInactive = new("profile_inactive", "That profile is not active.", StatusCodes.Status403Forbidden);

    public async Task<Result<PortalSelection>> SelectAsync(Guid profileId, CancellationToken ct)
    {
        var row = await (
                from p in db.Set<Profile>().AsNoTracking()
                where p.Id == profileId && p.UserAccountId == caller.UserId
                join b in db.Set<Business>().AsNoTracking() on p.BusinessId equals b.Id into biz
                from b in biz.DefaultIfEmpty()
                select new { Profile = p, BusinessName = b == null ? null : b.Name, BusinessStatus = b == null ? (BusinessStatus?)null : b.Status })
            .FirstOrDefaultAsync(ct);

        // Someone else's profile looks the same as a missing one.
        if (row is null)
            return ProfileNotFound;

        var businessOk = row.Profile.BusinessId is null || row.BusinessStatus == BusinessStatus.Active;
        if (row.Profile.Status != ProfileStatus.Active || !businessOk)
            return ProfileInactive;

        var session = await db.Set<Session>().FirstAsync(s => s.Id == caller.SessionId, ct);
        session.ActiveProfileId = row.Profile.Id;
        session.ActiveBranchId = null; // a new portal starts without a branch
        await db.SaveChangesAsync(ct);

        var access = await tokens.IssueAccessTokenAsync(session, ct);
        return new PortalSelection(
            access.Token,
            access.ExpiresAt,
            new ProfileSummary(row.Profile.Id, row.Profile.Type.ToString(), row.Profile.BusinessId, row.BusinessName));
    }
}
