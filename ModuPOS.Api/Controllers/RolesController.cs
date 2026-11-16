using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Services.Role;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for managing security roles and permission assignments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted roles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await roleService.GetRolesAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single role by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await roleService.GetRoleByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.CreateRoleAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing role and its permissions.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.UpdateRoleAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Soft-deletes a role.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await roleService.DeleteRoleAsync(id, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
