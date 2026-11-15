using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Permissions;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only endpoints that exercise pipeline steps no production endpoint uses yet
/// (branch-scoped actions, a business id in the route, platform permissions).
/// </summary>
[ApiController]
[Route("test")]
public sealed class TestEndpointsController(ITenantContext caller) : ControllerBase
{
    [HttpGet("branch-action")]
    [RequirePermission(PermissionCodes.RoleView)]
    [BranchScoped]
    public IActionResult BranchAction() => Ok(new { branchId = caller.BranchId });

    [HttpGet("businesses/{businessId:guid}/thing")]
    [RequirePermission(PermissionCodes.RoleView)]
    public IActionResult BusinessThing(Guid businessId) => Ok(new { businessId });

    [HttpGet("platform-action")]
    [RequirePlatformPermission(PermissionCodes.PlatformAuditView)]
    public IActionResult PlatformAction() => Ok(new { userId = caller.UserId });
}
