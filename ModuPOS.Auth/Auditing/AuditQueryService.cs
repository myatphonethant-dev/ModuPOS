using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Results;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Auditing;

public sealed record AuditQuery(
    DateTime? From,
    DateTime? To,
    string[]? Code,
    Guid? ActorUserId,
    Guid? BranchId,
    AuditSeverity? Severity,
    int Page = 1,
    int PageSize = 50);

public sealed record AuditItem(
    Guid Id,
    string Code,
    AuditSeverity Severity,
    DateTime OccurredAt,
    AuditOutcome Outcome,
    Guid? ActorUserId,
    Guid? ActorProfileId,
    Guid? BusinessId,
    Guid? BranchId,
    Guid? SessionId,
    string? TargetType,
    string? TargetId,
    string? SourceAddress,
    string? DeviceDescription,
    JsonElement Details,
    int RepeatCount);

public sealed record AuditPage(IReadOnlyList<AuditItem> Items, int Page, int Total);

/// <summary>
/// Read side of the audit log (contracts/audit-api.md, FR-039). Business viewers see only their
/// business (tenant filter); platform viewers must give a reason and each view is audited.
/// </summary>
public sealed class AuditQueryService(DbContext db, ITenantContext caller, IAuditWriter audit, TimeProvider time)
{
    public const int MaxPageSize = 200;

    public static readonly Error ReasonRequired = new(
        "reason_required", "Give a reason (5–500 characters) for viewing this business's audit log.", StatusCodes.Status400BadRequest);

    public static readonly Error QueryInvalid = new(
        "audit_query_invalid", "Use a page of 1 or more, a page size of 1–200, and a range within the last year.", StatusCodes.Status400BadRequest);

    public Task<Result<AuditPage>> ForCallerBusinessAsync(AuditQuery query, CancellationToken ct) =>
        RunAsync(db.Set<AuditEntryRecord>(), query, ct);

    public async Task<Result<AuditPage>> ForPlatformAsync(Guid businessId, AuditQuery query, string? reason, CancellationToken ct)
    {
        reason = reason?.Trim();
        if (reason is null || reason.Length is < 5 or > 500)
            return ReasonRequired;

        var result = await RunAsync(db.Set<AuditEntryRecord>().IgnoreQueryFilters().Where(a => a.BusinessId == businessId), query, ct);
        if (!result.IsSuccess)
            return result;

        audit.Enqueue(new AuditEntry
        {
            Code = AuditCodes.AuditViewedByPlatform,
            Severity = AuditSeverity.Warning,
            OccurredAt = time.GetUtcNow().UtcDateTime,
            ActorUserId = caller.UserId,
            ActorProfileId = caller.ProfileId,
            BusinessId = businessId,
            SessionId = caller.SessionId,
            TargetType = "Business",
            TargetId = businessId.ToString(),
            Details = new Dictionary<string, object?>
            {
                ["businessViewed"] = businessId,
                ["reason"] = reason,
                ["filters"] = new { query.From, query.To, query.Code, query.ActorUserId, query.BranchId, query.Severity, query.Page, query.PageSize }
            }
        });

        return result;
    }

    private async Task<Result<AuditPage>> RunAsync(IQueryable<AuditEntryRecord> source, AuditQuery q, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var oldest = now.AddYears(-1);
        var from = q.From?.ToUniversalTime() ?? now.AddDays(-30);
        var to = q.To?.ToUniversalTime() ?? now;

        if (q.Page < 1 || q.PageSize is < 1 or > MaxPageSize || from < oldest.AddMinutes(-1) || to < from)
            return QueryInvalid;

        var query = source.AsNoTracking().Where(a => a.OccurredAt >= from && a.OccurredAt <= to);
        if (q.Code is { Length: > 0 } codes) query = query.Where(a => codes.Contains(a.Code));
        if (q.ActorUserId is { } actor) query = query.Where(a => a.ActorUserId == actor);
        if (q.BranchId is { } branch) query = query.Where(a => a.BranchId == branch);
        if (q.Severity is { } severity) query = query.Where(a => a.Severity == severity);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(a => a.OccurredAt).ThenBy(a => a.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var items = rows.Select(r => new AuditItem(
            r.Id, r.Code, r.Severity, r.OccurredAt, r.Outcome, r.ActorUserId, r.ActorProfileId, r.BusinessId, r.BranchId,
            r.SessionId, r.TargetType, r.TargetId, r.SourceAddress, r.DeviceDescription,
            JsonDocument.Parse(r.Details).RootElement.Clone(), r.RepeatCount)).ToList();

        return new AuditPage(items, q.Page, total);
    }
}
