namespace ModuPOS.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Interfaces;
using ModuPOS.Shared.Pagination;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await permissionService.GetPermissionsAsync(request, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string name, CancellationToken ct)
    {
        var result = await permissionService.CreatePermissionAsync(name, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
