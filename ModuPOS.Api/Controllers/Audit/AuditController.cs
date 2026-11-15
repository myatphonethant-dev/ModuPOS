using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Auth.Permissions;
using ModuPOS.Shared.Auditing;

namespace ModuPOS.Api.Controllers.Audit;

/// <summary>Read-only audit log. No create, update or delete endpoints exist (contracts/audit-api.md).</summary>
[ApiController]
public sealed class AuditController(AuditQueryService audit) : ControllerBase
{
    [HttpGet("api/audit")]
    [RequirePermission(PermissionCodes.AuditView)]
    public async Task<IActionResult> ForBusiness(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string[]? code,
        [FromQuery] Guid? actorUserId, [FromQuery] Guid? branchId, [FromQuery] AuditSeverity? severity,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await audit.ForCallerBusinessAsync(new AuditQuery(from, to, code, actorUserId, branchId, severity, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }

    [HttpGet("api/platform/businesses/{businessId:guid}/audit")]
    [RequirePlatformPermission(PermissionCodes.PlatformAuditView)]
    public async Task<IActionResult> ForPlatform(
        Guid businessId, [FromQuery] string? reason,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string[]? code,
        [FromQuery] Guid? actorUserId, [FromQuery] Guid? branchId, [FromQuery] AuditSeverity? severity,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await audit.ForPlatformAsync(businessId, new AuditQuery(from, to, code, actorUserId, branchId, severity, page, pageSize), reason, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }
}
