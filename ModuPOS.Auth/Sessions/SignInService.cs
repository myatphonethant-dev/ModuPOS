using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Http;
using ModuPOS.Organization.Entities;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Sessions;

public sealed record ProfileSummary(Guid ProfileId, string Type, Guid? BusinessId, string? BusinessName);

public static class PortalNext
{
    public const string PortalSelected = "portal_selected";
    public const string ChoosePortal = "choose_portal";
    public const string NoAccess = "no_access";
}

public sealed record SignInResult(TokenPair Tokens, IReadOnlyList<ProfileSummary> Profiles, string Next);

/// <summary>Sign-in with generic failures, lockout, and session creation (US1, FR-001–FR-005).</summary>
public sealed class SignInService(
    DbContext db,
    PasswordService passwords,
    SessionTokens tokens,
    IAuditWriter audit,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthOptions> authOptions,
    TimeProvider time)
{
    public async Task<Result<SignInResult>> SignInAsync(string login, string password, RequestInfo request, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var lockout = new LockoutPolicy(authOptions.Value.Lockout);
        login = login.Trim();

        // Username and email columns use a case-insensitive collation.
        var account = await db.Set<UserAccount>().FirstOrDefaultAsync(a => a.Username == login || a.Email == login, ct);

        if (account is null)
        {
            passwords.VerifyDummy(password);
            Fail(null, login, "wrong_credentials", request, now);
            return AuthErrors.InvalidCredentials;
        }

        if (lockout.IsLocked(account, now))
        {
            Fail(account, login, "account_locked", request, now);
            return AuthErrors.AccountLocked(account.LockedUntil!.Value);
        }

        if (!passwords.Verify(account, password))
        {
            var lockedNow = lockout.RegisterFailure(account, now);
            await db.SaveChangesAsync(ct);

            Fail(account, login, "wrong_credentials", request, now);

            if (!lockedNow)
                return AuthErrors.InvalidCredentials;

            audit.Enqueue(new AuditEntry
            {
                Code = AuditCodes.AccountLocked,
                Severity = AuditSeverity.Warning,
                Outcome = AuditOutcome.Blocked,
                OccurredAt = now,
                ActorUserId = account.Id,
                TargetType = "UserAccount",
                TargetId = account.Id.ToString(),
                SourceAddress = request.SourceAddress,
                DeviceDescription = request.DeviceDescription,
                Details = new Dictionary<string, object?>
                {
                    ["failedAttempts"] = account.FailedSignInCount,
                    ["lockedUntil"] = account.LockedUntil
                }
            });
            return AuthErrors.AccountLocked(account.LockedUntil!.Value);
        }

        if (account.Status != AccountStatus.Active)
        {
            await db.SaveChangesAsync(ct); // keep a possible hash upgrade
            Fail(account, login, "account_inactive", request, now);
            return AuthErrors.AccountInactive;
        }

        lockout.Reset(account);

        var profiles = await ActiveProfilesAsync(account.Id, ct);
        var next = profiles.Count switch
        {
            0 => PortalNext.NoAccess,
            1 => PortalNext.PortalSelected,
            _ => PortalNext.ChoosePortal
        };

        var session = new Session
        {
            UserAccountId = account.Id,
            ActiveProfileId = profiles.Count == 1 ? profiles[0].ProfileId : null,
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
            SourceAddress = request.SourceAddress,
            DeviceDescription = request.DeviceDescription
        };
        db.Set<Session>().Add(session);

        var (refreshRow, refreshRaw) = tokens.NewRefreshToken(session);
        await db.SaveChangesAsync(ct);

        var access = await tokens.IssueAccessTokenAsync(session, ct);
        var selected = profiles.Count == 1 ? profiles[0] : null;

        audit.Enqueue(new AuditEntry
        {
            Code = AuditCodes.SignInSucceeded,
            OccurredAt = now,
            ActorUserId = account.Id,
            ActorProfileId = selected?.ProfileId,
            BusinessId = selected?.BusinessId,
            SessionId = session.Id,
            TargetType = "UserAccount",
            TargetId = account.Id.ToString(),
            SourceAddress = request.SourceAddress,
            DeviceDescription = request.DeviceDescription,
            Details = new Dictionary<string, object?> { ["portal"] = selected?.Type ?? next }
        });

        return new SignInResult(
            new TokenPair(access.Token, access.ExpiresAt, refreshRaw, refreshRow.ExpiresAt),
            profiles,
            next);
    }

    /// <summary>Active profiles; staff/member profiles only count when their business is active.</summary>
    private async Task<List<ProfileSummary>> ActiveProfilesAsync(Guid accountId, CancellationToken ct)
    {
        var rows = await (
                from p in db.Set<Profile>().AsNoTracking()
                where p.UserAccountId == accountId && p.Status == ProfileStatus.Active
                join b in db.Set<Business>().AsNoTracking() on p.BusinessId equals b.Id into biz
                from b in biz.DefaultIfEmpty()
                where p.BusinessId == null || b.Status == BusinessStatus.Active
                orderby p.Type
                select new { p.Id, p.Type, p.BusinessId, BusinessName = b == null ? null : b.Name })
            .ToListAsync(ct);

        return rows.Select(r => new ProfileSummary(r.Id, r.Type.ToString(), r.BusinessId, r.BusinessName)).ToList();
    }

    private void Fail(UserAccount? account, string login, string reason, RequestInfo request, DateTime now) =>
        audit.Enqueue(new AuditEntry
        {
            Code = AuditCodes.SignInFailed,
            Severity = AuditSeverity.Warning,
            Outcome = AuditOutcome.Denied,
            OccurredAt = now,
            ActorUserId = account?.Id,
            TargetType = account is null ? null : "UserAccount",
            TargetId = account?.Id.ToString(),
            SourceAddress = request.SourceAddress,
            DeviceDescription = request.DeviceDescription,
            Details = new Dictionary<string, object?> { ["login"] = login, ["reason"] = reason }
        });
}
