using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Auth.Permissions;
using ModuPOS.Auth.Roles;

namespace ModuPOS.Api.Controllers.Rbac;

public sealed record CreateRoleRequest(string Name, string? Description, string[]? Permissions);

public sealed record UpdateRoleRequest(string Name, string? Description, string[]? Permissions, string? Version);

/// <summary>Role management for the caller's business. See contracts/rbac-api.md.</summary>
[ApiController]
[Route("api/roles")]
public sealed class RolesController(RoleService roles) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.RoleView)]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await roles.ListAsync(ct));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.RoleView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await roles.GetAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.RoleManage)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest body, CancellationToken ct)
    {
        var result = await roles.CreateAsync(new RoleDraft(body.Name, body.Description, body.Permissions ?? []), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : result.Error!.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.RoleManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest body, CancellationToken ct)
    {
        var result = await roles.UpdateAsync(id, new RoleUpdate(body.Name, body.Description, body.Permissions ?? [], body.Version), ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(PermissionCodes.RoleManage)]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => SetActive(id, false, ct);

    [HttpPost("{id:guid}/reactivate")]
    [RequirePermission(PermissionCodes.RoleManage)]
    public Task<IActionResult> Reactivate(Guid id, CancellationToken ct) => SetActive(id, true, ct);

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.RoleManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await roles.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.Error!.ToActionResult();
    }

    private async Task<IActionResult> SetActive(Guid id, bool active, CancellationToken ct)
    {
        var result = await roles.SetActiveAsync(id, active, ct);
        return result.IsSuccess ? Ok(new { affectedStaff = result.Value }) : result.Error!.ToActionResult();
    }
}
