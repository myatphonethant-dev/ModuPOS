using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Http;
using ModuPOS.Organization.Services;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Results;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Sessions;

public static class BranchNext
{
    public const string BranchSelected = "branch_selected";
    public const string ChooseBranch = "choose_branch";
    public const string NoBranchAccess = "no_branch_access";
}

public sealed record BranchList(
    IReadOnlyList<BranchSummary> Branches,
    string Next,
    string? AccessToken,
    DateTime? AccessTokenExpiresAt);

public sealed record BranchSelection(string AccessToken, DateTime AccessTokenExpiresAt, BranchSummary Branch);

/// <summary>Lists, auto-selects and switches the staff member's active branch (US5, FR-028–FR-030).</summary>
public sealed class BranchContextService(
    DbContext db,
    IBranchAccessService branches,
    SessionTokens tokens,
    IAuditWriter audit,
    ITenantContext caller,
    TimeProvider time)
{
    public static readonly Error StaffPortalRequired = new(
        "staff_portal_required", "Choose your staff profile before selecting a branch.", StatusCodes.Status403Forbidden);

    public async Task<Result<BranchList>> ListAsync(RequestInfo request, CancellationToken ct)
    {
        if (!IsStaff(out var staffProfileId, out var businessId))
            return StaffPortalRequired;

        var available = await branches.ActiveAssignedAsync(staffProfileId, businessId, ct);

        switch (available.Count)
        {
            case 0:
                return new BranchList(available, BranchNext.NoBranchAccess, null, null);

            case 1:
                var selected = await SwitchToAsync(available[0], request, ct);
                return new BranchList(available, BranchNext.BranchSelected, selected.AccessToken, selected.AccessTokenExpiresAt);

            default:
                return new BranchList(available, BranchNext.ChooseBranch, null, null);
        }
    }

    public async Task<Result<BranchSelection>> SelectAsync(Guid branchId, RequestInfo request, CancellationToken ct)
    {
        if (!IsStaff(out var staffProfileId, out var businessId))
            return StaffPortalRequired;

        var access = await branches.CheckAsync(staffProfileId, businessId, branchId, ct);
        if (access != BranchAccess.Allowed)
        {
            var reason = BranchDeniedReason(access);
            audit.Enqueue(new AuditEntry
            {
                Code = AuditCodes.BranchDenied,
                Severity = access == BranchAccess.OtherBusiness ? AuditSeverity.High : AuditSeverity.Warning,
                Outcome = AuditOutcome.Denied,
                OccurredAt = time.GetUtcNow().UtcDateTime,
                ActorUserId = caller.UserId,
                ActorProfileId = caller.ProfileId,
                BusinessId = businessId,
                BranchId = caller.BranchId,
                SessionId = caller.SessionId,
                TargetType = "Branch",
                TargetId = branchId.ToString(),
                SourceAddress = request.SourceAddress,
                DeviceDescription = request.DeviceDescription,
                Details = new Dictionary<string, object?> { ["branchId"] = branchId, ["reason"] = reason, ["action"] = "select_branch" }
            });
            return AuthErrors.BranchDenied(reason);
        }

        var branch = (await branches.ActiveAssignedAsync(staffProfileId, businessId, ct)).First(b => b.BranchId == branchId);
        return await SwitchToAsync(branch, request, ct);
    }

    public static string BranchDeniedReason(BranchAccess access) => access switch
    {
        BranchAccess.Inactive => "inactive",
        BranchAccess.OtherBusiness => "other_business",
        _ => "not_assigned"
    };

    private async Task<BranchSelection> SwitchToAsync(BranchSummary branch, RequestInfo request, CancellationToken ct)
    {
        var session = await db.Set<Session>().FirstAsync(s => s.Id == caller.SessionId, ct);
        var previous = session.ActiveBranchId;

        if (previous != branch.BranchId)
        {
            session.ActiveBranchId = branch.BranchId;
            await db.SaveChangesAsync(ct);

            audit.Enqueue(new AuditEntry
            {
                Code = AuditCodes.BranchSelected,
                OccurredAt = time.GetUtcNow().UtcDateTime,
                ActorUserId = caller.UserId,
                ActorProfileId = caller.ProfileId,
                BusinessId = caller.BusinessId,
                BranchId = branch.BranchId,
                SessionId = caller.SessionId,
                TargetType = "Branch",
                TargetId = branch.BranchId.ToString(),
                SourceAddress = request.SourceAddress,
                DeviceDescription = request.DeviceDescription,
                Details = new Dictionary<string, object?> { ["previousBranchId"] = previous, ["newBranchId"] = branch.BranchId }
            });
        }

        var access = await tokens.IssueAccessTokenAsync(session, ct);
        return new BranchSelection(access.Token, access.ExpiresAt, branch);
    }

    private bool IsStaff(out Guid staffProfileId, out Guid businessId)
    {
        staffProfileId = caller.ProfileId ?? Guid.Empty;
        businessId = caller.BusinessId ?? Guid.Empty;
        return caller.ProfileType == nameof(ProfileType.Staff) && caller.ProfileId is not null && caller.BusinessId is not null;
    }
}
