using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Services.Permission;
using ModuPOS.Shared.Pagination;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for managing permissions catalog.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted permissions.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await permissionService.GetPermissionsAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string name, CancellationToken ct)
    {
        var result = await permissionService.CreatePermissionAsync(name, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
