using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Auth.Permissions;
using ModuPOS.Auth.Roles;

namespace ModuPOS.Api.Controllers.Rbac;

public sealed record GrantRoleRequest(Guid RoleId, Guid? BranchId);

/// <summary>Role assignments of one staff member. See contracts/rbac-api.md.</summary>
[ApiController]
[Route("api/staff/{staffProfileId:guid}/role-assignments")]
[RequirePermission(PermissionCodes.RoleAssign)]
public sealed class RoleAssignmentsController(RoleAssignmentService assignments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid staffProfileId, CancellationToken ct)
    {
        var result = await assignments.ListAsync(staffProfileId, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Grant(Guid staffProfileId, [FromBody] GrantRoleRequest body, CancellationToken ct)
    {
        var result = await assignments.GrantAsync(staffProfileId, body.RoleId, body.BranchId, ct);
        return result.IsSuccess
            ? Created($"/api/staff/{staffProfileId}/role-assignments/{result.Value!.Id}", result.Value)
            : result.Error!.ToActionResult();
    }

    [HttpDelete("{assignmentId:guid}")]
    public async Task<IActionResult> Revoke(Guid staffProfileId, Guid assignmentId, CancellationToken ct)
    {
        var result = await assignments.RevokeAsync(staffProfileId, assignmentId, ct);
        return result.IsSuccess ? NoContent() : result.Error!.ToActionResult();
    }
}
