using Microsoft.AspNetCore.Mvc;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Services.Staff;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for managing staff members and staff role definitions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StaffController(IStaffService staffService, IStaffRoleService staffRoleService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of staff members with optional branch filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? branchCode, [FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await staffService.GetStaffListAsync(branchCode, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single staff member by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await staffService.GetStaffByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Creates a new staff member.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        var result = await staffService.CreateStaffAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.StaffId }, result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing staff member.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffRequest request, CancellationToken ct)
    {
        var result = await staffService.UpdateStaffAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Soft-deletes a staff member.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await staffService.DeleteStaffAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // Staff Roles Catalog Endpoints

    /// <summary>
    /// Retrieves a paginated list of staff roles.
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await staffRoleService.GetStaffRolesAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single staff role by identifier.
    /// </summary>
    [HttpGet("roles/{id:int}")]
    public async Task<IActionResult> GetRoleById(int id, CancellationToken ct)
    {
        var result = await staffRoleService.GetStaffRoleByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Creates a new staff role definition.
    /// </summary>
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateStaffRoleRequest request, CancellationToken ct)
    {
        var result = await staffRoleService.CreateStaffRoleAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetRoleById), new { id = result.Data!.StaffRoleId }, result) : BadRequest(result);
    }

    /// <summary>
    /// Updates an existing staff role definition.
    /// </summary>
    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateStaffRoleRequest request, CancellationToken ct)
    {
        var result = await staffRoleService.UpdateStaffRoleAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Soft-deletes a staff role definition.
    /// </summary>
    [HttpDelete("roles/{id:int}")]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken ct)
    {
        var result = await staffRoleService.DeleteStaffRoleAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
