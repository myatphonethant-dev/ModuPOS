using Microsoft.AspNetCore.Mvc;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Services.Staff;

namespace ModuPOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController(IStaffService staffService, IStaffRoleService staffRoleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? branchCode, CancellationToken ct)
    {
        var result = await staffService.GetStaffListAsync(branchCode, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await staffService.GetStaffByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        var result = await staffService.CreateStaffAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.StaffId }, result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffRequest request, CancellationToken ct)
    {
        var result = await staffService.UpdateStaffAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await staffService.DeleteStaffAsync(id, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // Staff Roles
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await staffRoleService.GetStaffRolesAsync(ct);
        return Ok(result);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateStaffRoleRequest request, CancellationToken ct)
    {
        var result = await staffRoleService.CreateStaffRoleAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateStaffRoleRequest request, CancellationToken ct)
    {
        var result = await staffRoleService.UpdateStaffRoleAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("roles/{id:int}")]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken ct)
    {
        var result = await staffRoleService.DeleteStaffRoleAsync(id, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
